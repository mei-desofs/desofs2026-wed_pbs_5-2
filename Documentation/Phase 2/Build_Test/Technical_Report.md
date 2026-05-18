# Technical Report — LawyerApp

**Version:** 1.0  
**Date:** 2026-05-12  
**Authors:** Group Wed PBS 5-2  
**Project:** LawyerApp — Secure Legal Management Back-end (DESOFS 2026)

---

## 1. Project Overview

LawyerApp is a .NET 8 ASP.NET Core REST API implementing a secure back-end for a legal consultancy firm. It follows Clean Architecture and SSDLC (Secure Software Development Lifecycle) principles.

### Technology Stack

| Component | Technology |
|---|---|
| Runtime | .NET 8 / ASP.NET Core 8 |
| Language | C# 12 |
| Architecture | Clean Architecture (Domain · Application · Infrastructure · API) |
| Database | PostgreSQL (Aiven managed cloud) |
| ORM | Entity Framework Core 8 (Npgsql) |
| Password Hashing | BCrypt.Net-Next (work factor 10) |
| Secret Management | HashiCorp Vault (via VaultSharp) |
| API Documentation | Swagger / Swashbuckle |
| Containerisation | Docker (multi-stage build) |

---

## 2. Architecture

```
LawyerApp/
├── API/                        # Controllers — HTTP boundary
│   └── Client/
│       └── ClientController.cs
├── Application/                # Use-case services (no framework deps)
│   ├── Interfaces/User/
│   │   └── IClient.cs
│   └── Services/UserAggregate/
│       └── ClientService.cs
├── Domain/                     # Core business entities & interfaces
│   ├── Aggregates/
│   │   ├── UserAggregate/      # User (abstract), Client, Lawyer, LegalAssistant
│   │   ├── DocumentAggregate/  # Document, DocCategory
│   │   └── LegalProcessAggregate/ # LegalProcess, ProcessStatus
│   └── Interfaces/Security/
│       └── IPasswordHasher.cs
├── Infrastructure/             # EF Core, repositories, BCrypt, Vault
│   ├── HashiCorp/
│   ├── Persistence/            # LawyerAppDbContext + Repositories
│   └── Security/
├── Migrations/                 # EF Core migration history
├── LawyerApp.Tests/            # Test project (xUnit)
├── Dockerfile
└── appsettings.json
```

### Domain Model Summary

- **User** (abstract, TPH discriminator): base class with `Name`, `Email`, `PasswordHash`, `CreatedAt`.
  - **Client**: adds `BillingAddress`, `PhoneNumber`.
  - **Lawyer**: adds `LicenseNumber`, `Specialization`.
  - **LegalAssistant**: specialised staff role.
- **LegalProcess**: identified by a GUID (generated in the domain to guarantee ID availability before OS folder creation). Linked to a `Lawyer` and a `Client`.
- **Document**: linked to a `LegalProcess`. Stores a random `StoredFileName` (GUID-based) to prevent path traversal and filename enumeration.

---

## 3. Security Controls

| Control | Implementation |
|---|---|
| Password hashing | BCrypt with random salt per hash (`BCryptPasswordHasher`) |
| No plain-text credentials in DB | `ClientService.CreateClientAsync` hashes before persisting |
| No credentials in API responses | `ClientDto` has no `PasswordHash` property |
| Unpredictable resource IDs | `LegalProcess.ProcessId` and `Document.StoredFileName` use `Guid.NewGuid()` |
| Secret management | Database connection string retrieved from HashiCorp Vault at startup |
| HTTPS enforcement | `app.UseHttpsRedirection()` |
| Dependency scanning | `dotnet list package --vulnerable` in CI |
| Container scanning | Trivy (CRITICAL/HIGH, ignore-unfixed) in CI |
| Secret leak detection | Gitleaks on full git history in CI |

---

## 4. Running the Application

### Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0.x |
| Docker & Docker Compose | 20.x+ |
| PostgreSQL (optional for local dev) | 15+ |

### 4.1 Option A — Local .NET CLI (with Aiven or local PostgreSQL)

**Step 1 — Configure the database connection string**

The application reads the connection string from the `ConnectionString:PostgreSQL` configuration key. Set it via environment variable (recommended) or `appsettings.Development.json`.

```bash
# Environment variable (recommended — never commit credentials)
export ConnectionString__PostgreSQL="Host=<host>;Port=5432;Database=lawyerapp;Username=<user>;Password=<password>;SSL Mode=Require"
```

Or add to `LawyerApp/appsettings.Development.json` (never commit this file):

```json
{
  "ConnectionString": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=lawyerapp;Username=postgres;Password=postgres"
  }
}
```

**Step 2 — (Optional) Configure HashiCorp Vault secrets**

If using Vault to retrieve the connection string, set user-secrets (these are stored outside the repository):

```bash
cd LawyerApp
dotnet user-secrets init
dotnet user-secrets set "VaultSettings:ServerUri"  "<vault_uri>"
dotnet user-secrets set "VaultSettings:Token"      "<vault_token>"
dotnet user-secrets set "VaultSettings:MountPoint" "<mount_point>"
dotnet user-secrets set "VaultSettings:SecretPath" "<secret_path>"
```

