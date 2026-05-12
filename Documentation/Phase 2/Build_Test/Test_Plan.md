# Test Plan — LawyerApp

**Version:** 1.0  
**Date:** 2026-05-12  
**Author:** Group Wed PBS 5-2 — Build & Test (Member 2)  
**Project:** LawyerApp — Secure Legal Management Back-end

---

## 1. Objectives

This document describes the testing strategy for the LawyerApp REST API. The goals are:

1. Validate that all implemented business rules behave as specified.
2. Verify that security controls (password hashing, no credential exposure in responses) are enforced.
3. Ensure the API surface contracts are stable and regression-free.
4. Provide reproducible, automated evidence of correctness for the SSDLC report.

---

## 2. Scope

| In Scope | Out of Scope |
|---|---|
| `ClientService` application logic | UI / front-end (none exists) |
| `ClientController` API endpoints | Load / stress testing |
| `UserRepository` data-access layer | Manual exploratory testing |
| `BCryptPasswordHasher` security utility | HashiCorp Vault connectivity |
| Domain entities (Client, LegalProcess, Document) | Production database (Aiven PostgreSQL) |

---

## 3. Testing Levels

### 3.1 Unit Tests

**Location:** `LawyerApp.Tests/Unit/`  
**Framework:** xUnit 2.9 · Moq 4.20 · FluentAssertions 6.12  
**Isolation:** All external dependencies (repository, hasher) are replaced with Moq mocks.

| Test Class | Covers | # Tests |
|---|---|---|
| `ClientServiceTests` | CreateClientAsync happy path, duplicate email rejection, password hash enforcement, GetAll mapping, no PasswordHash in DTO | 7 |
| `BCryptPasswordHasherTests` | Hash format, uniqueness (random salt), verify-correct, verify-wrong, verify-empty | 7 |
| `ClientTests` | Constructor property mapping, CreatedAt UTC stamp, inheritance chain | 3 |
| `LegalProcessTests` | GUID uniqueness, initial status = Open | 2 |
| `DocumentTests` | StoredFileName uniqueness, extension preservation | 2 |

**Total unit tests:** 21

### 3.2 Integration Tests

**Location:** `LawyerApp.Tests/Integration/`  
**Framework:** `Microsoft.AspNetCore.Mvc.Testing` + EF Core InMemory provider  
**Infrastructure:** `CustomWebApplicationFactory` replaces the PostgreSQL `DbContext` with an isolated in-memory database per test collection.

| Test Class | Covers | # Tests |
|---|---|---|
| `ClientControllerTests` | GET /api/client/get/all (empty + after insert), POST /api/client/create (success, no hash in response, duplicate rejection) | 4 |
| `UserRepositoryTests` | AddClientAsync, GetAllClientsAsync (TPH discriminator), EmailExistsAsync (true/false/case), GetByEmailAsync (found/not found) | 7 |

**Total integration tests:** 11

---

## 4. Test Execution

### 4.1 Local Execution

```bash
# From the repository root
cd LawyerApp

# Run all tests (unit + integration)
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj -v normal

# Run with code-coverage collection
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Run only unit tests
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Integration"
```

### 4.2 CI/CD Execution

Tests run automatically on every push and pull request to `main` and `develop` via the [build-test.yml](../../../.github/workflows/build-test.yml) GitHub Actions workflow. Results are uploaded as artifacts (`.trx` + Cobertura XML).

---

## 5. Security-Focused Test Cases

The following tests directly exercise OWASP and ASVS controls:

| Test | ASVS Control | Description |
|---|---|---|
| `CreateClientAsync_NeverStoresPlainTextPassword` | V2.1.1 | Verifies the raw password string is never sent to the repository |
| `CreateClientAsync_HashesPasswordBeforePersisting` | V2.1.1 | Confirms the BCrypt hash (not the plain text) is stored |
| `BCryptPasswordHasherTests.*` | V2.1.9 | Full coverage of bcrypt hash/verify lifecycle |
| `Create_ResponseDoesNotContainPasswordHash` | V8.3.1 | HTTP response body must not include `passwordHash` field |
| `GetAllClientsAsync_DoesNotExposePasswordHash` | V8.3.1 | `ClientDto` must not have a `PasswordHash` property |
| `GetAllClientsAsync_WhenNoClients_ReturnsEmptyArray` | V13.1.1 | Valid JSON array returned even for empty result sets |

---

## 6. Test Data Strategy

All test data is self-contained within each test class (Arrange-Act-Assert pattern). No shared state between tests. The in-memory database is scoped to the test run via `Guid.NewGuid()` in `InMemoryDbContextFactory.Create()`, preventing cross-test pollution.

---

## 7. Coverage Targets

| Layer | Target Coverage |
|---|---|
| Application Services | ≥ 90% |
| Domain Entities | ≥ 85% |
| Infrastructure (Repositories) | ≥ 80% |
| API Controllers | ≥ 75% (via integration tests) |

---

## 8. Artifact Scanning

See [Artifact_Scanning.md](Artifact_Scanning.md) for the full scanning strategy, tooling, and CI integration.

---

## 9. Pass / Fail Criteria

A build is considered **passing** if:

- All `dotnet test` runs exit with code 0.
- No vulnerable NuGet packages are found (`dotnet list package --vulnerable`).
- Trivy finds no **CRITICAL** or **HIGH** CVEs in the container image.
- Gitleaks finds no hardcoded secrets.
