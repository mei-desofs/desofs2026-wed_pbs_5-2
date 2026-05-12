# Artifact Scanning Configuration — LawyerApp

**Version:** 1.0  
**Date:** 2026-05-12  
**Author:** Group Wed PBS 5-2 — Build & Test (Member 2)

---

## 1. Overview

This document describes the artifact scanning strategy integrated into the LawyerApp CI/CD pipeline. The goal is to detect security vulnerabilities in dependencies, container images, source code, and secrets before any artifact reaches production.

---

## 2. Scanning Tools

| Tool | Type | Trigger | Blocks Merge? |
|---|---|---|---|
| `dotnet list package --vulnerable` | SCA — NuGet dependencies | Every push / PR | Yes (CRITICAL/HIGH) |
| Trivy (image scan) | Container vulnerability scan | Every push to main/develop | Yes (CRITICAL/HIGH unfixed) |
| Trivy (filesystem scan) | Source/IaC misconfiguration | Every push to main | Yes (CRITICAL/HIGH) |
| .NET Roslyn Analyzers (CA rules) | SAST | Every build | Configurable |
| Gitleaks | Secret detection | Every push / PR | Yes |

---

## 3. Software Composition Analysis (SCA)

**Tool:** `dotnet list package --vulnerable --include-transitive`  
**Workflow:** [security-scan.yml](../../../.github/workflows/security-scan.yml) → job `sca`  

### What it checks
- All direct and transitive NuGet dependencies declared in `LawyerApp.csproj`.
- Vulnerability data sourced from the NuGet vulnerability database (updated by Microsoft).

### Failure condition
If any package with a known CVE is detected, the CI step exits with code 1 and the report is uploaded as `sca-report` artifact.

### Current dependencies reviewed

| Package | Version | Status |
|---|---|---|
| BCrypt.Net-Next | 4.1.0 | No known CVEs |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | No known CVEs |
| Swashbuckle.AspNetCore | 6.6.2 | No known CVEs |
| VaultSharp | 1.17.5.1 | No known CVEs |
| Microsoft.EntityFrameworkCore.Design | 8.0.4 | No known CVEs |

---

## 4. Container Image Scanning (Trivy)

**Tool:** [Trivy](https://github.com/aquasecurity/trivy) by Aqua Security  
**Workflow:** [security-scan.yml](../../../.github/workflows/security-scan.yml) → job `container-scan`  
**Ignore file:** [LawyerApp/.trivyignore](../../../LawyerApp/.trivyignore)

### Scan configuration

```yaml
image-ref: "lawyerapp:<git-sha>"
severity: "CRITICAL,HIGH"
exit-code: "1"          # Fail on findings
ignore-unfixed: true    # Skip CVEs with no available fix
format: "sarif"         # Uploaded to GitHub Security tab
```

### Base image
The Dockerfile uses `mcr.microsoft.com/dotnet/aspnet:8.0` (final stage) and `mcr.microsoft.com/dotnet/sdk:8.0` (build stage). Microsoft actively patches these images; the weekly scheduled scan (`cron: "0 6 * * 1"`) ensures newly disclosed CVEs are caught even without a code push.

### Ignoring a CVE
If a CVE is reviewed and accepted (e.g., not exploitable in our deployment configuration), add the CVE ID to `.trivyignore`:

```
CVE-YYYY-NNNNN  # Reviewed <date>: <reason>
```

---

## 5. Static Application Security Testing (SAST)

**Tool:** .NET Roslyn Analyzers (built into the SDK) with selected `CA` rules promoted to errors.

### Rules enforced as errors (build-breaking)

| Rule | Description |
|---|---|
| CA2100 | SQL queries should not be constructed from user input |
| CA3001 | Review code for SQL injection vulnerabilities |
| CA3002 | Review code for XSS vulnerabilities |
| CA3003 | Review code for file path injection vulnerabilities |
| CA3006 | Review code for process command injection |
| CA3007 | Review code for open redirect vulnerabilities |
| CA3010 | Review code for XAML injection vulnerabilities |
| CA3011 | Review code for DLL injection vulnerabilities |
| CA3012 | Review code for regex injection vulnerabilities |

### Running locally

```bash
dotnet build LawyerApp/LawyerApp.sln -c Release \
  /p:EnableNETAnalyzers=true \
  /p:AnalysisMode=All \
  /warnaserror:CA2100,CA3001,CA3002,CA3003,CA3006,CA3007,CA3010,CA3011,CA3012
```

---

## 6. Secret Detection (Gitleaks)

**Tool:** [Gitleaks](https://github.com/gitleaks/gitleaks)  
**Workflow:** [security-scan.yml](../../../.github/workflows/security-scan.yml) → job `secret-detection`

Gitleaks scans the full Git history for patterns matching known secret types: API keys, connection strings, tokens, private keys, etc. Configured via `GITHUB_TOKEN` to post results as GitHub checks.

### Developer notes
- Never commit `appsettings.Development.json` with real credentials.
- Use `dotnet user-secrets` for local development.
- Database connection strings must be retrieved from HashiCorp Vault in production (see `Program.cs` Vault section).

---

## 7. Filesystem & IaC Scanning

**Tool:** Trivy (config/IaC mode)  
**Workflow:** [trivy-config.yml](../../../.github/workflows/trivy-config.yml)

Scans `LawyerApp/` for:
- Dockerfile misconfigurations (e.g., running as root, COPY with `--chown` issues).
- Exposed ports, missing `USER` instructions, privileged containers.

---

## 8. Artifact Retention

All scan reports are uploaded as GitHub Actions artifacts and retained for 30 days:

| Artifact | Format | Retention |
|---|---|---|
| `sca-report` | Plain text | 30 days |
| `sast-report` | Plain text | 30 days |
| `trivy-report` | SARIF + JSON | 30 days |
| `test-results` | TRX | 30 days |
| `coverage-report` | Cobertura XML | 30 days |

SARIF files are also uploaded to the **GitHub Security → Code scanning** tab for persistent visibility.
