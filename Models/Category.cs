namespace bookism.Models;

/// <summary>
/// Domain entity representing a genre/category taxonomy.
/// Maps to table `categories`.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
}

