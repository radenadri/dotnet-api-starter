# Entity Framework Core & Dapper Cheatsheet

Panduan perbandingan arsitektur, sintaks query, manipulasi data, transaksi, optimasi performa (`AsSplitQuery`), `CancellationToken`, dan pola hybrid antara Entity Framework Core dan Dapper pada ASP.NET Core (.NET 8+).

---

## 1. Perbandingan Cepat: EF Core vs Dapper

| Fitur | Entity Framework Core (ORM) | Dapper (Micro-ORM) |
| :--- | :--- | :--- |
| **Kategori** | Full-featured ORM | Lightweight Object Mapper |
| **Gaya Query** | LINQ to SQL, Strongly-typed | Raw SQL murni / ANSI SQL |
| **Change Tracker** | Ada (otomatis melacak status perubahan objek) | Tidak ada (manual query tanpa overhead tracking) |
| **Migrasi Skema** | Bawaan (`dotnet ef migrations`) | Membutuhkan tool eksternal (DbUp, Flyway, FluentMigrator) |
| **Performa** | Baik (gunakan `AsNoTracking()` untuk read) | Sangat cepat (mendekati performa native ADO.NET) |
| **Use Case Utama** | Logika bisnis domain, transactional CRUD kompleks | Read-heavy, reporting/analitik, complex joins & aggregates |

---

## 2. Entity Framework Core

### A. Setup Dependency Injection & Context
```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

### B. Query Data (Read) & Optimasi
```csharp
// 1. Single Item / Null Check dengan CancellationToken
var item = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
var itemRequired = await _db.Todos.SingleAsync(t => t.Id == id, ct); // Throw exception jika kosong / duplikat

// 2. List + Filter + Sort (Read-Only Optimization)
var items = await _db.Todos
    .AsNoTracking() // Matikan change tracking untuk menghemat memori & CPU
    .Where(t => t.UserId == userId && !t.IsCompleted)
    .OrderByDescending(t => t.CreatedAt)
    .ToListAsync(ct);

// 3. Pagination
var paged = await _db.Todos
    .AsNoTracking()
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);

// 4. Projection langsung ke DTO (hanya mengambil kolom yang dibutuhkan)
var dtos = await _db.Todos
    .AsNoTracking()
    .Where(t => t.UserId == userId)
    .Select(t => new TodoResponseDto(t.Id, t.Title))
    .ToListAsync(ct);

// 5. Relasi / Join (Eager Loading)
var userWithTodos = await _db.Users
    .Include(u => u.Todos)
    .FirstOrDefaultAsync(u => u.Id == userId, ct);
```

#### Split Queries vs Single Query (`AsSplitQuery`)
Secara default, EF Core memuat relasi collection dengan single query SQL via `LEFT JOIN`. Ini bisa menimbulkan **Cartesian Explosion** jika tabel relasi berukuran besar:

```csharp
// Menghindari Cartesian Explosion pada multi-collection Include
var authorWithBooks = await _db.Authors
    .AsNoTracking()
    .Include(a => a.Books)
        .ThenInclude(b => b.Reviews)
    .AsSplitQuery() // Membagi jadi beberapa query SQL terpisah berdasarkan foreign key
    .FirstOrDefaultAsync(a => a.Id == authorId, ct);
```

> [!TIP]
> Gunakan `.AsSplitQuery()` ketika ada multiple `.Include` ke collection untuk mencegah duplikasi data baris yang ditransfer lewat jaringan.

---

### C. Mutasi Data (Insert, Update, Delete)

#### 1. Cara Konvensional (Change Tracking)
```csharp
// Insert single
var todo = new TodoItem { Title = "Beli kopi", UserId = userId };
_db.Todos.Add(todo);
await _db.SaveChangesAsync(ct);

// Insert batch
_db.Todos.AddRange(todoList);
await _db.SaveChangesAsync(ct);

// Update konvensional (Fetch -> Mutasi -> Save)
var existing = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
if (existing != null)
{
    existing.Title = "Updated title";
    await _db.SaveChangesAsync(ct);
}

