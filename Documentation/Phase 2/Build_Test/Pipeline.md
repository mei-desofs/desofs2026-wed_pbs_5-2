# CI/CD Pipeline — LawyerApp

> Sprint 2 deliverable for **Pipeline Automation** and the pipeline side of
> **Build & Test**. All workflows live under
> [`.github/workflows`](../../../.github/workflows) and trigger on every
> pull request to `main`/`develop`, on push to those branches, and on
> weekly schedules for drift detection. Companion documents:
> [Test_Plan.md](Test_Plan.md), [Artifact_Scanning.md](Artifact_Scanning.md),
> [Technical_Report.md](Technical_Report.md).

## 1. Design overview

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

### Trigger matrix

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

## 2. Practices adopted

The pipeline implements the practices required by the Sprint 2 rubric
("Pipeline automation 20%" + "Build & Test 30%"):

### Build & Test
- **Reproducible builds**: pinned .NET 8 SDK via `actions/setup-dotnet`,
  NuGet cache keyed on `*.csproj` hashes, build runs in `Release` and
  publishes a deployable artifact.
- **Tests on every PR**: xUnit + Moq + FluentAssertions cover unit and
  integration layers (see [Test_Plan.md](Test_Plan.md)). EF Core
  InMemory provider replaces PostgreSQL in tests for full isolation.
- **TRX + Cobertura coverage uploaded** as workflow artifacts (`test-results`,
  `coverage-report`) for inspection in the Actions UI.
- **Production payload uploaded** as `lawyerapp-publish` (7-day retention).

### SAST (Static Application Security Testing)
Three layers, plus an opt-in fourth:
- **GitHub CodeQL** (primary) — `security-and-quality` query suite. SARIF
  → *Security → Code scanning*.
- **Semgrep** (additional) — `p/default p/security-audit p/csharp
  p/dockerfile p/secrets` rulesets, SARIF → same tab.
- **In-build .NET analyzers + Security Code Scan** — runs as `sast` job in
  `security-scan.yml`, promotes selected `CA` rules to errors.
