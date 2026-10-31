using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using bookism.Data;
using bookism.Dto;
using bookism.Models;

namespace bookism.Services;

/// <summary>
/// Service contract defining operations for books catalog with caching support.
/// </summary>
public interface IBookService
{
    Task<(IEnumerable<BookDetailDto> Items, int TotalCount)> GetAllAsync(int page, int limit, string? search, CancellationToken ct = default);
    Task<BookDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BookDetailDto> CreateAsync(BookCreateDto dto, CancellationToken ct = default);
    Task<BookDetailDto?> UpdateAsync(int id, BookUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Hybrid implementation demonstrating CQS (Command Query Separation) with resilient caching:
/// - Cache-Aside pattern via ICacheService (Redis L2 + Memory L1).
/// - Dapper for Read queries with multi-mapping and zero tracking.
/// - EF Core for Write commands with entity relationships and automatic cache eviction.
/// </summary>
public class BookService : IBookService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    private const string BookDetailCachePrefix = "books:";
    private const string BookListCachePrefix = "books:list:";

    public BookService(AppDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>
    /// Cache-aside paginated list read query.
    /// Check cache first -> if miss, fetch Dapper -> store in cache (5m absolute / 1m sliding).
    /// </summary>
    public async Task<(IEnumerable<BookDetailDto> Items, int TotalCount)> GetAllAsync(int page, int limit, string? search, CancellationToken ct = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safeLimit = limit switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => limit
        };

        var cacheKey = $"{BookListCachePrefix}p{safePage}:l{safeLimit}:s{search?.Trim().ToLowerInvariant() ?? "all"}";

        // 1. Check Cache
        var cached = await _cache.GetAsync<BookListCacheEntry>(cacheKey, ct);
        if (cached != null)
        {
            return (cached.Items, cached.TotalCount);
        }

        // 2. Fetch from database using Dapper
        var offset = (safePage - 1) * safeLimit;
        var connection = _db.Database.GetDbConnection();
        var searchParam = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(1) 
            FROM books b
            WHERE b.deleted_at IS NULL
              AND (@Search IS NULL OR b.name ILIKE @Search);";

        int totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { Search = searchParam }, cancellationToken: ct));

        if (totalCount == 0)
        {
            var emptyResult = (Enumerable.Empty<BookDetailDto>(), 0);
            await _cache.SetAsync(cacheKey, new BookListCacheEntry(emptyResult.Item1.ToList(), 0), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(30), ct);
            return emptyResult;
        }

        const string booksSql = @"
            SELECT 
                b.id, b.name, b.summary, b.image, b.language, b.isbn_10 AS Isbn10, b.isbn_13 AS Isbn13,
                b.total_pages AS TotalPages, b.release_date AS ReleaseDate, b.rating, b.status,
                b.progress, b.current_page AS CurrentPage, b.date_starter AS DateStarter, b.date_finished AS DateFinished,
                b.created_at AS CreatedAt, b.updated_at AS UpdatedAt,
                p.id AS PublisherId, p.name AS PublisherName,
                f.id AS FormatId, f.name AS FormatName
            FROM books b
            LEFT JOIN publishers p ON b.publisher_id = p.id
            LEFT JOIN formats f ON b.format_id = f.id
            WHERE b.deleted_at IS NULL
              AND (@Search IS NULL OR b.name ILIKE @Search)
            ORDER BY b.id DESC
            LIMIT @Limit OFFSET @Offset;";

        var rows = await connection.QueryAsync(
            new CommandDefinition(booksSql, new { Search = searchParam, Limit = safeLimit, Offset = offset }, cancellationToken: ct));

        var bookList = rows.ToList();
        if (bookList.Count == 0)
        {
            return (Enumerable.Empty<BookDetailDto>(), totalCount);
        }

        var bookIds = bookList.Select(r => (int)r.id).ToArray();

        const string authorsSql = @"
            SELECT ba.book_id AS BookId, a.id, a.name
            FROM books_authors ba
            JOIN authors a ON ba.author_id = a.id
            WHERE ba.book_id = ANY(@BookIds);";

        var authorRows = (await connection.QueryAsync(
            new CommandDefinition(authorsSql, new { BookIds = bookIds }, cancellationToken: ct))).ToList();

        const string categoriesSql = @"
            SELECT bc.book_id AS BookId, c.id, c.name
            FROM books_categories bc
            JOIN categories c ON bc.category_id = c.id
            WHERE bc.book_id = ANY(@BookIds);";

        var categoryRows = (await connection.QueryAsync(
            new CommandDefinition(categoriesSql, new { BookIds = bookIds }, cancellationToken: ct))).ToList();

