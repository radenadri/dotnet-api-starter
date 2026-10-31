namespace bookism.Models;

/// <summary>
/// Domain entity representing a publication media format (e.g., Paperback, Hardcover, E-Book).
/// Maps to table `formats`.
/// </summary>
public class Format
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Book> Books { get; set; } = new List<Book>();
}

