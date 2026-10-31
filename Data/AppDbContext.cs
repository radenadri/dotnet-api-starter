using Microsoft.EntityFrameworkCore;
using bookism.Models;

namespace bookism.Data;

/// <summary>
/// Entity Framework Core DbContext managing relational mappings and entity life cycles.
/// Configured with snake_case naming conventions to adhere to PostgreSQL standards.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Format> Formats => Set<Format>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).HasMaxLength(255);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20).HasDefaultValue("user");
            entity.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // TodoItem entity configuration
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.ToTable("todos");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(50);
            entity.Property(t => t.IsCompleted).IsRequired();
            entity.HasOne(t => t.User)
                .WithMany(u => u.Todos)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Author entity configuration
        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("authors");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Name).IsUnique();
            entity.Property(a => a.Name).IsRequired().HasMaxLength(255);
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(a => a.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Publisher entity configuration
        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.ToTable("publishers");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Name).IsUnique();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(255);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Category entity configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(c => c.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Format entity configuration
        modelBuilder.Entity<Format>(entity =>
        {
            entity.ToTable("formats");
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => f.Name).IsUnique();
            entity.Property(f => f.Name).IsRequired().HasMaxLength(50);
            entity.Property(f => f.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(f => f.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Book entity configuration
        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("books");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(255);
            entity.Property(b => b.Summary).HasColumnType("text");
            entity.Property(b => b.Image).HasMaxLength(255);
            entity.Property(b => b.Language).HasMaxLength(50);
            entity.Property(b => b.Isbn10).HasMaxLength(20);
            entity.Property(b => b.Isbn13).HasMaxLength(30);
            entity.Property(b => b.Rating).HasColumnType("numeric(2,1)");
            entity.Property(b => b.Status).HasMaxLength(50);
            entity.Property(b => b.Progress).HasDefaultValue(0);
            entity.Property(b => b.CurrentPage).HasMaxLength(50);
            entity.Property(b => b.DateStarter).HasColumnType("date");
            entity.Property(b => b.DateFinished).HasColumnType("date");
            entity.Property(b => b.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(b => b.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Global query filter for soft delete
            entity.HasQueryFilter(b => b.DeletedAt == null);

            entity.HasOne(b => b.Publisher)
                .WithMany(p => p.Books)
                .HasForeignKey(b => b.PublisherId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(b => b.Format)
                .WithMany(f => f.Books)
                .HasForeignKey(b => b.FormatId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Junction: BookAuthor configuration
        modelBuilder.Entity<BookAuthor>(entity =>
        {
            entity.ToTable("books_authors");
            entity.HasKey(ba => new { ba.BookId, ba.AuthorId });

            entity.Property(ba => ba.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(ba => ba.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(ba => ba.Book)
                .WithMany(b => b.BookAuthors)
                .HasForeignKey(ba => ba.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ba => ba.Author)
                .WithMany(a => a.BookAuthors)
                .HasForeignKey(ba => ba.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Junction: BookCategory configuration
        modelBuilder.Entity<BookCategory>(entity =>
        {
            entity.ToTable("books_categories");
            entity.HasKey(bc => new { bc.BookId, bc.CategoryId });

            entity.Property(bc => bc.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(bc => bc.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(bc => bc.Book)
                .WithMany(b => b.BookCategories)
                .HasForeignKey(bc => bc.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bc => bc.Category)
                .WithMany(c => c.BookCategories)
                .HasForeignKey(bc => bc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