        var result = new List<BookDetailDto>();
        foreach (var r in bookList)
        {
            int bookId = r.id;
            PublisherSummaryDto? pub = r.publisherid != null ? new PublisherSummaryDto((int)r.publisherid, (string)r.publishername) : null;
            FormatSummaryDto? fmt = r.formatid != null ? new FormatSummaryDto((int)r.formatid, (string)r.formatname) : null;

            var authors = authorRows
                .Where(a => (int)a.bookid == bookId)
                .Select(a => new AuthorSummaryDto((int)a.id, (string)a.name))
                .ToList();

            var categories = categoryRows
                .Where(c => (int)c.bookid == bookId)
                .Select(c => new CategorySummaryDto((int)c.id, (string)c.name))
                .ToList();

            result.Add(new BookDetailDto(
                Id: bookId,
                Name: (string)r.name,
                Summary: (string?)r.summary,
                Image: (string?)r.image,
                Language: (string?)r.language,
                Isbn10: (string?)r.isbn10,
                Isbn13: (string?)r.isbn13,
                TotalPages: (int?)r.totalpages,
                ReleaseDate: (int?)r.releasedate,
                Rating: (decimal?)r.rating,
                Status: (string?)r.status,
                Progress: (int?)r.progress,
                CurrentPage: (string?)r.currentpage,
                DateStarter: r.datestarter != null ? DateOnly.FromDateTime((DateTime)r.datestarter) : null,
                DateFinished: r.datefinished != null ? DateOnly.FromDateTime((DateTime)r.datefinished) : null,
                Publisher: pub,
                Format: fmt,
                Authors: authors,
                Categories: categories,
                CreatedAt: (DateTime?)r.createdat,
                UpdatedAt: (DateTime?)r.updatedat
            ));
        }