Then uncomment the Vault block in `Program.cs` and comment out the direct connection string line.

**Step 3 — Apply database migrations**

```bash
cd LawyerApp
dotnet ef database update
```

**Step 4 — Run the application**

```bash
dotnet run
```

The API starts at `https://localhost:7xxx` and `http://localhost:5xxx` (exact ports in `Properties/launchSettings.json`). Swagger UI is available at `https://localhost:<port>/swagger`.

---

### 4.2 Option B — Docker

**Step 1 — Build the image**

```bash
cd LawyerApp
docker build -t lawyerapp:latest .
```

**Step 2 — Run the container**

Pass the connection string as an environment variable:

```bash
docker run -p 8080:8080 \
  -e "ConnectionString__PostgreSQL=Host=<host>;Port=5432;Database=lawyerapp;Username=<user>;Password=<password>;SSL Mode=Require" \
  lawyerapp:latest
```

The API will be available at `http://localhost:8080`.

---

### 4.3 Option C — Docker Compose (recommended for local development)

Create a `docker-compose.yml` in the repository root:

```yaml
version: "3.9"
services:
  db:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: lawyerapp
      POSTGRES_USER: dev
      POSTGRES_PASSWORD: devpassword
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U dev"]
      interval: 5s
      timeout: 5s
      retries: 5

  api:
    build:
      context: LawyerApp/
    environment:
      ConnectionString__PostgreSQL: "Host=db;Port=5432;Database=lawyerapp;Username=dev;Password=devpassword"
    ports:
      - "8080:8080"
    depends_on:
      db:
        condition: service_healthy
```

```bash
docker compose up --build
```

Then run migrations from the host:

```bash
cd LawyerApp
dotnet ef database update
```

---

## 5. Running Tests

### 5.1 All tests

```bash
cd LawyerApp
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj -v normal
```

**No database or network connection is required.** Integration tests use an EF Core InMemory provider isolated per test run.

### 5.2 With code coverage

```bash
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

Coverage reports are produced in Cobertura XML format under `./coverage/`.

### 5.3 Filter by category

```bash
# Only unit tests
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Unit"

# Only integration tests
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Integration"
```

---

## 6. API Endpoints

| Method | Path | Description | Request Body | Response |
|---|---|---|---|---|
| `GET` | `/api/client/get/all` | List all registered clients | — | `ClientDto[]` |
| `POST` | `/api/client/create` | Register a new client | `CreateClientDto` | `ClientDto` |

### CreateClientDto

```json
{
  "name": "Alice Lawyer Client",
  "email": "alice@example.com",
  "password": "S3cur3Password!",
  "billingAddress": "Rua das Flores 1, Lisboa",
  "phoneNumber": "912345678"
}
```

### ClientDto (response — no password field)

```json
{
  "name": "Alice Lawyer Client",
  "email": "alice@example.com",
  "billingAddress": "Rua das Flores 1, Lisboa",
  "phoneNumber": "912345678"
}
```

---

## 7. CI/CD Pipeline Summary

Full design, mapping to the rubric, and the GitHub setup checklist are in
[Pipeline.md](Pipeline.md).

| Workflow | Trigger | Jobs |
|---|---|---|
| `build-test.yml` | push / PR to main, develop, manual | Build (Release) + xUnit tests + coverage + publish artifact |
| `codeql.yml` | push / PR / weekly / manual | GitHub CodeQL SAST (C#, `security-and-quality`) |
| `semgrep.yml` | push / PR / weekly / manual | Semgrep SAST (`p/default`, `p/security-audit`, `p/csharp`, `p/dockerfile`, `p/secrets`) |
| `sonarcloud.yml` | manual (until `SONAR_TOKEN` configured) | Optional SonarCloud analysis — see Pipeline.md §5 |
| `dependency-review.yml` | PR | GitHub Dependency Review on PR diffs (fail on `high` or GPL/AGPL) |
| `security-scan.yml` | push / PR / weekly / manual | SCA (`dotnet list --vulnerable`), .NET analyzers, Trivy image scan |
| `trivy-config.yml` | push / PR / manual | Trivy filesystem + IaC/Dockerfile config scan (fails on CRITICAL/HIGH) |
| `sbom.yml` | push / PR / weekly / manual | CycloneDX SBOM (solution) + Syft SBOM (image) + Grype scan |
| `secrets-scan.yml` | push / PR / weekly / manual | Gitleaks (full history) + TruffleHog (verified-only) |
| `config-validation.yml` | push / PR / manual | Hadolint + actionlint + yamllint + JSON/appsettings validation + `dotnet format` |
| `dependabot.yml` (config) | weekly schedule | NuGet + GitHub Actions + Docker base-image updates |

---

## 8. Known Limitations & Future Work

| Item | Description |
|---|---|
| Authentication / Authorization | JWT / role-based access control not yet implemented |
| Remaining aggregates | `DocumentController`, `LegalProcessController` placeholders exist but are not implemented |
| Input validation | `[Required]` / `[MaxLength]` data annotations not yet applied to DTOs |
| Rate limiting | Not implemented |
| Audit logging | No structured audit trail for create/update operations |
