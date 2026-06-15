# DAST Report — Sprint 2 (Phase 3)

## 1. Introduction

This report documents the DAST (Dynamic Application Security Testing) re-run performed against the Sprint 2 build of the LawyerApp. It extends the [Sprint 1 DAST report](../../Phase%202/DAST/DAST-ZAP-Report.md) with a Sprint 1 → Sprint 2 delta, verifying whether previously identified issues were resolved and documenting newly discovered findings.

---

## 2. Scan Configuration

| Parameter | Value |
|---|---|
| Tool | OWASP ZAP (ghcr.io/zaproxy/zaproxy:stable) |
| Scan type | API Scan (`zap-api-scan.py`) |
| Target | `http://localhost:5000` |
| OpenAPI spec | `http://localhost:5000/swagger/v1/swagger.json` |
| Execution | Automated — `.github/workflows/security-scan.yml` (`dast` job) |
| Date | 2026-06-14 |
| URLs imported from spec | 11 |
| Total URLs scanned | 49 |
| Security checks run | 117+ |

**Key improvement over Sprint 1:** Sprint 1 used `zap-baseline.py` against a local stub with no OpenAPI spec. ZAP only discovered 3 default URLs (`/`, `/robots.txt`, `/sitemap.xml`). Sprint 2 uses `zap-api-scan.py` with the Swagger spec, giving ZAP full knowledge of the API surface and resulting in meaningful coverage of all real endpoints.

---

## 3. Sprint 1 → Sprint 2 Delta

| Finding | Sprint 1 | Sprint 2 | Status |
|---|---|---|---|
| HTTP Only Site (Medium) | ⚠️ WARN | ✅ PASS | **Closed** |
| Storable and Cacheable Content (Info) | ⚠️ WARN | ✅ PASS | **Closed** |
| X-Content-Type-Options Header Missing | Not tested | ⚠️ WARN | **New** |
| Cross-Origin-Resource-Policy Header Missing | Not tested | ⚠️ WARN | **New** |
| Server Error (500) on `/api/auth/login` | Not tested | ⚠️ WARN | **New** |
| Unexpected Content-Type on Swagger UI | Not tested | ⚠️ WARN | New (low priority) |

---

## 4. Sprint 2 Findings

### 4.1 WARN — X-Content-Type-Options Header Missing [10021]

- **Description:** The `X-Content-Type-Options: nosniff` header is absent from API responses. Without it, browsers may MIME-sniff responses and interpret them as a different content type than declared, enabling content injection attacks.
- **Affected endpoints:**
  - `http://localhost:5000/api/auth/register`
  - `http://localhost:5000/api/auth/login`
  - `http://localhost:5000/swagger/v1/swagger.json`
- **Recommendation:** Add the header globally via ASP.NET Core middleware:
  ```csharp
  app.Use(async (ctx, next) => {
      ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
      await next();
  });
  ```
- **Status:** Open

---

### 4.2 WARN — Cross-Origin-Resource-Policy Header Missing or Invalid [90004]

- **Description:** The `Cross-Origin-Resource-Policy` header is absent. This header controls which origins can load the resource, preventing cross-site leaks via `<img>`, `<script>`, and `<iframe>` inclusion.
- **Affected endpoints:**
  - `http://localhost:5000/api/auth/register`
  - `http://localhost:5000/api/auth/login`
  - `http://localhost:5000/swagger/v1/swagger.json`
- **Recommendation:** Add `Cross-Origin-Resource-Policy: same-origin` for API endpoints.
- **Status:** Open

---

### 4.3 WARN — Server Error Response on Login [100000]

- **Description:** `POST /api/auth/login` returned HTTP 500 when ZAP submitted a malformed request body (auto-generated from the OpenAPI schema). The server returned an unhandled exception instead of a 400 Bad Request.
- **Affected endpoint:** `http://localhost:5000/api/auth/login`
- **Note:** This is triggered by invalid input and is primarily an input validation / error handling issue rather than a direct vulnerability. However, a 500 response may leak stack traces or internal details depending on the error handling middleware configuration.
- **Recommendation:** Ensure the login endpoint validates input and returns 400 for malformed payloads. Verify the `ExceptionHandlingMiddleware` suppresses internal details in non-Development environments.
- **Status:** Open — requires verification

---

### 4.4 WARN — Unexpected Content-Type on Swagger UI [100001]

- **Description:** ZAP expected an API response content type but received `text/html` from the Swagger UI pages (`/swagger/index.html`, `/swagger/`).
- **Affected endpoints:** Swagger UI only
- **Assessment:** False positive. This is expected behaviour — Swagger UI serves HTML. Swagger UI should not be exposed in production environments.
- **Status:** Accepted / False positive

---

## 5. Closed Findings (from Sprint 1)

### 5.1 HTTP Only Site — CLOSED

Sprint 1 flagged the app as HTTP-only (Medium). In Sprint 2, this finding no longer appears. The app is configured with `UseHttpsRedirection()` middleware.

### 5.2 Storable and Cacheable Content — CLOSED

Sprint 1 flagged missing cache control headers on several URLs. In Sprint 2, this check passes across all scanned endpoints.

---

## 6. Security Checks Passed (117)

All major vulnerability categories returned PASS, including:

- SQL Injection (PostgreSQL, MySQL, MSSQL, Oracle, Hypersonic — time-based)
- Cross-Site Scripting (Reflected, Persistent, DOM-based)
- Remote OS Command Injection
- Path Traversal
- Remote File Inclusion
- XML External Entity (XXE)
- Server-Side Template Injection
- CSRF Token Absence
- Information Disclosure (debug messages, sensitive data in URLs)
- Private IP Disclosure
- Log4Shell, Spring4Shell
- Authentication and Session Management

---

## 7. Summary

| Category | Count |
|---|---|
| FAIL | 0 |
| WARN (open) | 3 |
| WARN (false positive) | 1 |
| PASS | 117 |

The application shows a significantly improved security posture compared to Sprint 1. No high or critical vulnerabilities were found. The three open WARN findings are related to missing HTTP security response headers and an input validation gap on the login endpoint — both are addressable without architectural changes.

---

## 8. Evidence

The full HTML and JSON scan reports are available as the `dast-zap-report` artifact in the GitHub Actions run for the `Security Scan` workflow.

- Pipeline: `.github/workflows/security-scan.yml` — `dast` job
- Artifact: `dast-zap-report` (retained 30 days per run)
