# .NET Core Web API Starter

A robust, enterprise-ready RESTful Web API built on **ASP.NET Core 8 (.NET 8)**, featuring hybrid data access with **Entity Framework Core** and **Dapper**, distributed caching with **Redis** (with graceful database fallback), native **Rate Limiting**, automated schema migrations, standardized JSON response envelopes, and structured health check probes for cloud-native orchestration.

---

## Architecture & Core Technologies

- **Framework**: ASP.NET Core 8 (`net8.0`) Web API.
- **Data Access Layer (Hybrid CQS)**:
  - **Entity Framework Core 8**: Domain model validation, transactional mutations, and database schema migrations.
  - **Dapper 2.1**: High-throughput read queries, projection, and zero-tracking overhead.
- **Database**: PostgreSQL 17 with snake_case naming conventions.
- **Cache Engine**: Redis 7 (Cache-aside pattern with graceful database bypass on timeout or outage).
- **Rate Limiting & Abuse Protection**:
  - Native ASP.NET Core 8 rate limiting (`Microsoft.AspNetCore.RateLimiting`).
  - Strict policy (`auth-policy`: 10 req/min) on authentication endpoints to prevent brute-force attacks.
  - Public policy (`public-policy`: 60 req/min) on catalog endpoints with standardized RFC `429 Too Many Requests` and `Retry-After` headers.
- **Authentication & Security**:
  - JWT Bearer Token stored in `HttpOnly; Secure; SameSite=Strict` cookies.
  - BCrypt password hashing.
  - OWASP BOLA/IDOR mitigation with user resource ownership validation (ABAC).
  - Swagger UI integrated with Bearer token authorization.
- **Validation**: FluentValidation with structured `422 Unprocessable Entity` error mapping.
- **Response Format**: Standardized enterprise envelope format (`{ message, data, meta }`) with mandatory `camelCase` keys.
- **Health Checks**: Segregated probes (`/health/live`, `/health/ready`, `/health/startup`, `/health/dependencies`) to prevent cascading pod restart loops.
- **Containerization**: Multi-stage Dockerfile (minimal Alpine runtime, non-root `app` user) and Docker Compose orchestration.
- **CI/CD**: GitHub Actions pipeline for automated restore, build, testing (with OpenCover code coverage), and Docker image build validation.

---

## Directory Structure

