# ASP.NET Core Controller, DTO & Health Check Cheatsheet

Panduan perancangan Controller, Action Result, Binding, Authentication, Validation, Error Handling, Response Envelope, dan Service Health Check pada ASP.NET Core Web API (.NET 8+) yang selaras dengan **Standar Internal RESTful API & Service Health Check**.

---

## 1. Aturan URL & Routing Standar

Setiap endpoint API internal wajib mengikuti konvensi URL baku:

| Aturan | Standar (Benar) | Salah | Keterangan |
| :--- | :--- | :--- | :--- |
| **Casing** | `/api/v1/user-profiles` | `/api/v1/userProfiles` | Wajib `kebab-case` huruf kecil |
| **Kuantitas** | `/api/v1/transactions` | `/api/v1/transaction` | Gunakan kata benda jamak (plural) |
| **Trailing Slash** | `/api/v1/users` | `/api/v1/users/` | Dilarang menggunakan trailing slash |
| **Tanpa Kata Kerja** | `POST /api/v1/orders` | `POST /api/v1/createOrder` | Gunakan HTTP Method sebagai penentu aksi |
| **Nesting Kedalaman** | `/api/v1/users/{userId}/orders` | `/api/v1/users/{u}/orders/{o}/items/{i}` | Maksimal kedalaman sub-resource 2 tingkat |
| **Base Path Gateway** | `/{domain}/v{major}/{resource}` | `/data/get` | Contoh domain: `public`, `operational`, `masterdata` |

---

## 2. Struktur Dasar Controller

```csharp
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _service;

    public ItemsController(IItemService service)
    {
        _service = service;
    }
}
```

### Penjelasan Komponen Kunci
- `[ApiController]`: Mengaktifkan validasi model otomatis, inferensi binding attribute, dan otomatis mengevaluasi ModelState.
- `ControllerBase`: Base class ringan tanpa view engine MVC (khusus RESTful Web API).
- Target **Richardson Maturity Level 2**: Resource URI eksplisit + HTTP Verb yang tepat + Status Code yang benar.

---

## 3. Parameter Binding & CancellationToken

Gunakan atribut binding eksplisit untuk menentukan sumber payload request HTTP, serta wajib sertakan `CancellationToken`:

```csharp
[HttpPost("submit/{category}")]
public async Task<IActionResult> Process(
    [FromRoute] string category,         // Path parameter identitas
    [FromQuery] int page,                // Query string URL (?page=1)
    [FromBody] CreateItemDto dto,        // JSON request body (camelCase)
    [FromHeader("X-Request-ID")] string reqId, // Correlation/Trace ID
    [FromHeader("Idempotency-Key")] string? ik, // Wajib pada mutasi kritis (pembayaran/transaksi)
    [FromServices] ILogger log,          // Inject service langsung ke action method
    [FromForm] IFormFile file,           // Multipart/form-data
    CancellationToken ct = default       // Membatalkan query DB bila koneksi client diputus
)
{
    var result = await _service.ProcessAsync(category, dto, ct);
    return Ok(result);
}
```

---

## 4. Standar Response Envelope & JSON Naming

> [!IMPORTANT]
> **Semua key JSON wajib berformat `camelCase`**. Dilarang menggunakan format `snake_case` atau `PascalCase` pada API output. Dilarang membuat format response sendiri per controller.

### A. Format Sukses Tunggal & List
```json
// Single Resource Response
{
  "message": "Data berhasil diambil",
  "data": {
    "id": 1,
    "title": "Belajar .NET",
    "isCompleted": true
  },
  "meta": {
    "timestamp": "2026-10-31T17:00:00.0000000Z",
    "version": "1.0.0"
  }
}
```

Untuk list, sematkan objek `pagination` di dalam `data` bersama array `items`:
```json
// Collection / List Response
{
  "message": "Data berhasil diambil",
  "data": {
    "items": [
      { "id": 1, "title": "Beli kopi", "isCompleted": false }
    ],
    "pagination": {
      "page": 1,
      "limit": 10,
      "totalPages": 5,
      "totalData": 50
    }
  },
  "meta": {
    "timestamp": "2026-10-31T17:00:00.0000000Z",
    "version": "1.0.0"
  }
}
```