// Delete konvensional (Fetch -> Remove -> Save)
var toDelete = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
if (toDelete != null)
{
    _db.Todos.Remove(toDelete);
    await _db.SaveChangesAsync(ct);
}
```

#### 2. Bulk Direct Execution (`ExecuteUpdate` & `ExecuteDelete`) (.NET 7/8+)
Eksekusi pembaruan/penghapusan langsung di level database tanpa memuat entity ke dalam memori aplikasi:

```csharp
// Bulk update langsung di DB
await _db.Todos
    .Where(t => t.UserId == userId && !t.IsCompleted)
    .ExecuteUpdateAsync(s => s
        .SetProperty(t => t.IsCompleted, true)
        .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), ct);

// Bulk delete langsung di DB
await _db.Todos
    .Where(t => t.IsCompleted && t.UserId == userId)
    .ExecuteDeleteAsync(ct);
```

---

### D. Raw SQL di EF Core
```csharp
// Query entity via Raw SQL interpolasi (aman dari SQL Injection)
var active = await _db.Todos
    .FromSqlInterpolated($"SELECT * FROM \"Todos\" WHERE \"UserId\" = {userId}")
    .ToListAsync(ct);

// Non-query command (DDL, Stored Procedure, atau Direct Update)
int rowsAffected = await _db.Database
    .ExecuteSqlRawAsync("UPDATE \"Todos\" SET \"IsCompleted\" = true WHERE \"UserId\" = {0}", new object[] { userId }, ct);
```

---

### E. Database Transaction
```csharp
using var tx = await _db.Database.BeginTransactionAsync(ct);
try
{
    _db.Todos.Add(todo);
    await _db.SaveChangesAsync(ct);

    _db.Logs.Add(new ActivityLog { Action = "Create Todo" });
    await _db.SaveChangesAsync(ct);

    await tx.CommitAsync(ct);
}
catch
{
    await tx.RollbackAsync(ct);
    throw;
}
```

---

## 3. Dapper (Micro-ORM)

### Instalasi Package
```bash
dotnet add package Dapper
```

---

### A. Setup Dependency Injection (`IDbConnection`)
```csharp
// Program.cs
using System.Data;
using Npgsql;