```text
├── Controllers/            # API Controllers (Auth, Book, Todo, Health)
├── Data/                   # EF Core DbContext & entity mappings
├── Dto/                    # DTOs & response envelopes (ApiResponse, PagedData, ApiError)
├── Migrations/             # EF Core database migrations
├── Models/                 # Domain entity models (User, Book, Todo, Author, Category, etc.)
├── Requests/               # FluentValidation validators
├── Services/               # Business services & middlewares (Cache, Correlation, Auth, Book, Todo)
├── guides/                 # Technical guides & seed data (Controllers, EF Core vs Dapper, SQL dump)
├── tests/                  # Unit test suite (xUnit, Moq, FluentAssertions, Coverlet)
├── .github/workflows/      # GitHub Actions CI workflow (ci.yml)
├── Dockerfile              # Multi-stage container build (build, migrator, final)
├── docker-compose.yml      # Orchestration for PostgreSQL 17 + Redis 7 + Migration Runner + API
├── .env.example            # Environment variables template
└── bookism.http            # HTTP request scratchpad for endpoint testing
```

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Docker Engine (v24+) & Docker Compose (v2+)
- [Git](https://git-scm.com/)

---

### Running with Docker Compose (Recommended)

1. **Prepare the environment configuration**:
   ```bash
   cp .env.example .env
   ```
   *Adjust port or credentials in `.env` if necessary (default API port: `5164`).*

2. **Build and start all containers**:
   ```bash
   docker compose up -d --build
   ```

3. **Verify container status**:
   ```bash
   docker compose ps
   ```
   *Startup sequence:*
   - `postgres` (17-alpine) and `redis` (7-alpine) start first and undergo health checks.
   - The ephemeral `migration` container applies pending EF Core migrations (`dotnet-ef database update`) to PostgreSQL, then exits cleanly with code `0`.
   - The `api` container starts once database, Redis, and migrations are ready.

4. **Verify Health Check Probes**:
   ```bash
   # Liveness probe (verifies application process only)
   curl -i http://localhost:5164/health/live

   # Readiness probe (verifies database and Redis availability)
   curl -i http://localhost:5164/health/ready
   ```

5. **Import Starter Seed Data (Optional)**:
   ```bash
   docker exec -i bookism_postgres psql -U postgres -d bookshelf < guides/seed_data.sql
   ```

6. **Stop Services**:
   ```bash
   docker compose down
   # To remove persistent database and cache volumes as well:
   docker compose down -v
   ```

---

### Running Locally on the Host Machine

1. **Start database and cache dependencies**:
   ```bash
   docker compose up -d postgres redis
   ```

2. **Apply database schema migrations**:
   ```bash
   dotnet ef database update
   ```

3. **Restore starter seed data (Optional)**:
   ```bash
   PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres -d bookshelf -f guides/seed_data.sql
   ```

4. **Restore dependencies and run the API**:
   ```bash
   dotnet restore
   dotnet run
   ```
   The API will listen on the configured endpoint URLs (e.g., `http://localhost:5164` or `https://localhost:7164`).
   Swagger documentation is available in development mode at `/swagger`.

---

## Running Tests

This project includes a comprehensive unit test suite utilizing **xUnit**, **Moq**, **FluentAssertions**, and **Coverlet** for coverage collection.

```bash
# Run all unit tests
dotnet test

# Run tests and collect code coverage (OpenCover format)
dotnet test --collect:"XPlat Code Coverage"
```

---

## Setting Up Git Repository

To initialize Git in this local repository and connect it to a remote GitHub repository:

```bash
# 1. Initialize local Git repository
git init

# 2. Stage all files (sensitive files and build artifacts are excluded by .gitignore)
git add .

# 3. Create the initial commit
git commit -m "feat: initial commit with .NET 8 Web API, multi-stage Dockerfile, and CI pipeline"

# 4. Set the default branch to main
git branch -M main

# 5. Add your remote GitHub repository
# git remote add origin https://github.com/<username>/<repo-name>.git

# 6. Push code to GitHub
# git push -u origin main
```

---

## CI Pipeline (Continuous Integration)

The CI workflow is defined in [`.github/workflows/ci.yml`](.github/workflows/ci.yml) and triggers automatically on every `push` and `pull_request` targeting the `main`/`master` branches:

1. **Job `build-and-test`**:
   - Provisions .NET 8 SDK runner (`ubuntu-latest`).
   - Caches NuGet dependencies for faster execution.
   - Compiles the entire solution in `Release` mode (`dotnet build`).
   - Executes unit tests with Code Coverage collection (`opencover` and `cobertura`).
   - Uploads test coverage reports as a downloadable CI artifact.
2. **Job `docker-validation`**:
   - Leverages Docker Buildx and GitHub Actions cache (`type=gha`).
   - Validates multi-stage Dockerfile builds for both the `final` (runtime image) and `migrator` (migration runner) targets to prevent packaging failures prior to deployment.

---

## API Standards & Technical Documentation

For in-depth architectural patterns, coding conventions, and starter data, refer to the [`guides/`](guides/) directory:
- **[guides/cheatsheet-controllers.md](guides/cheatsheet-controllers.md)**: RESTful URL conventions, Response Envelopes, Error Handling, Authentication, and Health Check Probes.
- **[guides/cheatsheet-ef-dapper.md](guides/cheatsheet-ef-dapper.md)**: Hybrid CQS architecture, Single vs. Split Queries (`AsSplitQuery`), and CancellationTokens via `CommandDefinition`.
- **[guides/seed_data.sql](guides/seed_data.sql)**: Starter SQL seed dump with initial users, books, authors, categories, and sequences.
- **[bookism.http](bookism.http)**: Scratchpad for testing API endpoints directly from your IDE (VS Code / JetBrains Rider / Visual Studio).

---

## License
This project is distributed under internal company terms and licensing.
