using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using bookism.Dto;
using bookism.Services;

namespace bookism.Controllers;

/// <summary>
/// RESTful endpoint controller managing Todo resource operations.
/// Conforms to Richardson Maturity Level 2 and enterprise response envelope standards.
/// </summary>
[Authorize]
[ApiController]
[Route("api/todos")]
public class TodoController : ControllerBase
{
    private readonly ITodoService _todos;

    public TodoController(ITodoService todos) => _todos = todos;

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string TraceId => CorrelationMiddleware.GetTraceId(HttpContext);

    /// <summary>
    /// Retrieves a paginated list of todo resources for the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        var safePage = page < 1 ? 1 : page;
        var safeLimit = limit < 1 ? 10 : limit;

        var (items, totalCount) = await _todos.GetAllAsync(UserId, safePage, safeLimit);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safeLimit);

        var pagination = new PaginationMeta(safePage, safeLimit, totalPages, totalCount);
        var pagedData = new PagedData<TodoResponseDto>(items, pagination);

        return Ok(ApiResponse<PagedData<TodoResponseDto>>.Create("Data berhasil diambil", pagedData));
    }

    /// <summary>
    /// Retrieves a specific todo item by identifier.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _todos.GetByIdAsync(id, UserId);
        if (item == null)
        {
            return NotFound(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Todo tidak ditemukan",
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<TodoResponseDto>.Create("Data berhasil diambil", item));
    }

    /// <summary>
    /// Creates a new todo item and emits HTTP 201 with Location header.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TodoCreateDto dto)
    {
        var created = await _todos.CreateAsync(dto, UserId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<TodoResponseDto>.Create("Todo berhasil dibuat", created)
        );
    }

    /// <summary>
    /// Replaces/updates an existing todo item if owned by the user.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TodoUpdateDto dto)
    {
        var updated = await _todos.UpdateAsync(id, dto, UserId);
        if (updated == null)
        {
            return NotFound(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Todo tidak ditemukan",
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<TodoResponseDto>.Create("Todo berhasil diperbarui", updated));
    }

    /// <summary>
    /// Deletes a specific todo item if owned by the user.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _todos.DeleteAsync(id, UserId);
        if (!deleted)
        {
            return NotFound(new ApiErrorResponse(new ApiErrorDetail(
                Message: "Todo tidak ditemukan",
                TraceId: TraceId
            )));
        }

        return Ok(ApiResponse<object>.Create("Todo berhasil dihapus", null));
    }
}
