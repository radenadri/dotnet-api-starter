namespace bookism.Models;

/// <summary>
/// Junction entity representing the many-to-many relationship between books and authors.
/// Maps to table `books_authors`.
/// </summary>
public class BookAuthor
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int AuthorId { get; set; }
    public Author? Author { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}