- **SonarCloud** (optional) — scaffolded as manual-trigger until
  `SONAR_TOKEN` is configured (see [§5](#5-things-you-still-need-to-do-outside-source-code)).

### SCA (Software Composition Analysis)
- **GitHub Dependency Review** runs on every PR and fails on any newly
  introduced `high` severity advisory or copyleft (GPL/AGPL) license.
- **`dotnet list package --vulnerable --include-transitive`** in CI fails
  the build on any vulnerable direct or transitive package.
- **`dotnet list package --deprecated` / `--outdated`** for visibility.
- **Dependabot** opens grouped pull requests weekly for NuGet (per project),
  GitHub Actions, and Docker base-image updates (see
  [`.github/dependabot.yml`](../../../.github/dependabot.yml)).
- **CycloneDX SBOM** (solution) and **SPDX SBOM** (image) generated each
  run — gives auditors a per-build component inventory.

### Artifact (container) scanning
- **Trivy image scan** — OS + library CVEs (CRITICAL/HIGH, ignore-unfixed)
  fails the build; SARIF uploaded.
- **Trivy filesystem scan** — repository-level vulnerable lockfiles /
  packages.
- **Trivy IaC/config scan** — Dockerfile + IaC misconfigurations
  (CRITICAL/HIGH fails the build).
- **Syft (Anchore)** produces an SPDX SBOM of the image.
- **Grype (Anchore)** scans the same image as an independent second
  opinion; SARIF uploaded under category `grype`.

### Configuration validation
- **Hadolint** — Dockerfile best-practice and security linting (SARIF +
  error-threshold gate).
- **actionlint** — validates every workflow YAML, catches shell-injection
  patterns in `run:` blocks, unknown action inputs.
- **yamllint** — generic YAML hygiene on `.github/**`.
- **JSON validation** — `jq empty` on every `appsettings*.json` plus a regex
  guard that fails CI if a likely plaintext secret is committed (dev
  placeholders allowed only in `appsettings.Development.json`).
- **`dotnet format --verify-no-changes`** — flags style/whitespace drift.

### Secret detection
- **Gitleaks** runs on full git history (`fetch-depth: 0`) — uploads a
  summary artifact and writes PR annotations.
- **TruffleHog** runs in `--results=verified,unknown` mode to focus on
  credentials it can actively verify, dramatically lowering false positives.

### Pipeline hygiene
- **Least-privilege tokens**: every workflow declares an explicit
  `permissions:` block (default `contents: read`). Jobs publishing to the
  Security tab get `security-events: write`; Dependency Review gets
  `pull-requests: write` for the inline summary. No workflow uses the
  default permissive token.
- **Pinned action versions**: every third-party action uses a major-version
  tag (`@v4`, `@v3`, ...) or an explicit release tag for security-relevant
  actions (`aquasecurity/trivy-action@0.28.0`). Dependabot lifts these
  forward and produces a reviewable PR with release notes.
- **`timeout-minutes`** on every job to bound the worst case.
- **`workflow_dispatch:`** on every workflow so any check can be re-run
  manually from the Actions tab without pushing a new commit.

## 3. Workflow reference

### `build-test.yml`
Restore → cache NuGet → build (Release) → run xUnit tests (TRX + Cobertura)
→ publish payload. Uploads `test-results`, `coverage-report`,
`lawyerapp-publish` artifacts.

### `codeql.yml`
Builds C# and runs the `security-and-quality` query pack. Weekly schedule
catches advisories on dormant branches.

### `semgrep.yml`
Runs `semgrep ci` against five rulesets (default, security-audit, csharp,
dockerfile, secrets). SARIF uploaded.

### `sonarcloud.yml`
Scaffolded as `workflow_dispatch` only. After [setting up
SonarCloud](#sonarcloud-optional) and uncommenting the PR/push triggers,
gives you per-PR quality gates with code-smell, security-hotspot,
duplication, and coverage metrics.

### `dependency-review.yml`
First-party GitHub action that compares the PR's lockfile / csproj diff
against the GitHub Advisory Database. Fails on `high` or copyleft licenses
introduced in the PR.

### `security-scan.yml`
Three jobs: `sca` (`dotnet list --vulnerable`), `sast` (Roslyn analyzers +
Security Code Scan), `container-scan` (Trivy image + JSON report).

### `trivy-config.yml`
Two scans: filesystem (`fs`) for vulnerable lockfiles, and config (`config`)
for Dockerfile/IaC misconfigurations. Both fail on CRITICAL/HIGH and
upload SARIF.

### `sbom.yml`
Three jobs: CycloneDX SBOM of the solution, Syft SPDX SBOM of the image,
Grype scan of the image as a second opinion.

### `secrets-scan.yml`
Gitleaks on full history + TruffleHog on the PR diff range with
verified-only results.

### `config-validation.yml`
Five independent jobs: Hadolint, actionlint, yamllint, JSON +
appsettings secret guard, `dotnet format`.

## 4. Mapping to the Sprint 2 rubric

| Rubric criterion | Evidence in this repo |
|---|---|
| Inventory of components | SBOMs in [`sbom.yml`](../../../.github/workflows/sbom.yml) (CycloneDX for solution, SPDX for image). |
| Execution of test plans | [`build-test.yml`](../../../.github/workflows/build-test.yml) runs xUnit unit + integration tests on every PR; plan documented in [Test_Plan.md](Test_Plan.md). |
| Static analysis (SAST) | [`codeql.yml`](../../../.github/workflows/codeql.yml), [`semgrep.yml`](../../../.github/workflows/semgrep.yml), `sast` job in [`security-scan.yml`](../../../.github/workflows/security-scan.yml). Optional: [`sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml). |
| Software composition analysis (SCA) | [`dependency-review.yml`](../../../.github/workflows/dependency-review.yml), `sca` job in [`security-scan.yml`](../../../.github/workflows/security-scan.yml), [`dependabot.yml`](../../../.github/dependabot.yml). |
| Artifact scanning | `container-scan` job in [`security-scan.yml`](../../../.github/workflows/security-scan.yml) + [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml) + Grype in [`sbom.yml`](../../../.github/workflows/sbom.yml). |
| Configuration validation | [`config-validation.yml`](../../../.github/workflows/config-validation.yml) (Hadolint + actionlint + yamllint + JSON / secret guard) and [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml) (IaC mode). |
| Dynamic analysis (DAST) | Out of scope for Pipeline Automation in Sprint 2 — owned by the Security Testing track. |
| Pipeline automation | All practices above run on every PR with no manual step required. |

## 5. Things you still need to do (outside source code)

Most workflows work the moment the branch is merged, but a handful of
**repository-level toggles** in GitHub unlock the full value (and three are
optional integrations with external SaaS).

### Required GitHub repository settings

In **Settings → Code security**:
1. Enable **Dependabot alerts**.
2. Enable **Dependabot security updates** (lets it open PRs for known CVEs).
3. Enable **Dependency graph** (usually already on for public repos).
4. Enable **Code scanning** — needed so SARIF from CodeQL / Semgrep / Trivy /
   Grype / Hadolint shows up under *Security → Code scanning alerts*.
5. Enable **Secret scanning** (and **push protection** if available) — runs
   GitHub's own scanner in addition to Gitleaks / TruffleHog.

In **Settings → Branches** (branch protection for `main`):
1. Require a pull request before merging.
2. Require status checks to pass — at minimum check these (the names match
   the workflow `name:` plus the job `name:`):
   - `Build & Test / Build, Test & Publish`
   - `CodeQL (SAST) / Analyze (csharp)`
   - `Semgrep (SAST) / Semgrep scan`
   - `Dependency Review / Review dependency changes`
   - `Security Scan / SCA — Vulnerable NuGet packages`
   - `Security Scan / Container Scan — Trivy`
   - `Trivy — Filesystem & IaC Scan / Trivy Filesystem Scan`
   - `Secrets Scan / Gitleaks`
   - `Configuration Validation / Hadolint (Dockerfile)`
   - `Configuration Validation / actionlint (GitHub workflows)`
   - `Configuration Validation / JSON / appsettings validation`
3. Require branches to be up to date before merging.
4. Block direct pushes to `main`.

### SonarCloud (optional)
1. Sign in to <https://sonarcloud.io> with the team's GitHub account.
2. *+ → Analyze new project*, pick this repo, choose the **Free plan for
   public projects**.
3. Note the **organisation key** and **project key** SonarCloud generates.
4. In SonarCloud *My Account → Security* generate a token.
5. In GitHub *Settings → Secrets and variables → Actions* create secret
   `SONAR_TOKEN` with that value.
6. Edit `.github/workflows/sonarcloud.yml`:
   - Replace `<org>` and `<project-key>` placeholders with your values.
   - Uncomment the `pull_request:` and `push:` triggers.

### GitHub Advanced Security (only if private repo)
CodeQL, secret scanning, and dependency review are free on **public** repos.
If your team's repo is private, an instructor can enable **GitHub Advanced
Security** for the org — until then, those three workflows will warn but
not fail.

## 6. Maintenance & evolution

- **Update cadence**: Dependabot raises action / NuGet / Docker base PRs
  every Monday morning. Merge them via the same gated PR flow.
- **Adding a new test project**: drop a `*.Tests.csproj` next to the
  solution and add it to `LawyerApp.sln` — the existing `dotnet test`
  command runs the whole solution so it'll be picked up automatically.
- **Tuning failure thresholds**: each scanner exposes a severity knob —
  `fail-on-severity` (Dependency Review), `severity-cutoff` (Grype), the
  Trivy `severity:` list, etc. Start strict (CRITICAL/HIGH fail), relax
  only with a documented exception.
- **Suppressing a known CVE**: add the CVE ID with a justification comment
  to [`LawyerApp/.trivyignore`](../../../LawyerApp/.trivyignore).
- **Local reproduction**: every check can be run locally — `dotnet build`,
  `dotnet list package --vulnerable`, `docker build -t lawyerapp .` then
  `trivy image lawyerapp`, etc. Same tooling, same results.

## 7. Lineage / references

- OWASP ASVS v4 — control families addressed by the pipeline: V14
  (Configuration), V10 (Malicious code, via SAST), V14.2 (Dependency).
- NIST SP 800-218 *Secure Software Development Framework* (SSDF) —
  practices PW.4, PW.5, PW.7, PW.8, RV.1 are automated here.
- GitHub Docs — *Keeping your supply chain secure with Dependabot*,
  *About code scanning*, *About secret scanning*.
