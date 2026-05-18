# CI/CD Pipeline — LawyerApp

> Sprint 2 deliverable for **Pipeline Automation** and the pipeline side of
> **Build & Test**. Operator's reference + design document.
>
> All workflows live under [`.github/workflows`](../../../.github/workflows)
> and trigger on every pull request to `main`/`develop`, on push to those
> branches, and on weekly schedules for drift detection.
>
> Companion documents: [Test_Plan.md](Test_Plan.md),
> [Artifact_Scanning.md](Artifact_Scanning.md),
> [Technical_Report.md](Technical_Report.md).

---

## Table of contents

1. [Executive overview](#1-executive-overview)
2. [Design overview](#2-design-overview)
3. [Trigger matrix](#3-trigger-matrix)
4. [Practices adopted](#4-practices-adopted)
5. [Workflow walkthroughs](#5-workflow-walkthroughs)
6. [Configuration files](#6-configuration-files)
7. [Tools used and rationale](#7-tools-used-and-rationale)
8. [Running every check locally](#8-running-every-check-locally)
9. [Interpreting findings](#9-interpreting-findings)
10. [Responding to a failed check](#10-responding-to-a-failed-check)
11. [Adding / modifying / suppressing checks](#11-adding--modifying--suppressing-checks)
12. [Troubleshooting](#12-troubleshooting)
13. [Required GitHub settings (outside source code)](#13-required-github-settings-outside-source-code)
14. [SonarCloud (optional setup)](#14-sonarcloud-optional-setup)
15. [Mapping to the Sprint 2 rubric](#15-mapping-to-the-sprint-2-rubric)
16. [Maintenance & lifecycle](#16-maintenance--lifecycle)
17. [Glossary](#17-glossary)
18. [References](#18-references)

---

## 1. Executive overview

The pipeline is **ten GitHub Actions workflows** + one Dependabot config +
three policy files (`.gitleaks.toml`, `.trivyignore`, `.gitignore`). Every
pull request to `main` or `develop` runs **all** the checks in parallel.
Mergeability is gated by status-check rules in branch protection.

The pipeline answers eight SSDLC questions on every PR:

| Question | Answered by |
|---|---|
| Does it build? | `build-test.yml` |
| Do the tests pass? | `build-test.yml` (xUnit + integration) |
| Is the source code free of known security anti-patterns? | `codeql.yml`, `semgrep.yml`, `sast` job in `security-scan.yml` |
| Does any dependency carry a known CVE? | `dependency-review.yml`, `sca` job in `security-scan.yml`, Dependabot |
| Does the container image carry CVEs we haven't suppressed? | `security-scan.yml` (Trivy image), `trivy-config.yml`, Grype job in `sbom.yml` |
| Have we committed a secret? | `secrets-scan.yml` (Gitleaks + TruffleHog) |
| Is our Dockerfile / YAML / JSON well-formed? | `config-validation.yml`, `trivy-config.yml` |
| Can we produce an auditable inventory of what's inside the build? | `sbom.yml` (CycloneDX + Syft SPDX) |

Everything reports SARIF to **Security → Code scanning** so findings
deduplicate and persist between runs. Failures are summarised on the PR
checks tab.

## 2. Design overview

The pipeline is decomposed into focused workflows that run in parallel on
each pull request. Splitting them keeps feedback loops fast, makes
individual checks easy to mark required in branch protection, and lets each
tool publish SARIF to the **GitHub Security tab** without one job blocking
another.

```
                       ┌──────────────────────────────────────────────┐
                       │           Pull request to main               │
                       └──────────────────────────────────────────────┘
                                            │
       ┌───────────┬──────────┬─────────────┼──────────────┬──────────┬───────────┐
       ▼           ▼          ▼             ▼              ▼          ▼           ▼
  ┌─────────┐ ┌─────────┐ ┌─────────┐  ┌─────────┐   ┌──────────┐ ┌────────┐ ┌─────────┐
  │ Build & │ │ CodeQL  │ │ Semgrep │  │ DepRev  │   │ Trivy fs │ │ SBOM   │ │ Secrets │
  │ Test    │ │ (SAST)  │ │ (SAST)  │  │ (SCA)   │   │ + IaC    │ │ + Grype│ │ Scan    │
  └─────────┘ └─────────┘ └─────────┘  └─────────┘   └──────────┘ └────────┘ └─────────┘
       │           │          │             │              │          │           │
       ▼           ▼          ▼             ▼              ▼          ▼           ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │           Security Scan (SCA + .NET-analyzer SAST + Trivy image)                │
  └─────────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │                      Configuration Validation                                    │
  │       Hadolint · actionlint · yamllint · JSON / appsettings · dotnet format      │
  └─────────────────────────────────────────────────────────────────────────────────┘
       │
       └─────► All findings aggregated under Security → Code scanning (SARIF) ◄─────
```

### Design principles

- **Focused workflows over one giant one** — easier to mark each as required
  in branch protection, easier to re-run individual jobs, easier to read
  logs.
- **Fail closed, document open** — any HIGH/CRITICAL finding fails the PR
  by default. To bypass, add a documented suppression entry
  (`.trivyignore`, `.gitleaks.toml`) so the reviewer sees the justification
  in the diff.
- **Defence in depth** — at least two independent tools cover each rubric
  area (SAST: CodeQL + Semgrep; SCA: Dependency Review + `dotnet list`;
  Image: Trivy + Grype; Secrets: Gitleaks + TruffleHog).
- **Pinned versions** — every third-party action carries an explicit
  version tag. Dependabot raises rebump PRs weekly so updates are visible
  and reviewable, never silent.
- **Least privilege** — every workflow declares an explicit `permissions:`
  block (default `contents: read`). Jobs that publish to the Security tab
  add `security-events: write`; Dependency Review adds
  `pull-requests: write` for the inline summary. No workflow uses the
  default permissive token.

## 3. Trigger matrix

| Workflow | Pull request | Push | Schedule (weekly) | Manual |
|---|:---:|:---:|:---:|:---:|
| [`build-test.yml`](../../../.github/workflows/build-test.yml) | yes | yes |  | yes |
| [`codeql.yml`](../../../.github/workflows/codeql.yml) | yes | yes | yes | yes |
| [`semgrep.yml`](../../../.github/workflows/semgrep.yml) | yes | yes | yes | yes |
| [`sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml) | (after setup) | (after setup) |  | yes |
| [`dependency-review.yml`](../../../.github/workflows/dependency-review.yml) | yes |  |  |  |
| [`security-scan.yml`](../../../.github/workflows/security-scan.yml) | yes | yes | yes | yes |
| [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml) | yes | yes |  | yes |
| [`sbom.yml`](../../../.github/workflows/sbom.yml) | yes | yes | yes | yes |
| [`secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml) | yes | yes | yes | yes |
| [`config-validation.yml`](../../../.github/workflows/config-validation.yml) | yes | yes |  | yes |

Weekly schedules surface advisories that drop **after** code freezes — a
no-code-change scan still catches new CVEs in pinned dependencies.

## 4. Practices adopted

### Build & Test
- Reproducible builds: pinned .NET 8 SDK, NuGet cache keyed on `*.csproj`,
  `Release` configuration.
- xUnit + Moq + FluentAssertions tests on every PR (see
  [Test_Plan.md](Test_Plan.md)).
- TRX + Cobertura coverage uploaded as artifacts.
- Production payload uploaded as `lawyerapp-publish`.

### SAST (Static Application Security Testing)
- **CodeQL** (`security-and-quality` query suite) — primary.
- **Semgrep** (`p/default p/security-audit p/csharp p/dockerfile p/secrets`)
  — additional.
- **.NET analyzers + Security Code Scan** — runs in-build, promotes
  selected `CA` rules to errors.
- **SonarCloud** — optional, scaffolded as manual-trigger.

### SCA (Software Composition Analysis)
- **GitHub Dependency Review** — PR-diff CVE + license gate.
- **`dotnet list package --vulnerable --include-transitive`** — fails on
  any vulnerable direct or transitive package.
- **`--deprecated`/`--outdated`** — visibility-only.
- **Dependabot** — weekly grouped PRs for NuGet, Actions, Docker.
- **CycloneDX SBOM** + **Syft SPDX SBOM** — per-build component inventory.

### Artifact (container) scanning
- **Trivy image scan** — OS + library CVEs, fail on CRITICAL/HIGH.
- **Trivy filesystem + IaC scan** — vulnerable lockfiles, Dockerfile
  misconfigs.
- **Grype** — independent second-opinion scan of the same image.

### Configuration validation
- **Hadolint** — Dockerfile lint, fail on error-severity.
- **actionlint** — GitHub workflow YAML validation.
- **yamllint** — generic YAML hygiene.
- **`jq` + regex guard** — JSON syntax + plaintext-secret scan for
  `appsettings*.json`.
- **`dotnet format --verify-no-changes`** — style/whitespace drift.

### Secret detection
- **Gitleaks** — full git history, with `.gitleaks.toml` allowlist for
  documented false positives.
- **TruffleHog** — PR diff range, verified-only mode.

### Pipeline hygiene
- **Least-privilege tokens** on every workflow (`permissions:` block).
- **Pinned action versions**; Dependabot raises rebump PRs.
- **`timeout-minutes`** on every job.
- **`workflow_dispatch:`** on every workflow for manual re-runs.

## 5. Workflow walkthroughs

Each workflow is described in three parts: **trigger**, **what each step
does**, and **failure modes**. Inputs and outputs are documented inline.

---

### 5.1 `build-test.yml` — Build & Test

**Path:** [`.github/workflows/build-test.yml`](../../../.github/workflows/build-test.yml)
**Triggers:** push/PR to `main`/`develop`, manual dispatch.
**Timeout:** 20 min.
**Permissions:** `contents: read`.

#### Job: `build-and-test`

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `Checkout source` | `actions/checkout@v4` — pulls the repo at the PR head. | Required for every workflow. |
| 2 | `Setup .NET` | Installs SDK `8.0.x`. | Reproducible toolchain — independent of runner-image defaults. |
| 3 | `Cache NuGet packages` | `actions/cache@v4` keyed on `hashFiles('**/*.csproj')`. | Avoids re-downloading dependencies on every run — cuts a typical build from ~3 min to ~30 s. |
| 4 | `Restore dependencies` | `dotnet restore LawyerApp/LawyerApp.sln`. | Downloads transitive NuGet graph. |
| 5 | `Build (Release)` | `dotnet build … -c Release --no-restore`. | Builds in the configuration we ship — `Release` enables compiler optimisations and is what the test/publish artifacts share. |
| 6 | `Run tests with code coverage` | `dotnet test … --logger trx --collect:"XPlat Code Coverage"`. | Runs every test project the solution references; emits TRX (Visual Studio test result format) and Cobertura coverage XML. |
| 7 | `Upload test results` | `actions/upload-artifact@v4` → `test-results`. | TRX is openable in Visual Studio or via `dotnet-trx`. Retained 7 days. |
| 8 | `Upload coverage report` | `actions/upload-artifact@v4` → `coverage-report`. | Cobertura XML is consumable by ReportGenerator, Codecov, SonarCloud, etc. |
| 9 | `Publish (production payload)` | `dotnet publish … -c Release --no-build`. | Produces the runtime payload that Docker would COPY into the image. Step name is "production payload" because it is bit-for-bit what ships. |
| 10 | `Upload published artifact` | `actions/upload-artifact@v4` → `lawyerapp-publish`. | Lets a reviewer download the exact build a PR produced. Retained 7 days. |

**Failure modes:** any compile error or test failure fails the job. To
debug a specific test failure, download the `test-results` artifact and
open the `.trx` file.

---

### 5.2 `codeql.yml` — Primary SAST

**Path:** [`.github/workflows/codeql.yml`](../../../.github/workflows/codeql.yml)
**Triggers:** push/PR/weekly (Mondays 03:17 UTC) / manual.
**Timeout:** 30 min.
**Permissions:** `contents: read`, `security-events: write`,
`actions: read`.

#### Job: `analyze` (matrix `language: [csharp]`)

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `Checkout` | Standard. | — |
| 2 | `Setup .NET` | Installs SDK `8.0.x`. | CodeQL needs to **build** the project to extract semantic information. |
| 3 | `Initialize CodeQL` | `github/codeql-action/init@v3` with `queries: security-and-quality`. | Picks the more aggressive query pack (default `security-extended` plus quality queries) — favoured for a security-graded project. |
| 4 | `Build for CodeQL` | `dotnet restore` + `dotnet build … --configuration Release`. | Required for compiled languages (C#). Without a successful build, CodeQL can't extract symbols. |
| 5 | `Perform CodeQL Analysis` | `github/codeql-action/analyze@v3`. | Runs the query pack against the extracted database, uploads SARIF to Security tab under category `/language:csharp`. |

**Failure modes:** build failure during step 4 fails the job. Findings
above CodeQL's default threshold appear in *Security → Code scanning* but
do **not** by themselves fail the workflow (GitHub does dedup and
classification server-side).

---

### 5.3 `semgrep.yml` — Additional SAST

**Path:** [`.github/workflows/semgrep.yml`](../../../.github/workflows/semgrep.yml)
**Triggers:** push/PR/weekly (Mondays 04:27 UTC) / manual.
**Timeout:** 20 min.
**Permissions:** `contents: read`, `security-events: write`.

#### Job: `semgrep`
Runs inside the official `semgrep/semgrep` container image.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `Checkout` | Standard. | — |
| 2 | `Run Semgrep` | `semgrep ci --sarif --output=semgrep.sarif --config=p/default --config=p/security-audit --config=p/csharp --config=p/dockerfile --config=p/secrets`. | Five rulesets, layered: general code quality, OWASP-aligned security, C#-specific, Dockerfile, and secrets. SARIF output for the Security tab. |
| 3 | `Upload SARIF to GitHub` | `github/codeql-action/upload-sarif@v3` under category `semgrep`. | Findings appear in Security tab alongside CodeQL — deduplication via category labels. |
| 4 | `Upload Semgrep report` | `actions/upload-artifact@v4` → `semgrep-report` (30 d). | Lets reviewers download the raw SARIF if the Security tab isn't accessible. |

**Failure modes:** Semgrep exits non-zero on findings in `p/` rulesets
classified as `ERROR`. Lower-severity matches are reported but do not
fail. To override, switch to `--severity=ERROR` only.

---

### 5.4 `sonarcloud.yml` — Optional SAST

**Path:** [`.github/workflows/sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml)
**Triggers:** manual dispatch only (until `SONAR_TOKEN` is configured).
**Timeout:** 30 min.
**Permissions:** `contents: read`, `pull-requests: read`.

Runs on `windows-latest` because the dotnet-sonarscanner CLI tool has
better Windows support and the team's solution compiles cleanly there.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `Checkout` (fetch-depth: 0) | Pulls full history. | Sonar uses git blame for "new code" detection. |
| 2 | `Setup .NET` | Same as build-test. | Build is required for analysis. |
| 3 | `Setup Java` | Temurin 17. | SonarScanner is a Java tool. |
| 4 | `Cache SonarCloud packages` | Caches `~\sonar\cache`. | Avoid re-downloading the analyzer. |
| 5 | `Cache SonarScanner` | Caches `.\.sonar\scanner`. | Avoid re-installing the dotnet global tool. |
| 6 | `Install SonarScanner` | `dotnet tool update dotnet-sonarscanner`. | Only runs on cache miss. |
| 7 | `Build & Analyze` | `sonarscanner begin … && dotnet build && sonarscanner end`. | Begin sets up an instrumentation hook, build collects data, end uploads. Skipped if `SONAR_TOKEN` is empty. |

**Setup:** see [§14](#14-sonarcloud-optional-setup).

---

### 5.5 `dependency-review.yml` — SCA on PR diff

**Path:** [`.github/workflows/dependency-review.yml`](../../../.github/workflows/dependency-review.yml)
**Triggers:** pull_request only.
**Timeout:** 5 min.
**Permissions:** `contents: read`, `pull-requests: write` (for the inline
PR comment on failure).

#### Job: `dependency-review`

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `Checkout` | Standard. | — |
| 2 | `Dependency Review` | `actions/dependency-review-action@v4` with `fail-on-severity: high`, `license-check: true`, `deny-licenses: GPL-2.0, GPL-3.0, AGPL-3.0`, `comment-summary-in-pr: on-failure`. | Compares the PR's lockfile/csproj diff against the GitHub Advisory Database. Blocks new HIGH CVE deps or copyleft licences. Adds a PR comment listing offending packages only on failure (keeps the PR clean otherwise). |

**Requires:** the repo's **Dependency graph** must be enabled in
*Settings → Code security*. Otherwise the action errors with "Dependency
review is not supported on this repository".

---

### 5.6 `security-scan.yml` — SCA + in-build SAST + Trivy image

**Path:** [`.github/workflows/security-scan.yml`](../../../.github/workflows/security-scan.yml)
**Triggers:** push/PR/weekly (Mondays 06:00 UTC) / manual.
**Permissions:** workflow-level `contents: read`; `container-scan` job
also requires `security-events: write` for SARIF upload.

#### Job 1 — `sca` (Vulnerable NuGet packages)
**Timeout:** 10 min.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `actions/checkout@v4` | Standard. | — |
| 2 | `Setup .NET` | Installs SDK. | — |
| 3 | `Restore packages` | `dotnet restore`. | Required before `list package`. |
| 4 | `Check for vulnerable packages` | `dotnet list … package --vulnerable --include-transitive` piped to `tee vulnerable-packages.txt`. Grep for the "has the following vulnerable packages" header → exit 1 if present. | Vulnerability data comes from the NuGet feed via Microsoft. Includes transitive dependencies (the ones our direct packages pull in). |
| 5 | `Upload SCA report` | Uploads `vulnerable-packages.txt`. | Persists the human-readable table even if no findings. |

#### Job 2 — `sast` (.NET analyzers + Security Code Scan)
**Timeout:** 20 min.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | Checkout + setup .NET. | Standard. | — |
| 2 | `Install Security Code Scan analyzer` | `dotnet tool install --global security-scan --version 5.6.7`. | Source-code-only SAST tool, complements CodeQL. |
| 3 | `Restore & build with analyzers` | `dotnet build … /p:EnableNETAnalyzers=true /p:AnalysisMode=All /warnaserror:CA2100,CA3001..CA3012`. | Roslyn analyzers run during build; specific CAs are promoted to errors (SQL injection, XSS, path traversal, command injection, etc.). Pipes output to `sast-output.txt`. |
| 4 | `Upload SAST output` | Uploads `sast-output.txt`. | — |

#### Job 3 — `container-scan` (Trivy image)
**Timeout:** 25 min.
**Permissions:** `contents: read`, `security-events: write`.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | Checkout. | — | — |
| 2 | `Build Docker image` | `docker build -t lawyerapp:${{ github.sha }} LawyerApp/`. | Build locally, no registry push. |
| 3 | `Trivy — CRITICAL/HIGH scan` | `aquasecurity/trivy-action@0.35.0` with `severity: "CRITICAL,HIGH"`, `exit-code: "1"`, `ignore-unfixed: true`, `trivyignores: LawyerApp/.trivyignore`, `limit-severities-for-sarif: true`. | Gating step. `ignore-unfixed: true` skips CVEs with no patch available. `limit-severities-for-sarif: true` is the key fix — without it the action expands severity to "all" for SARIF builds, so MEDIUM/LOW would fail the gate (see [§12](#12-troubleshooting)). |
| 4 | `Upload SARIF` | Uploads under category `trivy-image`. | — |
| 5 | `Trivy — full JSON report` | Same image, all severities, no exit-code. | Comprehensive record for offline analysis. |
| 6 | `Upload full Trivy report` | Uploads SARIF + JSON together as `trivy-report` artifact (30 d). | — |

---

### 5.7 `trivy-config.yml` — Filesystem + IaC scan

**Path:** [`.github/workflows/trivy-config.yml`](../../../.github/workflows/trivy-config.yml)
**Triggers:** push/PR/manual.
**Timeout:** 15 min.
**Permissions:** `contents: read`, `security-events: write`.

#### Job: `trivy-fs`

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | Checkout. | — | — |
| 2 | `Trivy — filesystem scan` | `scan-type: fs`, `scan-ref: LawyerApp/`, `severity: CRITICAL,HIGH`, `exit-code: 1`, `ignore-unfixed: true`. | Scans lockfiles + manifests inside `LawyerApp/`. For .NET this is mostly the `.csproj` / `project.assets.json`. |
| 3 | `Upload filesystem SARIF` | Category `trivy-fs`. | — |
| 4 | `Trivy — IaC / Dockerfile config scan` | `scan-type: config`, `scan-ref: .`, `severity: CRITICAL,HIGH`, `exit-code: 1`. | Lints Dockerfile (DS-0001…DS-0030 rule set). Caught by the rubric as "configuration validation". |
| 5 | `Upload IaC SARIF` | Category `trivy-config`. | — |

---

### 5.8 `sbom.yml` — SBOM generation + Grype

**Path:** [`.github/workflows/sbom.yml`](../../../.github/workflows/sbom.yml)
**Triggers:** push/PR/weekly (Mondays 05:37 UTC) / manual.
**Permissions:** workflow-level `contents: read`; `grype-second-opinion`
adds `security-events: write`.

#### Job 1 — `cyclonedx-solution` (.NET solution SBOM)
**Timeout:** 10 min.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | Checkout + setup .NET. | — | — |
| 2 | `Install CycloneDX tool` | `dotnet tool install --global CycloneDX`. | Microsoft's official SBOM generator for .NET. |
| 3 | `Generate SBOM (JSON)` | `dotnet CycloneDX LawyerApp/LawyerApp.sln --output ./sbom --json`. | CycloneDX is the OWASP-backed SBOM standard. JSON is the most portable format. |
| 4 | `Upload SBOM` | `sbom-cyclonedx` artifact, 90-day retention. | Auditor-facing artifact — proves we can produce an inventory on demand. |

#### Job 2 — `syft-image` (Container image SBOM)
**Timeout:** 20 min.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | Checkout + Docker Buildx setup. | — | — |
| 2 | `Build image` | `docker/build-push-action@v6` with GHA cache. | Local build, no push. |
| 3 | `Generate Syft SBOM (SPDX)` | `anchore/sbom-action@v0` with `format: spdx-json`. | SPDX is the Linux Foundation SBOM standard. Includes every OS package and language-layer dep. |

#### Job 3 — `grype-second-opinion` (independent vulnerability scan)
**Timeout:** 20 min.

| # | Step | What it does | Why |
|---|---|---|---|
| 1-3 | Same image build as job 2. | — | — |
| 4 | `Grype scan` | `anchore/scan-action@v4`, `severity-cutoff: high`, `output-format: sarif`. | Independent CVE database & engine — catches things Trivy may miss and vice versa. |
| 5 | `Inspect Grype SARIF` | Guard script — sets `found=true` only if the SARIF is non-empty. | The action emits an empty SARIF when there are no findings above the cutoff, which makes the upload step fail (see [§12](#12-troubleshooting)). |
| 6 | `Upload Grype SARIF` | Category `grype`. Only runs when `found=true`. | — |

---

### 5.9 `secrets-scan.yml` — Gitleaks + TruffleHog

**Path:** [`.github/workflows/secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml)
**Triggers:** push/PR/weekly (Mondays 07:07 UTC) / manual.
**Permissions:** `contents: read`, `security-events: write`.

#### Job 1 — `gitleaks`
**Timeout:** 10 min.

We run the **gitleaks binary directly** rather than `gitleaks/gitleaks-action@v2`
because v2 requires a paid licence for organisation-owned repos
(`mei-desofs` is an org). The binary itself is MIT-licensed and free.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `Checkout (full history)` | `fetch-depth: 0`. | Scans every commit, not just HEAD — a secret committed and later removed is still a leak. |
| 2 | `Install gitleaks` | curl release + tar to `/usr/local/bin/`. Pin version `8.18.4`. | Avoids the licensed action. |
| 3 | `Run gitleaks (full history)` | `gitleaks detect --source . --config .gitleaks.toml --report-format sarif --report-path gitleaks.sarif --redact --exit-code 1 --verbose`. | `.gitleaks.toml` carries the documented allowlist (see [§6.1](#61-gitleakstoml)). `--redact` masks secret values in the report. |
| 4 | `Upload SARIF to GitHub Security` | Category `gitleaks`. Only if SARIF non-empty. | — |
| 5 | `Upload Gitleaks report artifact` | `gitleaks-report`, 30-day retention. | — |
| 6 | `Fail on findings` | Exits 1 if step 3's exit code was non-zero. | Decoupled from the SARIF step so the artifact still gets uploaded on failure. |

#### Job 2 — `trufflehog`
**Timeout:** 10 min.

| # | Step | What it does | Why |
|---|---|---|---|
| 1 | `Checkout (full history)` | Standard. | — |
| 2 | `TruffleHog scan` | `trufflesecurity/trufflehog@main` with `--results=verified,unknown` on `${base}..HEAD`. | "Verified" means TruffleHog actively API-checks the credential against the issuer. Massively reduces false positives compared to entropy-only scanning. |

---

### 5.10 `config-validation.yml` — Configuration validation

**Path:** [`.github/workflows/config-validation.yml`](../../../.github/workflows/config-validation.yml)
**Triggers:** push/PR/manual.
**Permissions:** `contents: read`, `security-events: write` (Hadolint
SARIF upload).

Five independent jobs (~5 min each):

#### `dockerfile-lint` — Hadolint
1. `Hadolint (SARIF output)` — uses `hadolint/hadolint-action@v3.1.0` with
   `no-fail: true` so SARIF is uploaded even when issues exist.
2. `Upload Hadolint SARIF` — category `hadolint`.
3. `Hadolint (fail on error severity)` — second invocation with
   `failure-threshold: error` to gate the build. Splitting the two
   invocations is the cleanest way to upload SARIF for warnings while
   still gating on errors.

#### `workflow-lint` — actionlint
Downloads the actionlint binary via the upstream `download-actionlint.bash`
script and runs `actionlint -color`. Catches:
- Unknown action inputs / outputs.
- Shell-injection patterns in `run:` blocks (`${{ … }}` of untrusted
  origin).
- Invalid `if:` expressions.
- Missing `secrets:` declarations.

#### `yaml-lint` — yamllint
Generic YAML hygiene on the `.github/` tree. Rules tuned to avoid noisy
warnings about line length and quoted `on:` keys.

#### `json-validation` — appsettings.json + plaintext-secret guard
1. `Install jq`.
2. `Validate appsettings*.json syntax` — `jq empty` on every
   `LawyerApp/**/appsettings*.json`.
3. `Forbid plaintext secrets in appsettings*.json` — regex sweep for
   `(Password|Pwd|ApiKey|Secret|Token|ConnectionString)\s*=\s*[^"$ ]{4,}`.
   Allowlists dev placeholders (`Password=postgres`, `Server=localhost`)
   only when the file is named `*.Development.json`.

#### `dotnet-format` — style consistency
`dotnet format … --verify-no-changes --severity warn`. `continue-on-error: true`
so style drift doesn't fail the build today, but the warning is visible.

## 6. Configuration files

### 6.1 `.gitleaks.toml`

**Path:** [`/.gitleaks.toml`](../../../.gitleaks.toml)
**Purpose:** allowlist of documented false positives.

```toml
[extend]
useDefault = true              # build on top of the default ruleset

[allowlist]
regexes = [
    '''PublicKeyToken=[0-9a-fA-F]{16}''',
]
paths = [
    '''(^|/)\.vs/''',
    '''(^|/)bin/''',
    '''(^|/)obj/''',
]
```

**Why the regex:** .NET assembly `PublicKeyToken=<hex>` values are *public*
identifiers of signed assemblies (e.g. `b03f5f7f11d50a3a` for `System.Web`).
Gitleaks' `generic-api-key` rule misclassifies them as random tokens
because they have high entropy.

**Why the paths:** `.vs/`, `bin/`, `obj/` are IDE/build-output folders.
Even after we untracked them in commit `7e33e6f`, the full-history scan
keeps surfacing entries from earlier commits.

### 6.2 `LawyerApp/.trivyignore`

**Path:** [`LawyerApp/.trivyignore`](../../../LawyerApp/.trivyignore)
**Purpose:** suppressed CVE IDs with a reviewed-on date and one-line
justification.

```
CVE-2026-0861    # Reviewed 2026-05-17: libc-bin / libc6 — pending Microsoft image rebuild
CVE-2026-4878    # Reviewed 2026-05-17: libcap2 — pending Microsoft image rebuild
CVE-2026-29111   # Reviewed 2026-05-17: libsystemd0 / libudev1 — pending Microsoft image rebuild
```

**Process:** every entry **must** include the date reviewed, the package,
and a one-line justification. Re-audit quarterly — if the upstream image
rebuilds with the fixed package version, **remove the entry** so the
suppression doesn't outlive its rationale.

Wired into both Trivy steps in `security-scan.yml` via the
`trivyignores: LawyerApp/.trivyignore` action input.

### 6.3 `.github/dependabot.yml`

**Path:** [`.github/dependabot.yml`](../../../.github/dependabot.yml)
**Purpose:** automated weekly dependency-update PRs.

Four ecosystems configured:

| Ecosystem | Directory | Schedule | Group |
|---|---|---|---|
| `nuget` | `/LawyerApp` | Monday 03:00 Europe/Lisbon | `microsoft`, `ef-core`, `security` |
| `nuget` | `/LawyerApp.Tests` | Monday 03:15 Europe/Lisbon | (none) |
| `github-actions` | `/` | Monday 03:30 Europe/Lisbon | (none) |
| `docker` | `/LawyerApp` | Monday 04:00 Europe/Lisbon | (none) |

Commit-message prefixes (`chore(deps)`, `chore(test-deps)`, `chore(ci)`,
`chore(docker)`) make it easy to filter Dependabot PRs.

### 6.4 `.gitignore`

**Path:** [`/.gitignore`](../../../.gitignore)
**Purpose:** stop tracking build outputs, IDE state, and scan reports.

Notable additions vs the original template:
- `**/bin/`, `**/obj/` — .NET build output.
- `.vs/`, `*.user` — Visual Studio per-user state.
- `coverage/`, `TestResults/`, `**/coverage.cobertura.xml`.
- `*.sarif`, `sbom/`, `*.spdx.json`, `*.cyclonedx.json`,
  `vulnerable-packages.txt`, `trivy-*.sarif`, `semgrep.sarif`,
  `hadolint.sarif`, `sast-output.txt` — every pipeline output filename.
- `appsettings.Local.json`, `secrets.json`, `*.pfx`, `*.key`, `*.pem`,
  `.env`, `.env.*` (`!.env.example` kept).

## 7. Tools used and rationale

Why each tool was chosen, the alternatives considered, and where its
limits are.

| Tool | Category | Chosen because | Alternatives considered | Limit |
|---|---|---|---|---|
| **`dotnet` CLI** | Build/Test | First-party, only practical option for .NET. | — | — |
| **CodeQL** | SAST | First-party, free on public repos, deep semantic analysis. SARIF integration is best-in-class. | SonarCloud, PVS-Studio, NDepend. | Slower than Semgrep; requires a build (true for compiled languages anyway). |
| **Semgrep** | SAST | Open-source, fast (~1 min), broad rule packs, no build needed. Catches things CodeQL misses (Dockerfile / secrets / cross-language patterns). | Bandit (Python-only), gosec (Go-only). | Pattern-based — less precise than CodeQL for complex flows. |
| **Security Code Scan** | SAST | .NET-specific, runs in-build as a Roslyn analyzer, catches CA-classified issues at compile time. | `Microsoft.CodeAnalysis.NetAnalyzers` only. | Last release 5.6.7 (somewhat stale); we use it as additional, not primary. |
| **SonarCloud** | SAST | Best-in-class quality gate UX (PR comments, hotspots, coverage overlay). | None comparable. | Requires a SaaS account; manual setup, hence optional. |
| **GitHub Dependency Review** | SCA | First-party, runs only on the PR diff, fastest feedback for the contributor. | Snyk PR check. | Only reports new vulns introduced by the PR — doesn't catch pre-existing ones. |
| **`dotnet list package --vulnerable`** | SCA | First-party, simplest possible check, runs against the NuGet feed. | OWASP Dependency-Check. | Requires `dotnet restore` first; report is plain text. |
| **Dependabot** | SCA | First-party, automated PR creation, group rules, native to GitHub. | Renovate. | Granular grouping is less expressive than Renovate. |
| **CycloneDX** | SBOM | OWASP-backed standard, .NET tooling is first-party (Microsoft). | SPDX (used elsewhere — see Syft). | — |
| **Syft (Anchore)** | SBOM | De-facto standard for container images, SPDX output. | CycloneDX-cli. | — |
| **Trivy** | Container + IaC + filesystem | Single tool that covers three rubric areas, very widely adopted. | Snyk, Clair, Anchore Engine. | Some quirks around severity filtering — see [§12](#12-troubleshooting). |
| **Grype (Anchore)** | Container (second opinion) | Independent engine + DB, lightweight, catches what Trivy misses. | None added — we already have Trivy. | — |
| **Hadolint** | Config (Dockerfile) | De-facto Dockerfile linter, integrates with SARIF. | Dockerlinter (less mature). | — |
| **actionlint** | Config (workflows) | Catches GitHub Actions–specific bugs (shell injection, missing inputs). | yamllint alone misses these. | — |
| **yamllint** | Config (YAML) | Generic YAML hygiene. | — | — |
| **`jq`** | Config (JSON) | Standard JSON CLI tool. | Python `json.tool`. | — |
| **Gitleaks** | Secrets | Open-source, mature, configurable. | TruffleHog (also in pipeline). | gitleaks-action@v2 needs a paid licence for orgs — we use the binary directly. |
| **TruffleHog** | Secrets | "Verified" mode validates credentials by hitting the issuer's API. Much lower false-positive rate than entropy-only scanning. | Gitleaks alone. | Slower than Gitleaks; we run it on PR diff only. |

## 8. Running every check locally

Every check in CI can be reproduced locally. Run from the repo root.

### Build & Test
```bash
dotnet restore LawyerApp/LawyerApp.sln
dotnet build LawyerApp/LawyerApp.sln -c Release
dotnet test  LawyerApp/LawyerApp.Tests/LawyerApp.Tests.csproj \
  -c Release --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet publish LawyerApp/LawyerApp.csproj -c Release --output ./publish /p:UseAppHost=false
```

### CodeQL
Use the CodeQL CLI bundle from <https://github.com/github/codeql-action/releases>
or simply rely on the CI run. Local CodeQL is heavy (~15 min for setup);
in practice nobody runs it locally for incremental changes.

### Semgrep
```bash
# Install once
brew install semgrep                 # macOS
pipx install semgrep                 # any platform

semgrep ci --config=p/default --config=p/security-audit \
           --config=p/csharp --config=p/dockerfile --config=p/secrets .
```

### .NET analyzers
```bash
dotnet build LawyerApp/LawyerApp.sln -c Release \
  /p:EnableNETAnalyzers=true /p:AnalysisMode=All \
  /warnaserror:CA2100,CA3001,CA3002,CA3003,CA3006,CA3007,CA3010,CA3011,CA3012
```

### SCA — vulnerable / deprecated / outdated NuGet packages
```bash
dotnet restore LawyerApp/LawyerApp.sln
dotnet list LawyerApp/LawyerApp.sln package --vulnerable --include-transitive
dotnet list LawyerApp/LawyerApp.sln package --deprecated
dotnet list LawyerApp/LawyerApp.sln package --outdated
```

### CycloneDX SBOM
```bash
dotnet tool install --global CycloneDX
dotnet CycloneDX LawyerApp/LawyerApp.sln --output ./sbom --json
```

### Container build + Trivy + Grype + Syft (Docker required)
```bash
# Build the image locally
docker build -t lawyerapp:local LawyerApp/

# Trivy image scan (matches CI exactly)
docker run --rm -v "$(pwd):/repo" -w /repo aquasec/trivy:0.69.3 \
  image --severity CRITICAL,HIGH --ignore-unfixed \
  --ignorefile LawyerApp/.trivyignore lawyerapp:local

# Trivy filesystem scan
docker run --rm -v "$(pwd):/repo" -w /repo aquasec/trivy:0.69.3 \
  fs --severity CRITICAL,HIGH --ignore-unfixed LawyerApp/

# Trivy Dockerfile config scan
docker run --rm -v "$(pwd):/repo" -w /repo aquasec/trivy:0.69.3 \
  config LawyerApp/Dockerfile

# Syft SBOM of the image
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  anchore/syft lawyerapp:local -o spdx-json

# Grype scan of the image
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  anchore/grype lawyerapp:local --fail-on high
```

### Hadolint
```bash
docker run --rm -i hadolint/hadolint:latest < LawyerApp/Dockerfile
```

### actionlint
```bash
docker run --rm -v "$(pwd):/repo" -w /repo rhysd/actionlint:latest -color
```

### yamllint
```bash
brew install yamllint
yamllint -d "{extends: default, rules: {line-length: disable, truthy: {check-keys: false}}}" .github/
```

### Gitleaks (full history)
```bash
# Install once
brew install gitleaks                # macOS
# or download binary from https://github.com/gitleaks/gitleaks/releases

gitleaks detect --source . --config .gitleaks.toml --redact --verbose
```

### TruffleHog (PR diff)
```bash
docker run --rm -v "$(pwd):/repo" -w /repo trufflesecurity/trufflehog:latest \
  git file:///repo --since-commit=origin/main --results=verified,unknown
```

### appsettings JSON + plaintext-secret guard
```bash
for f in LawyerApp/**/appsettings*.json; do jq empty "$f"; done
grep -E -i '(Password|Pwd|ApiKey|Secret|Token|ConnectionString)\s*=\s*[^"$ ]{4,}' LawyerApp/**/appsettings*.json
```

### `dotnet format` style check
```bash
dotnet format LawyerApp/LawyerApp.sln --verify-no-changes --severity warn
```

## 9. Interpreting findings

Findings surface in three places, each with a different use:

### 9.1 The PR's Checks tab
- One row per workflow job.
- Click → log viewer with collapsible step output.
- Best for: figuring out *why* a check failed.

### 9.2 Security → Code scanning
- Aggregates SARIF from CodeQL, Semgrep, Trivy (image / fs / config),
  Grype, Hadolint, Gitleaks.
- Each finding has a category (set by `category:` on the upload step),
  severity, location (file + line), and de-dup fingerprint.
- Best for: triaging a backlog, marking findings as false-positive or
  won't-fix (those decisions persist across runs).

### 9.3 Actions → workflow run → Artifacts
- One artifact per scanner with a human-readable copy of the report
  (`vulnerable-packages.txt`, `trivy-report`, `sbom-cyclonedx`, etc.).
- Best for: feeding the result into a downstream tool, attaching to a
  ticket, or offline review.

### Severity definitions

| Severity | Threshold for failing the build | Examples |
|---|---|---|
| **CRITICAL** | Always blocks merge. | RCE in a dep, hardcoded private key. |
| **HIGH** | Blocks merge unless suppressed with documented justification. | Authentication bypass, SQL injection. |
| **MEDIUM** | Reported, not gated. | DoS vector, weak crypto. |
| **LOW** | Reported, not gated. | Style, weak randomness in non-security context. |

## 10. Responding to a failed check

A decision tree for the most common failure scenarios:

```
PR check is red
│
├── Is the failure a build error?
│   └── Fix the code. CI will turn green automatically.
│
├── Is it a test failure?
│   ├── Reproduce locally:
│   │     dotnet test LawyerApp/LawyerApp.Tests/LawyerApp.Tests.csproj --filter "Name~Failing"
│   └── Fix and push.
│
├── Is it a HIGH/CRITICAL CVE in a dependency?
│   ├── Can we upgrade?  Yes → update the .csproj, push, done.
│   ├── No fix available (ignore-unfixed already on)?  Investigate exploitability.
│   ├── Mitigated by deployment context?
│   │     Add CVE to LawyerApp/.trivyignore with date + justification.
│   └── Otherwise → block the PR.
│
├── Is it a HIGH/CRITICAL CVE in the base image?
│   ├── New Microsoft image available?  Bump the FROM tag (Dependabot will do this).
│   └── Otherwise → add CVE to .trivyignore with "pending upstream rebuild" note.
│
├── Is it a SAST finding (CodeQL / Semgrep)?
│   ├── True positive?  Fix the code.
│   ├── False positive?  Add a suppression comment in code OR mark dismissed
│   │     in Security → Code scanning (with reason).
│
├── Is it a Gitleaks finding?
│   ├── Real secret?  Rotate it immediately, revoke the issuer credential,
│   │     and use BFG / git-filter-repo to scrub history.
│   ├── False positive?  Add to .gitleaks.toml allowlist with comment.
│
├── Is it a Dependency Review failure?
│   ├── Real HIGH CVE?  Choose a different version.
│   ├── Copyleft licence?  Pick a permissive alternative.
│
├── Is it a Hadolint / Trivy config failure?
│   └── Fix the Dockerfile or workflow. These rules are easy to satisfy.
│
└── Is the workflow itself broken (config error, action version mismatch)?
    └── See troubleshooting (§12).
```

## 11. Adding / modifying / suppressing checks

### Add a new test project

1. `dotnet new xunit -o LawyerApp.Whatever.Tests`
2. Add the project to `LawyerApp.sln` (`dotnet sln add`).
3. CI will automatically pick it up — `dotnet test` runs every test
   project in the solution.

### Add a new scanner

1. Create a new workflow file in `.github/workflows/<scanner>.yml`.
2. Reuse the existing skeleton:
   - `permissions: contents: read` (plus `security-events: write` if
     uploading SARIF).
   - `timeout-minutes:` on every job.
   - Pin the action version.
   - Add `workflow_dispatch:` for manual re-runs.
3. Add an entry to the trigger matrix ([§3](#3-trigger-matrix)).
4. Add a walkthrough section ([§5](#5-workflow-walkthroughs)).
5. Add the corresponding badge to the [README](../../../README.md).
6. After the first successful run, the new check appears in the branch-
   protection picker. Add it as required.

### Suppress a CVE

`LawyerApp/.trivyignore`:
```
CVE-YYYY-NNNNN    # Reviewed <date>: <package> — <one-line reason>
```

Re-audit quarterly. Remove entries when the underlying fix is available.

### Suppress a Gitleaks false positive

`.gitleaks.toml`:
```toml
[allowlist]
regexes = [
    '''<your-pattern>''',     # <comment with reason>
]
paths = [
    '''(^|/)path/to/file/''',
]
```

### Suppress a CodeQL / Semgrep finding

Two options, pick one:
- **Dismiss in Security tab** with a written reason. Persists across runs
  via the fingerprint, but only stored in GitHub (not in git history).
- **Inline comment** at the source location:
  - CodeQL: `// codeql[<rule-id>]` on the line above.
  - Semgrep: `// nosemgrep: <rule-id>  # reason` on the same line.

Inline is preferred when the finding is permanent and team-known; Security
tab dismissal is fine for one-off triage decisions.

### Modify a severity threshold

- Trivy: change `severity: "CRITICAL,HIGH"` to add or remove levels.
- Dependency Review: change `fail-on-severity: high` to `critical` (more
  lenient) or `moderate` (stricter).
- Grype: change `severity-cutoff: high`.

Document every change in the PR description so reviewers know.

## 12. Troubleshooting

Common errors we've actually hit, with the fix:

### `Unable to resolve action aquasecurity/trivy-action@X.Y.Z`
The tag doesn't exist. Real tags are `v0.X.Y` or `0.X.Y` depending on the
release. Check <https://github.com/aquasecurity/trivy-action/releases>.
We pin `0.35.0`.

### `[mei-desofs] is an organization. License key is required.` (Gitleaks)
`gitleaks/gitleaks-action@v2` requires a paid licence for org-owned repos
since the Aug-2023 relicensing. Fix: use the gitleaks binary directly
(`secrets-scan.yml` does this — install via curl, run with `--config
.gitleaks.toml`).

### `Dependency review is not supported on this repository`
Enable **Dependency graph** in *Settings → Code security → Dependency
graph*.

### Trivy image scan exits 1 despite empty CRITICAL/HIGH findings
The `aquasecurity/trivy-action` overrides the severity filter to
all-severities when `format: sarif`, so the same run that produces the
comprehensive SARIF also fails on MEDIUM/LOW findings. Fix: set
`limit-severities-for-sarif: true`. The full MEDIUM/LOW report is
preserved in the separate `Trivy — full report (JSON)` step.

### `Invalid SARIF. JSON syntax error: Unexpected end of JSON input` (Grype)
`anchore/scan-action@v4` emits an empty SARIF when there are no findings
above the severity cutoff. Fix: guard the upload step with `if: always()
&& steps.sarif_check.outputs.found == 'true'` where `sarif_check` is a
`run:` block that tests `[ -s "$path" ]`.

### Hadolint passes locally but Trivy IaC scan fails on the Dockerfile
The `aquasecurity/trivy-action@0.35.0` for `scan-type: config` doesn't
respect the `severity` filter — it fails on any finding, even LOW. Fix:
satisfy the rule (e.g. `HEALTHCHECK NONE` for DS-0026). If the rule isn't
fixable, add the rule ID to `.trivyignore`.

### `git push` 403 (wrong GitHub account)
`gh auth setup-git` added a `gh`-backed credential helper specifically for
`https://github.com` that bypasses the Git Credential Manager picker. Fix:
```bash
git config --global --unset-all credential.https://github.com.helper
```

### `dotnet format --verify-no-changes` fails
Run `dotnet format LawyerApp/LawyerApp.sln` locally to apply the fixes,
commit, push.

## 13. Required GitHub settings (outside source code)

These can't be automated from the repo — they're per-repo settings.

### Settings → Code security
1. **Dependabot alerts** — on.
2. **Dependabot security updates** — on (opens PRs for known CVEs).
3. **Dependency graph** — on (required for `dependency-review.yml`).
4. **Code scanning** — on (so SARIF from CodeQL / Semgrep / Trivy / Grype
   / Hadolint / Gitleaks appears under *Code scanning alerts*).
5. **Secret scanning** + **Push protection** — on (complements Gitleaks
   and TruffleHog with GitHub's own scanner; push protection blocks new
   secrets *before* they're committed).

### Settings → Branches → branch protection for `main`
1. Require a pull request before merging.
2. Require status checks to pass (after the first run, these names appear
   in the picker):
   - `Build & Test / Build, Test & Publish`
   - `CodeQL (SAST) / Analyze (csharp)`
   - `Semgrep (SAST) / Semgrep scan`
   - `Dependency Review / Review dependency changes`
   - `Security Scan / SCA — Vulnerable NuGet packages`
   - `Security Scan / SAST — Security Code Scan`
   - `Security Scan / Container Scan — Trivy`
   - `Trivy — Filesystem & IaC Scan / Trivy Filesystem Scan`
   - `SBOM Generation / CycloneDX SBOM (.NET solution)`
   - `SBOM Generation / Syft SBOM (Docker image)`
   - `SBOM Generation / Grype (independent image scan)`
   - `Secrets Scan / Gitleaks`
   - `Secrets Scan / TruffleHog (verified secrets)`
   - `Configuration Validation / Hadolint (Dockerfile)`
   - `Configuration Validation / actionlint (GitHub workflows)`
   - `Configuration Validation / yamllint`
   - `Configuration Validation / JSON / appsettings validation`
   - `Configuration Validation / dotnet format (style)`
3. Require branches to be up to date before merging.
4. Block direct pushes to `main`.

### GitHub Advanced Security (only if private repo)
CodeQL, secret scanning, and dependency review are free on **public**
repos. If the repo is private, an org owner must enable GitHub Advanced
Security for the team — otherwise those three workflows will warn but not
fail.

## 14. SonarCloud (optional setup)

SonarCloud gives per-PR quality gates with code-smell, security-hotspot,
duplication, and coverage overlays — nice to have, not required by the
rubric.

1. Sign in to <https://sonarcloud.io> with the team's GitHub account.
2. *+ → Analyze new project*, pick this repo, choose the **Free plan for
   public projects**.
3. Note the **organisation key** and **project key**.
4. In SonarCloud *My Account → Security* generate a token.
5. In GitHub *Settings → Secrets and variables → Actions* create secret
   `SONAR_TOKEN`.
6. Edit `.github/workflows/sonarcloud.yml`:
   - Replace `<org>` and `<project-key>` placeholders.
   - Uncomment the `pull_request:` and `push:` triggers.

## 15. Mapping to the Sprint 2 rubric

| Rubric criterion | Evidence in this repo |
|---|---|
| **Inventory of components** | SBOMs in [`sbom.yml`](../../../.github/workflows/sbom.yml) (CycloneDX for solution, SPDX for image). |
| **Execution of test plans** | [`build-test.yml`](../../../.github/workflows/build-test.yml) runs xUnit unit + integration tests on every PR; plan documented in [Test_Plan.md](Test_Plan.md). |
| **Static analysis (SAST)** | [`codeql.yml`](../../../.github/workflows/codeql.yml), [`semgrep.yml`](../../../.github/workflows/semgrep.yml), `sast` job in [`security-scan.yml`](../../../.github/workflows/security-scan.yml). Optional: [`sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml). |
| **Software composition analysis (SCA)** | [`dependency-review.yml`](../../../.github/workflows/dependency-review.yml), `sca` job in [`security-scan.yml`](../../../.github/workflows/security-scan.yml), [`dependabot.yml`](../../../.github/dependabot.yml). |
| **Artifact scanning** | `container-scan` job in [`security-scan.yml`](../../../.github/workflows/security-scan.yml), [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml), Grype in [`sbom.yml`](../../../.github/workflows/sbom.yml). |
| **Configuration validation** | [`config-validation.yml`](../../../.github/workflows/config-validation.yml) (Hadolint + actionlint + yamllint + JSON / secret guard) and [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml) (IaC mode). |
| **Secret detection** | [`secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml) (Gitleaks + TruffleHog). |
| **Dynamic analysis (DAST)** | Out of scope for Pipeline Automation in Sprint 2 — owned by the Security Testing track. |
| **Pipeline automation** | All practices above run on every PR with no manual step required. |

## 16. Maintenance & lifecycle

- **Update cadence**: Dependabot raises action / NuGet / Docker base PRs
  every Monday morning. Merge them via the same gated PR flow.
- **Adding a new test project**: drop a `*.Tests.csproj` next to the
  solution and add it to `LawyerApp.sln` — the existing `dotnet test`
  command runs the whole solution so it'll be picked up automatically.
- **Tuning failure thresholds**: each scanner exposes a severity knob —
  `fail-on-severity` (Dependency Review), `severity-cutoff` (Grype), the
  Trivy `severity:` list, etc. Start strict (CRITICAL/HIGH fail), relax
  only with a documented exception.
- **Audit of suppressions**: re-review `.trivyignore` and `.gitleaks.toml`
  every quarter. Remove entries that are no longer needed (fix landed
  upstream, secret was rotated).
- **Action version updates**: Dependabot opens a PR for each major. Read
  the action's CHANGELOG before merging; some bumps change defaults.
- **Pipeline runtime**: target keeps the whole PR-gate suite under
  ~10 minutes wall-clock. If any single workflow grows past 15 min,
  consider splitting it.

## 17. Glossary

- **SAST** — *Static Application Security Testing*. Analyses source code
  (or built artifacts) without running it. Examples here: CodeQL,
  Semgrep, .NET analyzers, Security Code Scan, SonarCloud.
- **DAST** — *Dynamic Application Security Testing*. Tests a running
  instance of the application (e.g. OWASP ZAP, Burp). Out of scope for
  this Sprint 2 pipeline; lives in the Security Testing track.
- **IAST** — *Interactive Application Security Testing*. Hybrid; an
  agent inside the running app observes runtime data flows. Also out of
  scope for Sprint 2.
- **SCA** — *Software Composition Analysis*. Identifies known-vulnerable
  versions of third-party dependencies. Tools here: GitHub Dependency
  Review, `dotnet list --vulnerable`, Dependabot.
- **SBOM** — *Software Bill of Materials*. Machine-readable inventory of
  every component shipped in a build. Standards: CycloneDX (OWASP),
  SPDX (Linux Foundation).
- **SARIF** — *Static Analysis Results Interchange Format*. OASIS-
  standard JSON schema for security-tool findings. Every scanner in this
  pipeline emits SARIF so GitHub's Code Scanning UI can render them.
- **CVE** — *Common Vulnerabilities and Exposures*. The MITRE-maintained
  catalogue of publicly disclosed vulnerabilities (`CVE-YYYY-NNNNN`).
- **CWE** — *Common Weakness Enumeration*. Higher-level classification
  of vulnerability types (e.g. CWE-79 = XSS). Most SAST findings cite a
  CWE.
- **Trust boundary** — Where data crosses from one privilege/trust level
  to another. Threats are often analysed at trust boundaries (STRIDE).
- **PR-time gate** — A check that runs on every pull request and must
  pass before merge.
- **SSDLC** — *Secure Software Development Lifecycle*. Process model
  this project follows.
- **OWASP ASVS** — *Application Security Verification Standard*. OWASP's
  catalogue of testable security requirements. Pipeline maps to V14
  (Configuration), V10 (Malicious Code), V14.2 (Dependency).
- **SSDF** — NIST SP 800-218 *Secure Software Development Framework*.
  Higher-level practice catalogue. This pipeline automates PW.4 (reuse
  security guidance), PW.5 (create well-secured software), PW.7 (review
  software for vulnerabilities), PW.8 (test software), RV.1 (identify
  vulnerabilities on a continual basis).
- **STRIDE** — Microsoft threat-modelling taxonomy: Spoofing, Tampering,
  Repudiation, Information disclosure, Denial of service, Elevation of
  privilege. Used in Phase 1 threat modelling.

## 18. References

- OWASP — *Application Security Verification Standard v4*:
  <https://owasp.org/www-project-application-security-verification-standard/>
- NIST — *SP 800-218 Secure Software Development Framework (SSDF)*:
  <https://csrc.nist.gov/publications/detail/sp/800-218/final>
- GitHub Docs — *About code scanning*:
  <https://docs.github.com/en/code-security/code-scanning/introduction-to-code-scanning/about-code-scanning>
- GitHub Docs — *Keeping your supply chain secure with Dependabot*:
  <https://docs.github.com/en/code-security/dependabot>
- GitHub Docs — *About secret scanning*:
  <https://docs.github.com/en/code-security/secret-scanning/about-secret-scanning>
- CodeQL — <https://codeql.github.com/>
- Semgrep registry — <https://semgrep.dev/explore>
- Trivy docs — <https://trivy.dev/docs/>
- Anchore (Syft + Grype) — <https://github.com/anchore/syft>, <https://github.com/anchore/grype>
- Gitleaks — <https://github.com/gitleaks/gitleaks>
- TruffleHog — <https://github.com/trufflesecurity/trufflehog>
- Hadolint — <https://github.com/hadolint/hadolint>
- actionlint — <https://github.com/rhysd/actionlint>
- CycloneDX — <https://cyclonedx.org/>
- SPDX — <https://spdx.dev/>
