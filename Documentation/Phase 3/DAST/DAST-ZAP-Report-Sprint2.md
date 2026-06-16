So # DAST Report — Sprint 2 (Phase 3)

## 1. Introduction

This report documents the DAST re-run performed against the Sprint 2 build of the LawyerApp (`feature/initial_template` branch). It extends the [Sprint 1 DAST report](../../Phase%202/DAST/DAST-ZAP-Report.md) with a Sprint 1 → Sprint 2 delta, verifying whether previously identified issues were resolved and documenting newly discovered findings.

---

## 2. Scan Configuration

| Parameter | Value |
|---|---|
| Tool | OWASP ZAP 2.17.0 |
| Scan type | API Scan (`zap-api-scan.py`) |
| Target | `http://localhost:5000` |
| OpenAPI spec | `http://localhost:5000/swagger/v1/swagger.json` |
| Execution | Automated — `.github/workflows/security-scan.yml` (`dast` job) |
| Date | 2026-06-16 |
| Branch | `feature/initial_template` (Sprint 2 build) |

**Key improvement over Sprint 1:** Sprint 1 used `zap-baseline.py` against a local stub with no OpenAPI spec — ZAP only discovered 3 default URLs. Sprint 2 uses `zap-api-scan.py` with the full Swagger spec, giving ZAP knowledge of all real API endpoints and producing meaningful coverage across all controllers.

---

## 3. Sprint 1 → Sprint 2 Delta

| Finding | Sprint 1 | Sprint 2 | Status |
|---|---|---|---|
| HTTP Only Site (Medium) | ⚠️ Present | ✅ Not found | **Closed** |
| Storable and Cacheable Content (Warn) | ⚠️ Present | ℹ️ Informational only | **Reduced** |
| Server Error 500 on login | ⚠️ Present (Sprint 1 base) | ✅ Returns 400 now | **Closed** |
| X-Content-Type-Options Header Missing | ❌ Not tested | ⚠️ Low | **New** |
| Cross-Origin-Resource-Policy Header Missing | ❌ Not tested | ⚠️ Low | **New** |
| Unexpected Content-Type (Swagger UI) | ❌ Not tested | ℹ️ Informational | **New (false positive)** |

---

## 4. Findings

### 4.1 Low — X-Content-Type-Options Header Missing [10021]

- **Risk:** Low (Medium confidence)
- **CWE:** 693
- **Description:** The `X-Content-Type-Options: nosniff` header is absent from API responses. Without it, browsers may MIME-sniff responses and interpret them as a different content type than declared.
- **Affected endpoint:** `GET /swagger/v1/swagger.json`
- **Recommendation:** Add globally via ASP.NET Core middleware:
  ```csharp
  app.Use(async (ctx, next) => {
      ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
      await next();
  });
  ```
- **Status:** Open

---

### 4.2 Low — Cross-Origin-Resource-Policy Header Missing [90004]

- **Risk:** Low (Medium confidence)
- **CWE:** 693
- **Description:** The `Cross-Origin-Resource-Policy` header is absent, potentially allowing other origins to load resources via `<img>`, `<script>`, or `<iframe>` tags, enabling side-channel attacks.
- **Affected endpoint:** `GET /swagger/v1/swagger.json`
- **Recommendation:** Add `Cross-Origin-Resource-Policy: same-origin` to API responses.
- **Status:** Open

---

### 4.3 Informational — Unexpected Content-Type on Swagger UI [100001]

- **Risk:** Informational (High confidence)
- **Description:** ZAP received `text/html` from the Swagger UI pages (`/swagger/`, `/swagger/index.html`), which it did not expect from an API endpoint.
- **Assessment:** False positive. This is expected — Swagger UI serves HTML. Swagger UI should not be exposed in production environments.
- **Status:** Accepted / False positive

---

### 4.4 Informational — Client Error Responses [100000]

- **Risk:** Informational (High confidence)
- **Description:** Various endpoints returned 401 (unauthenticated access correctly blocked) and 404 (no root endpoint, expected for a REST API).
- **Assessment:** Expected behaviour. Protected endpoints correctly return 401. Root and non-existent paths correctly return 404.
- **Notable:** All protected endpoints (`/api/client/get/all`, `/api/processes`, `/api/documents/*`, `/api/audit/*`) returned 401 — authentication is enforced correctly.
- **Status:** Informational — no action required

---

### 4.5 Informational — Authentication Request Identified [10111]

- **Risk:** Informational (High confidence)
- **Description:** ZAP correctly identified `POST /api/auth/login` as the authentication endpoint (parameters: `email`, `password`).
- **Assessment:** Informational only. Confirms the login endpoint is functioning and reachable.
- **Status:** Informational — no action required

---

### 4.6 Informational — Non-Storable Content [10049]

- **Risk:** Informational (Medium confidence)
- **Description:** Protected endpoints (returning 401) include cache-control directives preventing their storage in caches.
- **Assessment:** Correct and expected security behaviour — authenticated responses should not be cached.
- **Status:** Informational — no action required

---

## 5. Closed Findings (from Sprint 1)

### 5.1 HTTP Only Site — CLOSED
Sprint 1 flagged the app as HTTP-only (Medium). In Sprint 2 this finding does not appear. The app is configured with `UseHttpsRedirection()` middleware.

### 5.2 Server Error 500 on Login — CLOSED
The previous scan (against Sprint 1 base code) produced a 500 on `POST /api/auth/login` when ZAP submitted malformed input. Sprint 2 correctly returns **400 Bad Request**, indicating input validation is now in place.

### 5.3 Storable and Cacheable Content — Reduced
Sprint 1 flagged this as a WARN on multiple endpoints. In Sprint 2 it only appears as Informational on `/swagger/v1/swagger.json` (a static file with no sensitive data). All API endpoints correctly use non-storable cache directives.

---

## 6. Summary

| Risk Level | Count |
|---|---|
| High | 0 |
| Medium | 0 |
| Low | 2 |
| Informational | 4 |
| **Total** | **6** |

No high or critical vulnerabilities were found. The two Low findings relate to missing HTTP security response headers (`X-Content-Type-Options`, `Cross-Origin-Resource-Policy`) and are addressable with minimal middleware changes. The Sprint 2 build shows a clear improvement over Sprint 1 in both coverage (full API surface scanned vs. 3 default URLs) and security posture (500 errors resolved, authentication enforced on all protected endpoints).

---

## 7. Evidence

Full scan reports are stored at:
- [`dast-zap-report/zap-report.html`](./dast-zap-report/zap-report.html) — human-readable report
- [`dast-zap-report/zap-report.json`](./dast-zap-report/zap-report.json) — machine-readable report

Pipeline: `.github/workflows/security-scan.yml` — `dast` job  
Branch scanned: `feature/initial_template`
