using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using bookism.Data;
using bookism.Dto;
using bookism.Models;

namespace bookism.Services;

/// <summary>
/// Service contract defining operations for managing todo items scoped to authenticated users.
/// </summary>
public interface ITodoService
{
    /// <summary>
    /// Retrieves a paginated slice of todo records for the requesting user using Dapper (Read).
    /// </summary>
    Task<(IEnumerable<TodoResponseDto> Items, int TotalCount)> GetAllAsync(Guid userId, int page, int limit, CancellationToken ct = default);

    /// <summary>
    /// Fetches a single todo item by ID, enforcing user ownership using Dapper (Read).
    /// </summary>
    Task<TodoResponseDto?> GetByIdAsync(int id, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new todo item associated with the requesting user using EF Core (Write).
    /// </summary>
    Task<TodoResponseDto> CreateAsync(TodoCreateDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing todo item if owned by the user using EF Core (Write).
    /// </summary>
    Task<TodoResponseDto?> UpdateAsync(int id, TodoUpdateDto dto, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a todo item if owned by the user using EF Core (Write).
    /// </summary>
    Task<bool> DeleteAsync(int id, Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Hybrid CQS implementation:
/// - READ: Dapper executing raw SQL with CommandDefinition and CancellationToken.
/// - WRITE: Entity Framework Core change tracking and persistence.
/// </summary>
public class TodoService : ITodoService
{
    private readonly AppDbContext _db;

    public TodoService(AppDbContext db) => _db = db;

    /// <summary>
    /// Read Query using Dapper for optimal retrieval performance.
    /// </summary>
    public async Task<(IEnumerable<TodoResponseDto> Items, int TotalCount)> GetAllAsync(Guid userId, int page, int limit, CancellationToken ct = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safeLimit = limit switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => limit
        };
        var offset = (safePage - 1) * safeLimit;

        var connection = _db.Database.GetDbConnection();

        const string sql = @"
            SELECT COUNT(1) FROM todos WHERE user_id = @UserId;

            SELECT id AS Id, title AS Title, is_completed AS IsCompleted
            FROM todos
            WHERE user_id = @UserId
            ORDER BY id DESC
            LIMIT @Limit OFFSET @Offset;";

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, Limit = safeLimit, Offset = offset }, cancellationToken: ct));

        int totalCount = await multi.ReadFirstAsync<int>();
        var items = (await multi.ReadAsync<TodoResponseDto>()).ToList();

        return (items, totalCount);
    }

    /// <summary>
    /// Single item Read Query using Dapper.
    /// </summary>
    public async Task<TodoResponseDto?> GetByIdAsync(int id, Guid userId, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();

        const string sql = @"
            SELECT id AS Id, title AS Title, is_completed AS IsCompleted
            FROM todos
            WHERE id = @Id AND user_id = @UserId
            LIMIT 1;";

        return await connection.QueryFirstOrDefaultAsync<TodoResponseDto>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));
    }

    /// <summary>
    /// Write Command using Entity Framework Core.
    /// </summary>
    public async Task<TodoResponseDto> CreateAsync(TodoCreateDto dto, Guid userId, CancellationToken ct = default)
    {
        var todo = new TodoItem
        {
            Title = dto.Title,
            IsCompleted = false,
            UserId = userId
        };

        _db.Todos.Add(todo);
        await _db.SaveChangesAsync(ct);

        return new TodoResponseDto(todo.Id, todo.Title, todo.IsCompleted);
    }

    /// <summary>
    /// Write Command using Entity Framework Core.
    /// </summary>
    public async Task<TodoResponseDto?> UpdateAsync(int id, TodoUpdateDto dto, Guid userId, CancellationToken ct = default)
    {
        var todo = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (todo == null) return null;

        todo.Title = dto.Title;
        todo.IsCompleted = dto.IsCompleted;

        await _db.SaveChangesAsync(ct);
        return new TodoResponseDto(todo.Id, todo.Title, todo.IsCompleted);
    }

    /// <summary>
    /// Write Command using Entity Framework Core.
    /// </summary>
    public async Task<bool> DeleteAsync(int id, Guid userId, CancellationToken ct = default)
    {
        var todo = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (todo == null) return false;

        _db.Todos.Remove(todo);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
