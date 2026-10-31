namespace bookism.Models;

/// <summary>
/// Domain entity representing a book item in the catalog.
/// Maps to table `books`.
/// </summary>
public class Book
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Image { get; set; }
    public string? Language { get; set; }
    public string? Isbn10 { get; set; }
    public string? Isbn13 { get; set; }
    public int? TotalPages { get; set; }
    public int? ReleaseDate { get; set; }
    public decimal? Rating { get; set; }
    public string? Status { get; set; }
    public int? Progress { get; set; } = 0;
    public string? CurrentPage { get; set; }
    public DateOnly? DateStarter { get; set; }
    public DateOnly? DateFinished { get; set; }
    public DateTime? DeletedAt { get; set; }

    public int? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }

    public int? FormatId { get; set; }
    public Format? Format { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
}

