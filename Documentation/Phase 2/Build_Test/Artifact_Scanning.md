# Artifact Scanning Configuration — LawyerApp

**Version:** 1.1
**Date:** 2026-05-17
**Author:** Group Wed PBS 5-2 — Build & Test

> Companion to [Pipeline.md](Pipeline.md) — the full pipeline operator's
> reference. This document focuses specifically on **artifact scanning**
> (scanners, severity policy, suppression process). For workflow design,
> trigger matrix, local reproduction, troubleshooting, glossary, and
> setup checklist, see [Pipeline.md](Pipeline.md).

---

## 1. Overview

This document describes the artifact scanning strategy integrated into the LawyerApp CI/CD pipeline. The goal is to detect security vulnerabilities in dependencies, container images, source code, and secrets before any artifact reaches production.

---

## 2. Scanning Tools

| Tool | Type | Trigger | Blocks Merge? |
|---|---|---|---|
| `dotnet list package --vulnerable` | SCA — NuGet dependencies | Every push / PR / weekly | Yes (any vulnerable package) |
| GitHub Dependency Review | SCA — PR diff | Every PR | Yes (`high` severity or GPL/AGPL) |
| Dependabot | SCA — automated update PRs | Weekly | n/a (raises PRs) |
| CodeQL | SAST (primary) | Every push / PR / weekly | Critical CodeQL findings |
| Semgrep | SAST (additional) | Every push / PR / weekly | Blocking rules in chosen packs |
| .NET Roslyn Analyzers + Security Code Scan | SAST (in-build) | Every build | Configurable |
| SonarCloud | SAST (optional) | Manual until `SONAR_TOKEN` configured | Per-PR quality gate |
| Trivy (image scan) | Container vulnerability scan | Every push / PR | Yes (CRITICAL/HIGH unfixed) |
| Trivy (filesystem + IaC) | Source / IaC misconfiguration | Every push / PR | Yes (CRITICAL/HIGH) |
| Syft (Anchore) | SBOM — container image | Every push / PR / weekly | n/a (evidence artifact) |
| CycloneDX | SBOM — .NET solution | Every push / PR / weekly | n/a (evidence artifact) |
| Grype (Anchore) | Independent image scan | Every push / PR / weekly | No (second opinion) |
| Gitleaks | Secret detection (git history) | Every push / PR / weekly | Yes |
| TruffleHog | Secret detection (verified) | Every push / PR / weekly | Yes |
| Hadolint | Dockerfile linting | Every push / PR | Yes (severity `error`) |
| actionlint | Workflow YAML validation | Every push / PR | Yes |
| yamllint | YAML hygiene (`.github/`) | Every push / PR | Yes |
| jq + regex guard | `appsettings*.json` syntax + plaintext-secret guard | Every push / PR | Yes |

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

SAST is layered across three engines so we catch issues that any single tool
would miss:

- **GitHub CodeQL** (primary) — workflow
  [`codeql.yml`](../../../.github/workflows/codeql.yml). Runs the
  `security-and-quality` query pack for C# on every push, PR, and weekly.
  Findings appear under *Security → Code scanning*.
- **Semgrep** (additional, open rules) — workflow
  [`semgrep.yml`](../../../.github/workflows/semgrep.yml). Runs `p/default`,
  `p/security-audit`, `p/csharp`, `p/dockerfile`, and `p/secrets`. SARIF is
  uploaded to the same Security tab.
- **SonarCloud** (optional) — workflow
  [`sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml). Disabled by
  default; enable by adding the `SONAR_TOKEN` secret and uncommenting the
  PR/push triggers. See Pipeline.md §5 for setup.
- **In-build SAST** (Roslyn analyzers + Security Code Scan) — runs as part of
  [`security-scan.yml`](../../../.github/workflows/security-scan.yml) job
  `sast`. Selected `CA` rules below are promoted to errors so they break the
  build.

### Roslyn rules enforced as errors (build-breaking)

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

## 6. Secret Detection (Gitleaks + TruffleHog)

**Workflow:** [`secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml)

Two scanners run in parallel to catch leaks from different angles:

- **Gitleaks** — job `gitleaks`. Checks out the full git history
  (`fetch-depth: 0`) and runs the default Gitleaks ruleset against every
  commit. Uploads a summary artifact and writes PR annotations on findings.
- **TruffleHog** — job `trufflehog`. Scans the PR diff range with
  `--results=verified,unknown` so we focus on credentials it can actively
  verify, dramatically reducing false positives for "this looks like a hex
  string".

Both jobs fail the workflow on any finding, blocking the PR.

### Developer notes
- Never commit `appsettings.Development.json` with real credentials.
- Use `dotnet user-secrets` for local development.
- Database connection strings must be retrieved from HashiCorp Vault in production (see `Program.cs` Vault section).
- Plaintext-secret patterns in `appsettings*.json` are also caught by the
  `json-validation` job in
  [`config-validation.yml`](../../../.github/workflows/config-validation.yml).

---

## 7. Filesystem & IaC Scanning

**Tool:** Trivy (filesystem + config/IaC mode)
**Workflow:** [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml)

Two scans run in this workflow:
- **Filesystem scan** (`scan-type: fs`) — looks at `LawyerApp/` for vulnerable
  packages and embedded lockfiles. Fails on CRITICAL/HIGH unfixed.
- **IaC / Dockerfile config scan** (`scan-type: config`) — looks at the whole
  repo for Dockerfile misconfigurations (root user, missing `USER`,
  unpinned `FROM`, etc.) and any IaC files. Fails on CRITICAL/HIGH.

Both produce SARIF uploaded to *Security → Code scanning* under categories
`trivy-fs` and `trivy-config`.

### Companion Dockerfile linter

[`config-validation.yml`](../../../.github/workflows/config-validation.yml)
runs **Hadolint** against `LawyerApp/Dockerfile` as a SARIF check first and
then again with `failure-threshold: error` — the second invocation blocks
the PR on error-level findings.

---

## 8. Software Bills of Materials (SBOMs)

**Workflow:** [`sbom.yml`](../../../.github/workflows/sbom.yml)

Two complementary SBOMs are generated for every push and PR:

- **CycloneDX (.NET solution)** — job `cyclonedx-solution`. Uses the
  `CycloneDX` global tool to walk every package reference in
  `LawyerApp.sln` and emit a JSON SBOM. Uploaded as `sbom-cyclonedx`
  (90-day retention).
- **Syft (Docker image, SPDX)** — job `syft-image`. Builds the production
  image and runs Anchore's `sbom-action` to emit SPDX-JSON.
- **Grype scan** — job `grype-second-opinion`. Runs Anchore's vulnerability
  scanner against the same image as an independent second opinion to
  Trivy. SARIF is uploaded under category `grype`.

## 9. Artifact Retention

All scan reports are uploaded as GitHub Actions artifacts:

| Artifact | Format | Retention |
|---|---|---|
| `sca-report` | Plain text | 30 days |
| `sast-report` | Plain text | 30 days |
| `trivy-report` | SARIF + JSON | 30 days |
| `semgrep-report` | SARIF | 30 days |
| `sbom-cyclonedx` | CycloneDX JSON | 90 days |
| `lawyerapp-image-sbom.spdx.json` | SPDX JSON | 90 days |
| `test-results` | TRX | 30 days |
| `coverage-report` | Cobertura XML | 30 days |
| `lawyerapp-publish` | .NET publish payload | 7 days |

SARIF files are also uploaded to the **GitHub Security → Code scanning** tab for persistent visibility (categories: `trivy-image`, `trivy-fs`, `trivy-config`, `semgrep`, `hadolint`, `grype`).
