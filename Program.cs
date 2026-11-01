using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using bookism.Controllers;
using bookism.Data;
using bookism.Dto;
using bookism.Services;
using bookism.Validators;

var builder = WebApplication.CreateBuilder(args);

// DB Postgres with snake_case naming convention
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
       .UseSnakeCaseNamingConvention());

// Caching: Redis Distributed Cache
var redisConnectionStr = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var config = ConfigurationOptions.Parse(redisConnectionStr!);
    config.AbortOnConnectFail = false;
    config.ConnectTimeout = 3000;
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = redisConnectionStr;
    opt.InstanceName = "bookism:";
});

builder.Services.AddSingleton<ICacheService, CacheService>();

// Dependency Injection Service
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IBookService, BookService>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

// Built-in .NET 8 Rate Limiter (Protects auth and public endpoints against brute force/DoS)
builder.Services.AddRateLimiter(opt =>
{
    // 1. Strict policy for sensitive mutation/auth endpoints (10 requests per minute)
    opt.AddFixedWindowLimiter("auth-policy", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 0;
    });

    // 2. Standard policy for general public reads (60 requests per minute)
    opt.AddFixedWindowLimiter("public-policy", policy =>
    {
        policy.PermitLimit = 60;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 5;
    });

    // Standardized 429 response matching organization error envelope and RFC Retry-After header
    opt.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);
        }

        var traceId = CorrelationMiddleware.GetTraceId(context.HttpContext);
        var errorResponse = new ApiErrorResponse(new ApiErrorDetail(
            Message: "Terlalu banyak permintaan (Rate limit exceeded). Silakan coba lagi nanti.",
            TraceId: traceId
        ));

        await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
    };
});

// Auth via HttpOnly Cookie
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("jwt", out var token))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Controllers configuration with global Exception Filter
builder.Services.AddControllers(opt =>
{
    opt.Filters.Add<ApiExceptionFilter>();
});

// Configure standardized 422 Unprocessable Entity response for model validation failures
builder.Services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.InvalidModelStateResponseFactory = context =>
    {
        var traceId = CorrelationMiddleware.GetTraceId(context.HttpContext);
        var details = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(err => new ValidationErrorDetail(
                Field: System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(kvp.Key),
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bookism API", Version = "v1" });

    // Enable Bearer token authorization button in Swagger UI for developer convenience
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Format: 'Bearer {token}' or via HttpOnly cookie.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS credentials support
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Correlation ID middleware to inject/propagate X-Request-ID
app.UseMiddleware<CorrelationMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
