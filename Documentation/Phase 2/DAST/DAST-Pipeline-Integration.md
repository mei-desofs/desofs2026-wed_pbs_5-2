# DAST Pipeline Integration — OWASP ZAP in CI/CD

## 1. Overview

In Phase 2, DAST (Dynamic Application Security Testing) was integrated into the existing CI/CD pipeline as an automated job inside `.github/workflows/security-scan.yml`. Previously, ZAP was run manually once against a local instance. This integration makes DAST a permanent, repeatable part of the security pipeline that runs automatically on every push or pull request to `main` or `develop`.

---

## 2. Why Automate DAST

Running ZAP manually once is evidence of a tool working, not evidence of a security process. The goal of DevSecOps is to catch vulnerabilities continuously as the codebase evolves. With the pipeline integration:

- Every code change is automatically scanned
- Results are archived as workflow artifacts (30-day retention)
- Findings are comparable across sprints (Sprint 1 → Sprint 2 → ...)
- No manual intervention required to produce DAST evidence

---

## 3. Pipeline Architecture

The `dast` job runs in parallel with the existing `sca`, `sast`, and `container-scan` jobs inside `security-scan.yml`.

```
Security Scan Workflow
├── sca              (NuGet vulnerable packages)
├── sast             (Security Code Scan / Roslyn analyzers)
├── container-scan   (Trivy image scan)
└── dast             (OWASP ZAP API scan)  ← added in Phase 2
```

### Job flow

```
PostgreSQL service container (postgres:16)
        ↓
Checkout + Build (.NET Release)
        ↓
Install EF Core tools + Apply migrations
        ↓
Start application
        ↓
Wait until port is open (nc -z check)
        ↓
ZAP API scan
        ↓
Upload HTML + JSON report as workflow artifact
```

---

## 4. Key Technical Decisions

### PostgreSQL service container
The application requires a real PostgreSQL database to start. GitHub Actions service containers spin up a `postgres:16` instance before the job steps run. The connection string is injected via the `ConnectionString__PostgreSQL` environment variable, which maps to `ConnectionString:PostgreSQL` in ASP.NET Core's configuration system (double underscore = colon separator).

### `--no-launch-profile` flag
`dotnet run` reads `launchSettings.json` by default, which overrides the `ASPNETCORE_URLS` environment variable and hardcodes port `5139`. Adding `--no-launch-profile` disables this, allowing `ASPNETCORE_URLS=http://0.0.0.0:5000` to take effect. Binding to `0.0.0.0` (all interfaces) is required so the ZAP Docker container can reach the app via the host network.

### `--network host` for ZAP container
ZAP runs inside Docker. With `--network host`, the ZAP container shares the runner's network namespace, meaning `localhost:5000` inside ZAP resolves to the same `localhost:5000` where the app is running. This avoids Docker bridge networking complexity.

### Port readiness check with `nc -z`
The wait loop uses `nc -z localhost 5000` (TCP connection check) instead of `curl`. A `curl`-based check had a subtle bug: when curl fails to connect it outputs `000` to stdout, and the `|| echo "000"` fallback also outputs `000`, causing the combined output `000\n000` to be incorrectly interpreted as a non-zero HTTP status, producing a false positive that skipped the wait.

### `zap-api-scan.py` with OpenAPI spec
The standard `zap-baseline.py` uses an HTML spider that cannot discover REST API endpoints. `zap-api-scan.py` reads the OpenAPI/Swagger definition (`-t http://localhost:5000/swagger/v1/swagger.json -f openapi`) and generates requests for every defined endpoint, producing meaningful coverage of the actual API surface.

### `continue-on-error: true`
ZAP exits with code 2 when it finds WARN-level findings. Setting `continue-on-error: true` on the ZAP step prevents this from failing the job — the report is always uploaded regardless of findings. GitHub still surfaces a yellow annotation so findings are visible without blocking the build.

---

## 5. Artifacts

Each pipeline run produces a `dast-zap-report` artifact containing:
- `zap-report.html` — human-readable report
- `zap-report.json` — machine-readable report for automated processing

Artifacts are retained for 30 days.

---

## 6. Triggers

The DAST job runs on:
- Push to `main` or `develop`
- Pull requests targeting `main` or `develop`
- Manual trigger (`workflow_dispatch`) via the Actions tab
- Weekly scheduled run (Monday 06:00 UTC, inherited from the parent workflow)