builder.Services.AddScoped<IDbConnection>(sp =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

### B. Query Single, First, Scalar, & CommandDefinition (CancellationToken)
Untuk meneruskan `CancellationToken` pada Dapper, bungkus query dengan `CommandDefinition`:

```csharp
// 1. Query satu baris atau default (null) dengan CancellationToken
string sql = "SELECT * FROM \"Todos\" WHERE \"Id\" = @Id AND \"UserId\" = @UserId";
var cmd = new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct);
var todo = await _db.QueryFirstOrDefaultAsync<TodoItem>(cmd);

// 2. Single item (lempar exception jika tidak ada atau lebih dari 1)
var single = await _db.QuerySingleAsync<TodoItem>(cmd);

// 3. Scalar value (misal: COUNT / SUM)
string countSql = "SELECT COUNT(1) FROM \"Todos\" WHERE \"UserId\" = @UserId";
int count = await _db.ExecuteScalarAsync<int>(new CommandDefinition(countSql, new { UserId = userId }, cancellationToken: ct));
```

---

### C. Query List & Collection
```csharp
string sql = @"
    SELECT ""Id"", ""Title"", ""IsCompleted""
    FROM ""Todos""
    WHERE ""UserId"" = @UserId
    ORDER BY ""Id"" DESC";

var todos = await _db.QueryAsync<TodoResponseDto>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
```

---

### D. Parameter List (`IN` / `ANY` Clause)
```csharp
// Sintaks PostgreSQL
string sql = "SELECT * FROM \"Todos\" WHERE \"Id\" = ANY(@Ids)";

// Atau sintaks standar ANSI SQL (IN clause)
string sqlStandard = "SELECT * FROM \"Todos\" WHERE \"Id\" IN @Ids";

var items = await _db.QueryAsync<TodoItem>(sqlStandard, new { Ids = new[] { 1, 2, 3, 4 } });
```

---

### E. Execute Command (Insert, Update, Delete)
```csharp
// 1. Insert & Ambil Generated ID (PostgreSQL RETURNING)
string insertSql = @"
    INSERT INTO ""Todos"" (""Title"", ""IsCompleted"", ""UserId"")
    VALUES (@Title, @IsCompleted, @UserId)
    RETURNING ""Id"";";

int newId = await _db.ExecuteScalarAsync<int>(new CommandDefinition(insertSql, new 
{ 
    Title = dto.Title, 
    IsCompleted = false, 
    UserId = userId 
}, cancellationToken: ct));

// 2. Update
string updateSql = @"
    UPDATE ""Todos""
    SET ""Title"" = @Title, ""IsCompleted"" = @IsCompleted
    WHERE ""Id"" = @Id AND ""UserId"" = @UserId;";

int affectedRows = await _db.ExecuteAsync(new CommandDefinition(updateSql, new 
{ 
    Id = id, 
    Title = dto.Title, 
    IsCompleted = dto.IsCompleted, 
    UserId = userId 
}, cancellationToken: ct));

// 3. Delete
string deleteSql = "DELETE FROM \"Todos\" WHERE \"Id\" = @Id AND \"UserId\" = @UserId;";
int deletedRows = await _db.ExecuteAsync(new CommandDefinition(deleteSql, new { Id = id, UserId = userId }, cancellationToken: ct));
```

---

### F. Multi-Mapping (Join Antar Tabel)
```csharp
string sql = @"
    SELECT t.""Id"", t.""Title"", t.""IsCompleted"", u.""Id"", u.""Username""
    FROM ""Todos"" t
    INNER JOIN ""Users"" u ON t.""UserId"" = u.""Id""
    WHERE t.""Id"" = @Id;";

var result = await _db.QueryAsync<TodoItem, User, TodoItem>(
    sql,
    (todo, user) =>
    {
        todo.User = user;
        return todo;
    },
    new { Id = id },
    splitOn: "Id"
);
```

---

### G. Multiple Result Sets (`QueryMultiple`)
```csharp
string sql = @"
    SELECT * FROM ""Users"" WHERE ""Id"" = @UserId;
    SELECT * FROM ""Todos"" WHERE ""UserId"" = @UserId;";

using var multi = await _db.QueryMultipleAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

var user = await multi.ReadFirstOrDefaultAsync<User>();
var todos = (await multi.ReadAsync<TodoItem>()).ToList();
```

---

### H. Transaksi di Dapper
```csharp
_db.Open(); // Pastikan koneksi terbuka sebelum inisialisasi transaksi
using var tx = _db.BeginTransaction();

try
{
    await _db.ExecuteAsync(new CommandDefinition(insertSql, newTodo, transaction: tx, cancellationToken: ct));
    await _db.ExecuteAsync(new CommandDefinition(logSql, newLog, transaction: tx, cancellationToken: ct));

    tx.Commit();
}
catch
{
    tx.Rollback();
    throw;
}
```

---

## 4. Pola Hybrid: EF Core + Dapper Bersama

Pendekatan populer dalam arsitektur modern berbasis **CQS (Command Query Separation)**:
- **Write Command (Create / Update / Delete):** Gunakan **EF Core** untuk memanfaatkan validasi model, change-tracking, lifecycle hooks, dan manajemen migrasi skema.
- **Read Query (Complex Read / High Traffic):** Gunakan **Dapper** untuk optimasi latensi, query raw SQL fleksibel, dan zero tracking overhead.

### Memakai Koneksi yang Sama Saat Runtime
```csharp
// Eksekusi Dapper menggunakan connection instance milik EF Core DbContext
using var connection = _dbContext.Database.GetDbConnection();
var data = await connection.QueryAsync<TodoResponseDto>(
    new CommandDefinition("SELECT \"Id\", \"Title\" FROM \"Todos\" WHERE \"UserId\" = @UserId",
    new { UserId = userId },
    cancellationToken: ct)
);
```
