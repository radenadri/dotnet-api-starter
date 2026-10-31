namespace bookism.Models;

/// <summary>
/// Domain entity representing an authenticated user account with RBAC capabilities.
/// Follows enterprise standards for identity tracking, account status, and audit timestamps.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // "admin" or "user"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TodoItem> Todos { get; set; } = new List<TodoItem>();
}
