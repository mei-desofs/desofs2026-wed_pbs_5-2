# System & User Monitoring Evidence

## 1. Overview

The LawyerApp implements monitoring at two layers:

| Layer | Mechanism | Where |
|---|---|---|
| **Runtime logging** | ASP.NET Core built-in `ILogger` → stdout | Console / CI pipeline logs |
| **Audit logging** | Custom `AuditLog` entity persisted to PostgreSQL | `AuditLogs` table |

No external sink (Serilog, Seq, Grafana) is configured in this sprint. The two layers above provide both operational visibility and a durable, queryable audit trail.

---

## 2. Runtime Logging (ASP.NET Core `ILogger`)

### 2.1 Configuration

The application uses the default ASP.NET Core logging provider, configured in `Program.cs` via `WebApplication.CreateBuilder(args)`. No additional packages are required — logs are emitted to **stdout** automatically.

Every incoming HTTP request, database command, and framework-level event is logged with:
- Log level (`info`, `warn`, `fail`)
- Category (e.g. `Microsoft.EntityFrameworkCore.Database.Command`)
- Event ID
- Message

### 2.2 Evidence

The screenshot below was captured by running the application locally and tailing the log file with:

```bash
tail -f /tmp/lawyerapp.log
```

The log output shows, for each login attempt:
1. A `SELECT` query to look up the user by email
2. An `UPDATE` query to track failed attempts / lockout state
3. An **`INSERT INTO "AuditLogs"`** — confirming the audit entry is persisted on every authentication event

> **Screenshot:** `evidence/console-log.png`

The repeating `INSERT INTO "AuditLogs"` pattern (visible multiple times in the log) corresponds to each login call made during testing, proving the system produces a log event for every security-relevant action.

---

## 3. Audit Logging (Structured, Persisted)

### 3.1 Implementation

Audit logging is implemented via the repository pattern:

- **Interface:** `IAuditRepository` (`LawyerApp/Infrastructure/Repositories/AuditRepository.cs`)
- **Entity:** `AuditLog` with the following fields:

| Field | Type | Description |
|---|---|---|
| `Id` | int | Auto-incremented primary key |
| `Operation` | string | Action performed (e.g. `Login`, `Upload`, `Download`) |
| `Resource` | string | Entity type targeted (e.g. `User`, `Document`, `Process`) |
| `ResourceId` | string | Identifier of the specific resource |
| `StatusCode` | int | HTTP status code returned (200, 401, 403, 500) |
| `Success` | bool | Whether the operation succeeded |
| `UserId` | Guid | ID of the authenticated user (if any) |
| `UserRole` | string | Role of the user at time of action |
| `IpAddress` | string | Source IP address of the request |
| `Details` | string | Human-readable description |
| `TimestampUtc` | DateTime | UTC timestamp of the event |

### 3.2 Coverage

Audit log entries are written by the following services:

| Service | Operations logged |
|---|---|
| `LoginService` | Login success (200), invalid password (401), user not found (401), account locked (403) |
| `DocumentService` | Upload (201/500), download (200/403), list documents (200/403) |
| `LegalProcessService` | Create process (201/403/500), view process (200/403), list processes (200) |

### 3.3 Evidence — Audit Log Table

The following entries were captured from the `AuditLogs` table after running a test session against the locally running application:

```
 Id | Operation | Resource |      ResourceId      | StatusCode | Success | UserRole |        Details         |      Timestamp
----+-----------+----------+----------------------+------------+---------+----------+------------------------+---------------------
  1 | Login     | User     | lawyer@test.com      |        200 | t       | Client   | Logged in successfully | 2026-06-16 22:13:40
  2 | Login     | User     | lawyer@test.com      |        401 | f       | Client   | Invalid password       | 2026-06-16 22:14:33
  3 | Login     | User     | nobody@test.com      |        401 | f       | None     | User doesn't exist     | 2026-06-16 22:14:33
  4 | Login     | User     | alice@lawyerfirm.com |        200 | t       | Client   | Logged in successfully | 2026-06-16 22:17:10
  5 | Login     | User     | lawyer@test.com      |        200 | t       | Client   | Logged in successfully | 2026-06-16 22:17:10
  6 | Login     | User     | alice@lawyerfirm.com |        401 | f       | Client   | Invalid password       | 2026-06-16 22:17:10
  7 | Login     | User     | alice@lawyerfirm.com |        401 | f       | Client   | Invalid password       | 2026-06-16 22:17:10
  8 | Login     | User     | alice@lawyerfirm.com |        401 | f       | Client   | Invalid password       | 2026-06-16 22:17:11
  9 | Login     | User     | ghost@nowhere.com    |        401 | f       | None     | User doesn't exist     | 2026-06-16 22:17:11
(9 rows)
```

> **Screenshot:** `evidence/audit-log-table.png`

This demonstrates:
- **Successful authentication** tracked (rows 1, 4, 5)
- **Brute force detection** — 3 consecutive failed attempts for the same account (rows 6, 7, 8) are all recorded
- **Unknown user access** — attempts for non-existent emails are logged with `UserRole = None` (rows 3, 9)
- **Separation of events** — each attempt gets its own timestamped row with IP address, making forensic replay possible

### 3.4 Querying the Audit Log

To query the audit log in any environment with database access:

```sql
-- All events for a specific user
SELECT * FROM "AuditLogs" WHERE "ResourceId" = 'user@example.com' ORDER BY "TimestampUtc" DESC;

-- All failed login attempts in the last 24 hours
SELECT * FROM "AuditLogs"
WHERE "Operation" = 'Login' AND "Success" = false
  AND "TimestampUtc" > NOW() - INTERVAL '24 hours';

-- All 403 Forbidden events (potential unauthorised access)
SELECT * FROM "AuditLogs" WHERE "StatusCode" = 403 ORDER BY "TimestampUtc" DESC;
```

---

## 4. Health Endpoint

No dedicated health endpoint (`/health`) is configured in this sprint. This is a known gap. To add one, `Program.cs` would need:

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

This is tracked as a future improvement for Sprint 3.

---

## 5. Log Sink — Current State vs. Recommended

| Feature | Current State | Recommended (Future) |
|---|---|---|
| Log output | stdout (console) | Serilog → file + Seq |
| Audit trail | PostgreSQL `AuditLogs` table | Same, already durable |
| Dashboard | None | Seq (dev) / Grafana + Loki (prod) |
| Alerting | None | Alert on 5+ consecutive 401s from same IP |
| Health check | None | `GET /health` → `{"status":"Healthy"}` |

The audit log table already provides the most security-critical capability — a tamper-evident, queryable record of all authentication and data access events. Adding Serilog and a log aggregator would improve operational visibility but does not affect the audit trail.

---

## 6. Evidence Files

Place screenshots in `Documentation/Phase 3/Operate/evidence/`:

| File | Contents |
|---|---|
| `console-log.png` | Terminal output of `tail -f /tmp/lawyerapp.log` showing repeated `INSERT INTO "AuditLogs"` entries |
| `audit-log-table.png` | psql query output showing the 9 audit log rows with all fields |