### B. Definisi Record C# (DTO Envelopes)
```csharp
namespace App.Dto;

public record ApiMeta(string Timestamp, string Version);

public record PaginationMeta(int Page, int Limit, int TotalPages, int TotalData);

public record PagedData<T>(IEnumerable<T> Items, PaginationMeta Pagination);

public record ApiResponse<T>(string Message, T? Data, ApiMeta Meta)
{
    public static ApiResponse<T> Create(string message, T? data, string version = "1.0.0") =>
        new(message, data, new ApiMeta(DateTime.UtcNow.ToString("o"), version));
}
```

### C. Konfigurasi Serializer di `Program.cs`
ASP.NET Core menggunakan `camelCase` secara default. Namun, pastikan policy terkonfigurasi dengan jelas:
```csharp
using System.Text.Json;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
```

---

## 5. Standar Error Payload & Status Code

Format response error harus terstruktur seragam dan wajib menyertakan `traceId` (`X-Request-ID`):

```json
{
  "error": {
    "message": "Validasi data gagal",
    "traceId": "c4b9b940-02ab-4de0-8e50-938b812ef667",
    "details": [
      {
        "field": "title",
        "issue": "Judul tidak boleh kosong"
      }
    ]
  }
}
```

### Definisi Model C# untuk Error
```csharp
namespace App.Dto;

public record ValidationErrorDetail(string Field, string Issue);

public record ApiErrorDetail(string Message, string TraceId, IEnumerable<ValidationErrorDetail>? Details = null);

public record ApiErrorResponse(ApiErrorDetail Error);
```

### Matriks Status Code
| HTTP Code | Arti Semantik | Skenario Pemakaian |
| :--- | :--- | :--- |
| **200 OK** | Sukses | `GET` sukses, `PUT`/`PATCH` sukses mengembalikan data terbaru |
| **201 Created** | Dibuat | `POST` sukses membuat resource baru (wajib sertakan header `Location`) |
| **202 Accepted** | Diterima | Proses asinkron yang butuh waktu (wajib sertakan header `Location` status job) |
| **204 No Content** | Sukses Kosong | `PUT` atau `DELETE` sukses tanpa mengembalikan body data |
| **400 Bad Request** | Format Salah | Payload malformed atau sintaks JSON tidak terbaca |
| **401 Unauthorized** | Tidak Terautentikasi | Token tidak ada, signature invalid, atau token expired |
| **403 Forbidden** | Ditolak Akses | Token valid, namun gagal lolos pengecekan hak akses/RBAC/ABAC |
| **404 Not Found** | Tidak Ada | Resource yang diminta tidak ditemukan di database |
| **409 Conflict** | Konflik State | Duplikasi data unique (misal email / username sudah dipakai) |
| **422 Unprocessable** | Validasi Gagal | Format JSON valid, tapi validasi aturan bisnis gagal (FluentValidation) |
| **429 Too Many Requests**| Rate Limit | Melebihi kuota request (sertakan header `Retry-After`) |
| **500 Server Error**| Internal Error | Bug sistem (Dilarang membocorkan stack trace / raw query DB) |
| **503 Service Unavailable**| Down / Maintenance| Layanan sedang down atau dependency kritis tidak siap |

> [!IMPORTANT]
> **Disiplin Status Code:** Bedakan secara tegas antara `401` (siapa kamu?) vs `403` (kamu dilarang masuk sini), serta `400` (JSON rusak) vs `422` (data valid JSON tapi melanggar rule bisnis).

---

## 6. Validasi Model (FluentValidation & Custom 422 Factory)

Gunakan **FluentValidation** untuk memisahkan logika validasi dari DTO, dan mapping kesalahan ke `422 Unprocessable Entity`:

### A. Validator
```csharp
using FluentValidation;

public class TodoCreateDtoValidator : AbstractValidator<TodoCreateDto>
{
    public TodoCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Judul tidak boleh kosong")
            .MaximumLength(100).WithMessage("Judul maksimal 100 karakter");
    }
}
```

