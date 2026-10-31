namespace bookism.Models;

/// <summary>
/// Junction entity representing the many-to-many relationship between books and categories.
/// Maps to table `books_categories`.
/// </summary>
public class BookCategory
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}

