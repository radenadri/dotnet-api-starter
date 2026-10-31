using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using bookism.Dto;
using bookism.Services;

namespace bookism.Controllers;

/// <summary>
/// RESTful endpoint controller governing the Book Catalog resource.
/// Implements Richardson Maturity Level 2, RBAC (admin for mutations), and envelope standards.
/// </summary>
[ApiController]
[Route("api/books")]
public class BookController : ControllerBase
{
    private readonly IBookService _bookService;

    public BookController(IBookService bookService)
    {
        _bookService = bookService;
    }

    private string TraceId => CorrelationMiddleware.GetTraceId(HttpContext);

    /// <summary>
    /// Retrieves a paginated list of books with optional text search.
    /// Executed via Dapper for high-throughput reads.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safeLimit = limit < 1 ? 10 : limit;

        var (items, totalCount) = await _bookService.GetAllAsync(safePage, safeLimit, search, ct);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safeLimit);

        var pagination = new PaginationMeta(safePage, safeLimit, totalPages, totalCount);
        var pagedData = new PagedData<BookDetailDto>(items, pagination);

        return Ok(ApiResponse<PagedData<BookDetailDto>>.Create("Data buku berhasil diambil", pagedData));
    }

    /// <summary>
    /// Retrieves a single book by identifier.
    /// Executed via Dapper.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var book = await _bookService.GetByIdAsync(id, ct);
        if (book == null)
        {
            return NotFound(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Buku tidak ditemukan",
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<BookDetailDto>.Create("Detail buku berhasil diambil", book));
    }

    /// <summary>
    /// Creates a new book entity.
    /// Restricted to administrators. Executed via EF Core.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] BookCreateDto dto, CancellationToken ct)
    {
        var created = await _bookService.CreateAsync(dto, ct);
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<BookDetailDto>.Create("Buku berhasil ditambahkan", created)
        );
    }

    /// <summary>
    /// Updates an existing book entity.
    /// Restricted to administrators. Executed via EF Core.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] BookUpdateDto dto, CancellationToken ct)
    {
        var updated = await _bookService.UpdateAsync(id, dto, ct);
        if (updated == null)
        {
            return NotFound(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Buku tidak ditemukan",
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<BookDetailDto>.Create("Buku berhasil diperbarui", updated));
    }

    /// <summary>
    /// Soft-deletes a book entity.
    /// Restricted to administrators. Executed via EF Core.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _bookService.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Buku tidak ditemukan",
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<object>.Create("Buku berhasil dihapus", null));
    }
}

