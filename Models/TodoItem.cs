namespace bookism.Models;

/// <summary>
/// Domain entity representing a personal task associated with a user.
/// </summary>
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
}