### B. Standardisasi Error 422 di `Program.cs`
```csharp
builder.Services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.InvalidModelStateResponseFactory = context =>
    {
        var traceId = context.HttpContext.Request.Headers["X-Request-ID"].FirstOrDefault() 
                      ?? context.HttpContext.TraceIdentifier;

        var details = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(err => new ValidationErrorDetail(
                Field: JsonNamingPolicy.CamelCase.ConvertName(kvp.Key),
                Issue: string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Field tidak valid" : err.ErrorMessage
            )))
            .ToList();

        var errorResponse = new ApiErrorResponse(new ApiErrorDetail(
            Message: "Validasi data gagal",
            TraceId: traceId,
            Details: details
        ));

        return new UnprocessableEntityObjectResult(errorResponse);
    };
});
```

---

## 7. Keamanan & Autentikasi (JWT & Cookie)

- **JWT RS256 Umur Pendek (~15 menit)**: Disimpan di cookie `HttpOnly; Secure; SameSite=Strict` — **tidak pernah disimpan di `localStorage`**.
- **Cek Kepemilikan Resource (ABAC/BOLA Defense)**: Selalu verifikasi bahwa resource yang diakses benar-benar milik `UserId` dari token JWT, bukan hanya mengecek role.

```csharp
// 1. Set Cookie Aman saat Login
Response.Cookies.Append("jwt", token, new CookieOptions
{
    HttpOnly = true,             // Mitigasi XSS
    Secure = true,               // Hanya lewat HTTPS
    SameSite = SameSiteMode.Strict, // Proteksi CSRF penuh
    Expires = DateTime.UtcNow.AddMinutes(15)
});

// 2. Proteksi Endpoint dengan ABAC di Action
[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(int id, CancellationToken ct)
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    // Pastikan pengecekan ID sekaligus UserId (BOLA protection)
    var deleted = await _service.DeleteAsync(id, userId, ct);
    if (!deleted)
    {
        return NotFound(new ApiErrorResponse(new ApiErrorDetail("Data tidak ditemukan", TraceId)));
    }

    return Ok(ApiResponse<object>.Create("Data berhasil dihapus", null));
}
```

---

## 8. Service Health Check Probes (Kubernetes / Orchestrator)

Jangan menggabungkan seluruh health check dalam satu endpoint biner `OK/KO`. Pisahkan probe berdasarkan keputusan tindakan yang dipicu:

### A. Klasifikasi Probe
| Endpoint | Pertanyaan | Keputusan Tindakan | Akses Dependency? |
| :--- | :--- | :--- | :--- |
| `/health/live` | *Should I keep running?* | Restart proses / Pod | **TIDAK BOLEH** menyentuh external dependency |
| `/health/ready` | *Am I ready for traffic?* | Masuk/keluar dari pool load balancer | **YA**, hanya dependency **kritis** (DB) |
| `/health/startup` | Sudah selesai inisialisasi? | Menunda probe liveness saat proses warm-up | Sesuai kebutuhan inisialisasi |
| `/health/dependencies` | Komponen mana yang bermasalah? | Dashboard internal & alerting | **YA**, detail — wajib diproteksi akses internal |

> [!CAUTION]
> **Larangan Keras:** Liveness probe (`/health/live`) yang ikut mengecek database akan menyebabkan **Cascading Failure (Restart Loop Massal)**: satu gangguan transient pada database akan membunuh seluruh instance aplikasi secara bersamaan!

### B. Kontrak Response Health Check
Gunakan spektrum `UP` / `DEGRADED` / `DOWN`:
```json
{
  "status": "UP",
  "service": "bookism-api",
  "version": "1.0.0",
  "timestamp": "2026-10-31T17:00:00+07:00",
  "checks": {
    "database": "UP",
    "redis": "UP",
    "externalMail": "DEGRADED"
  }
}
```

- `200 OK`: Sehat & siap menerima traffic.
- `206 Partial Content / Degraded`: Dependency non-kritis bermasalah, aplikasi tetap dapat beroperasi dalam fallback mode.
- `503 Service Unavailable`: Dependency kritis gagal, instance harus dikeluarkan dari pool routing load balancer.

### C. Registrasi Native ASP.NET Core Health Checks
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, 
               name: "database", 
               tags: new[] { "ready" });

var app = builder.Build();

// Liveness: Ringan, tanpa sentuh DB
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Tidak menjalankan check eksternal apa pun
});

// Readiness: Cek koneksi DB kritis
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```