        // 3. Save to Cache (Absolute 5m, Sliding 1m)
        await _cache.SetAsync(cacheKey, new BookListCacheEntry(result, totalCount), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), ct);

        return (result, totalCount);
    }

    /// <summary>
    /// Cache-aside single book details read query (Absolute 10m, Sliding 2m).
    /// </summary>
    public async Task<BookDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"{BookDetailCachePrefix}{id}";

        // 1. Check Cache
        var cached = await _cache.GetAsync<BookDetailDto>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        // 2. Fetch from DB
        var connection = _db.Database.GetDbConnection();

        const string bookSql = @"
            SELECT 
                b.id, b.name, b.summary, b.image, b.language, b.isbn_10 AS Isbn10, b.isbn_13 AS Isbn13,
                b.total_pages AS TotalPages, b.release_date AS ReleaseDate, b.rating, b.status,
                b.progress, b.current_page AS CurrentPage, b.date_starter AS DateStarter, b.date_finished AS DateFinished,
                b.created_at AS CreatedAt, b.updated_at AS UpdatedAt,
                p.id AS PublisherId, p.name AS PublisherName,
                f.id AS FormatId, f.name AS FormatName
            FROM books b
            LEFT JOIN publishers p ON b.publisher_id = p.id
            LEFT JOIN formats f ON b.format_id = f.id
            WHERE b.id = @Id AND b.deleted_at IS NULL
            LIMIT 1;";

        var r = await connection.QueryFirstOrDefaultAsync(
            new CommandDefinition(bookSql, new { Id = id }, cancellationToken: ct));

        if (r == null) return null;

        const string authorsSql = @"
            SELECT a.id, a.name
            FROM books_authors ba
            JOIN authors a ON ba.author_id = a.id
            WHERE ba.book_id = @Id;";

        var authors = (await connection.QueryAsync<AuthorSummaryDto>(
            new CommandDefinition(authorsSql, new { Id = id }, cancellationToken: ct))).ToList();

        const string categoriesSql = @"
            SELECT c.id, c.name
            FROM books_categories bc
            JOIN categories c ON bc.category_id = c.id
            WHERE bc.book_id = @Id;";

        var categories = (await connection.QueryAsync<CategorySummaryDto>(
            new CommandDefinition(categoriesSql, new { Id = id }, cancellationToken: ct))).ToList();

        PublisherSummaryDto? pub = r.publisherid != null ? new PublisherSummaryDto((int)r.publisherid, (string)r.publishername) : null;
        FormatSummaryDto? fmt = r.formatid != null ? new FormatSummaryDto((int)r.formatid, (string)r.formatname) : null;

        var book = new BookDetailDto(
            Id: (int)r.id,
            Name: (string)r.name,
            Summary: (string?)r.summary,
            Image: (string?)r.image,
            Language: (string?)r.language,
            Isbn10: (string?)r.isbn10,
            Isbn13: (string?)r.isbn13,
            TotalPages: (int?)r.totalpages,
            ReleaseDate: (int?)r.releasedate,
            Rating: (decimal?)r.rating,
            Status: (string?)r.status,
            Progress: (int?)r.progress,
            CurrentPage: (string?)r.currentpage,
            DateStarter: r.datestarter != null ? DateOnly.FromDateTime((DateTime)r.datestarter) : null,
            DateFinished: r.datefinished != null ? DateOnly.FromDateTime((DateTime)r.datefinished) : null,
            Publisher: pub,
            Format: fmt,
            Authors: authors,
            Categories: categories,
            CreatedAt: (DateTime?)r.createdat,
            UpdatedAt: (DateTime?)r.updatedat
        );

        // 3. Save to Cache
        await _cache.SetAsync(cacheKey, book, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2), ct);

        return book;
    }

    /// <summary>
    /// Write Command using Entity Framework Core. Evicts list cache on creation.
    /// </summary>
    public async Task<BookDetailDto> CreateAsync(BookCreateDto dto, CancellationToken ct = default)
    {
        var book = new Book
        {
            Name = dto.Name,
            Summary = dto.Summary,
            Image = dto.Image,
            Language = dto.Language,
            Isbn10 = dto.Isbn10,
            Isbn13 = dto.Isbn13,
            TotalPages = dto.TotalPages,
            ReleaseDate = dto.ReleaseDate,
            Rating = dto.Rating,
            Status = dto.Status,
            Progress = dto.Progress ?? 0,
            CurrentPage = dto.CurrentPage,
            PublisherId = dto.PublisherId,
            FormatId = dto.FormatId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dto.AuthorIds != null && dto.AuthorIds.Count > 0)
        {
            foreach (var authorId in dto.AuthorIds)
            {
                book.BookAuthors.Add(new BookAuthor { AuthorId = authorId });
            }
        }

        if (dto.CategoryIds != null && dto.CategoryIds.Count > 0)
        {
            foreach (var categoryId in dto.CategoryIds)
            {
                book.BookCategories.Add(new BookCategory { CategoryId = categoryId });
            }
        }

        _db.Books.Add(book);
        await _db.SaveChangesAsync(ct);

        // Invalidate list cache
        await _cache.RemoveByPrefixAsync(BookListCachePrefix, ct);

        if (_db.Database.IsRelational())
        {
            return (await GetByIdAsync(book.Id, ct))!;
        }

        return new BookDetailDto(
            book.Id, book.Name, book.Summary, book.Image, book.Language,
            book.Isbn10, book.Isbn13, book.TotalPages, book.ReleaseDate,
            book.Rating, book.Status, book.Progress, book.CurrentPage,
            book.DateStarter, book.DateFinished, null, null, new(), new(),
            book.CreatedAt, book.UpdatedAt);
    }

    /// <summary>
    /// Write Command using Entity Framework Core. Evicts single & list cache on update.
    /// </summary>
    public async Task<BookDetailDto?> UpdateAsync(int id, BookUpdateDto dto, CancellationToken ct = default)
    {
        var book = await _db.Books
            .Include(b => b.BookAuthors)
            .Include(b => b.BookCategories)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (book == null) return null;

        book.Name = dto.Name;
        book.Summary = dto.Summary;
        book.Image = dto.Image;
        book.Language = dto.Language;
        book.Isbn10 = dto.Isbn10;
        book.Isbn13 = dto.Isbn13;
        book.TotalPages = dto.TotalPages;
        book.ReleaseDate = dto.ReleaseDate;
        book.Rating = dto.Rating;
        book.Status = dto.Status;
        book.Progress = dto.Progress ?? 0;
        book.CurrentPage = dto.CurrentPage;
        book.PublisherId = dto.PublisherId;
        book.FormatId = dto.FormatId;
        book.UpdatedAt = DateTime.UtcNow;

        if (dto.AuthorIds != null)
        {
            book.BookAuthors.Clear();
            foreach (var authorId in dto.AuthorIds)
            {
                book.BookAuthors.Add(new BookAuthor { BookId = id, AuthorId = authorId });
            }
        }

        if (dto.CategoryIds != null)
        {
            book.BookCategories.Clear();
            foreach (var categoryId in dto.CategoryIds)
            {
                book.BookCategories.Add(new BookCategory { BookId = id, CategoryId = categoryId });
            }
        }

        await _db.SaveChangesAsync(ct);

        // Invalidate single item cache & all list cache
        await _cache.RemoveAsync($"{BookDetailCachePrefix}{id}", ct);
        await _cache.RemoveByPrefixAsync(BookListCachePrefix, ct);

        if (_db.Database.IsRelational())
        {
            return (await GetByIdAsync(id, ct))!;
        }

        return new BookDetailDto(
            book.Id, book.Name, book.Summary, book.Image, book.Language,
            book.Isbn10, book.Isbn13, book.TotalPages, book.ReleaseDate,
            book.Rating, book.Status, book.Progress, book.CurrentPage,
            book.DateStarter, book.DateFinished, null, null, new(), new(),
            book.CreatedAt, book.UpdatedAt);
    }

    /// <summary>
    /// Write Command using EF Core for soft delete. Evicts single & list cache on delete.
    /// </summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book == null) return false;

        book.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Invalidate cache
        await _cache.RemoveAsync($"{BookDetailCachePrefix}{id}", ct);
        await _cache.RemoveByPrefixAsync(BookListCachePrefix, ct);

        return true;
    }
}

/// <summary>
/// Serializable internal cache payload for paginated book collections.
/// </summary>
public record BookListCacheEntry(List<BookDetailDto> Items, int TotalCount);
