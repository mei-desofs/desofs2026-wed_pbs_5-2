# Configuração de Análise de Artefactos — LawyerApp

**Versão:** 1.1
**Data:** 2026-05-17
**Autor:** Grupo Wed PBS 5-2 — Build & Test

> Complemento ao [Pipeline.md](Pipeline.md) — a referência operacional
> completa da pipeline. Este documento foca-se especificamente na
> **análise de artefactos** (analisadores, política de severidades,
> processo de supressão). Para o desenho dos workflows, matriz de
> disparos, reprodução local, resolução de problemas, glossário e
> checklist de configuração, ver [Pipeline.md](Pipeline.md).

---

## 1. Visão geral

Este documento descreve a estratégia de análise de artefactos integrada
na pipeline CI/CD do LawyerApp. O objetivo é detetar vulnerabilidades de
segurança em dependências, imagens de contentor, código-fonte e segredos
antes que qualquer artefacto chegue a produção.

---

## 2. Ferramentas de análise

| Ferramenta | Tipo | Disparo | Bloqueia merge? |
|---|---|---|---|
| `dotnet list package --vulnerable` | SCA — dependências NuGet | Cada push / PR / semanal | Sim (qualquer pacote vulnerável) |
| GitHub Dependency Review | SCA — diff do PR | Cada PR | Sim (severidade `high` ou GPL/AGPL) |
| Dependabot | SCA — PRs automáticos de atualização | Semanal | n/a (levanta PRs) |
| CodeQL | SAST (primária) | Cada push / PR / semanal | Ocorrências críticas do CodeQL |
| Semgrep | SAST (adicional) | Cada push / PR / semanal | Regras bloqueantes dos packs escolhidos |
| Analisadores Roslyn .NET + Security Code Scan | SAST (em build) | Cada build | Configurável |
| SonarCloud | SAST (opcional) | Manual até `SONAR_TOKEN` estar configurado | Quality gate por PR |
| Trivy (análise de imagem) | Análise de vulnerabilidades em contentores | Cada push / PR | Sim (CRITICAL/HIGH com correção) |
| Trivy (filesystem + IaC) | Más configurações de código-fonte / IaC | Cada push / PR | Sim (CRITICAL/HIGH) |
| Syft (Anchore) | SBOM — imagem de contentor | Cada push / PR / semanal | n/a (artefacto de evidência) |
| CycloneDX | SBOM — solução .NET | Cada push / PR / semanal | n/a (artefacto de evidência) |
| Grype (Anchore) | Análise independente da imagem | Cada push / PR / semanal | Não (segunda opinião) |
| Gitleaks | Deteção de segredos (histórico git) | Cada push / PR / semanal | Sim |
| TruffleHog | Deteção de segredos (verificados) | Cada push / PR / semanal | Sim |
| Hadolint | Lint ao Dockerfile | Cada push / PR | Sim (severidade `error`) |
| actionlint | Validação do YAML dos workflows | Cada push / PR | Sim |
| yamllint | Higiene de YAML (`.github/`) | Cada push / PR | Sim |
| jq + guard regex | Sintaxe de `appsettings*.json` + guard de segredos em texto | Cada push / PR | Sim |

---

## 3. Software Composition Analysis (SCA)

**Ferramenta:** `dotnet list package --vulnerable --include-transitive`
**Workflow:** [security-scan.yml](../../../.github/workflows/security-scan.yml) → job `sca`

### O que verifica
- Todas as dependências NuGet diretas e transitivas declaradas em
  `LawyerApp.csproj`.
- Dados de vulnerabilidade obtidos da base de dados de vulnerabilidades
  do NuGet (atualizada pela Microsoft).

### Condição de falha
Se for detetado qualquer pacote com um CVE conhecido, o passo de CI sai
com código 1 e o relatório é carregado como artefacto `sca-report`.

### Dependências atuais revistas

| Pacote | Versão | Estado |
|---|---|---|
| BCrypt.Net-Next | 4.1.0 | Sem CVEs conhecidos |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 | Sem CVEs conhecidos |
| Swashbuckle.AspNetCore | 6.6.2 | Sem CVEs conhecidos |
| VaultSharp | 1.17.5.1 | Sem CVEs conhecidos |
| Microsoft.EntityFrameworkCore.Design | 8.0.4 | Sem CVEs conhecidos |

---

## 4. Análise de imagem de contentor (Trivy)

**Ferramenta:** [Trivy](https://github.com/aquasecurity/trivy) da Aqua Security
**Workflow:** [security-scan.yml](../../../.github/workflows/security-scan.yml) → job `container-scan`
**Ficheiro de ignore:** [LawyerApp/.trivyignore](../../../LawyerApp/.trivyignore)

### Configuração da análise

```yaml
image-ref: "lawyerapp:<git-sha>"
severity: "CRITICAL,HIGH"
exit-code: "1"          # Falhar em ocorrências
ignore-unfixed: true    # Ignorar CVEs sem correção disponível
format: "sarif"         # Carregado para o separador GitHub Security
```

### Imagem base
O Dockerfile usa `mcr.microsoft.com/dotnet/aspnet:8.0` (fase final) e
`mcr.microsoft.com/dotnet/sdk:8.0` (fase de build). A Microsoft aplica
patches ativamente a estas imagens; a análise agendada semanalmente
(`cron: "0 6 * * 1"`) garante que CVEs recém-divulgados são apanhados
mesmo sem um push de código.

### Ignorar um CVE
Se um CVE for revisto e aceite (por exemplo, não explorável na nossa
configuração de deployment), adicionar o ID do CVE ao `.trivyignore`:

```
CVE-AAAA-NNNNN  # Revisto <data>: <motivo>
```

---

## 5. Static Application Security Testing (SAST)

O SAST é organizado em camadas com três motores para apanharmos
problemas que qualquer ferramenta isolada deixaria passar:

- **GitHub CodeQL** (primária) — workflow
  [`codeql.yml`](../../../.github/workflows/codeql.yml). Corre o pacote
  de queries `security-and-quality` para C# em cada push, PR e
  semanalmente. Ocorrências aparecem em *Security → Code scanning*.
- **Semgrep** (adicional, regras abertas) — workflow
  [`semgrep.yml`](../../../.github/workflows/semgrep.yml). Corre `p/default`,
  `p/security-audit`, `p/csharp`, `p/dockerfile`, e `p/secrets`. SARIF é
  carregado para o mesmo Security tab.
- **SonarCloud** (opcional) — workflow
  [`sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml).
  Desativado por defeito; ativar adicionando o secret `SONAR_TOKEN` e
  descomentando os disparos de PR/push. Ver Pipeline.md §5 para
  configuração.
- **SAST em build** (analisadores Roslyn + Security Code Scan) — corre
  como parte do job `sast` do
  [`security-scan.yml`](../../../.github/workflows/security-scan.yml). As
  regras `CA` selecionadas abaixo são promovidas a erros para que façam
  falhar a build.

### Regras Roslyn obrigatórias como erros (faz falhar a build)

| Regra | Descrição |
|---|---|
| CA2100 | Queries SQL não devem ser construídas a partir de input do utilizador |
| CA3001 | Rever código à procura de vulnerabilidades SQL injection |
| CA3002 | Rever código à procura de vulnerabilidades XSS |
| CA3003 | Rever código à procura de vulnerabilidades de path injection em ficheiros |
| CA3006 | Rever código à procura de command injection em processos |
| CA3007 | Rever código à procura de vulnerabilidades de open redirect |
| CA3010 | Rever código à procura de vulnerabilidades XAML injection |
| CA3011 | Rever código à procura de vulnerabilidades DLL injection |
| CA3012 | Rever código à procura de vulnerabilidades regex injection |

### Execução local

```bash
dotnet build LawyerApp/LawyerApp.sln -c Release \
  /p:EnableNETAnalyzers=true \
  /p:AnalysisMode=All \
  /warnaserror:CA2100,CA3001,CA3002,CA3003,CA3006,CA3007,CA3010,CA3011,CA3012
```

---

## 6. Deteção de segredos (Gitleaks + TruffleHog)

**Workflow:** [`secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml)

Dois analisadores correm em paralelo para apanhar fugas a partir de
ângulos diferentes:

- **Gitleaks** — job `gitleaks`. Faz checkout do histórico git completo
  (`fetch-depth: 0`) e corre o conjunto de regras predefinido do
  Gitleaks contra cada commit. Carrega um artefacto de resumo e escreve
  anotações no PR em caso de ocorrências.
- **TruffleHog** — job `trufflehog`. Corre o binário diretamente
  (versão `v3.95.3`) em vez da action `trufflesecurity/trufflehog@main`
  porque a action depende de uma imagem em `ghcr.io` que falha por
  timeout nos runners GitHub-hosted. Em PRs analisa o diff entre base
  e head; em push/schedule/dispatch analisa o histórico completo,
  sempre com `--results=verified` para nos focarmos em credenciais que
  o TruffleHog pode confirmar contra a API do emissor (sem falsos
  positivos por definição). Resultados "unknown" (alta entropia mas
  sem verificação) são deixados para o Gitleaks, que corre em paralelo
  com o ruleset por defeito.

Ambos os jobs fazem o workflow falhar em qualquer ocorrência,
bloqueando o PR.

### Notas para programadores
- Nunca fazer commit de `appsettings.Development.json` com credenciais
  reais.
- Usar `dotnet user-secrets` para desenvolvimento local.
- As connection strings da base de dados devem ser obtidas do HashiCorp
  Vault em produção (ver a secção do Vault no `Program.cs`).
- Padrões de segredos em texto em `appsettings*.json` também são
  apanhados pelo job `json-validation` em
  [`config-validation.yml`](../../../.github/workflows/config-validation.yml).

---

## 7. Análise de filesystem e IaC

**Ferramenta:** Trivy (modo filesystem + config/IaC)
**Workflow:** [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml)

Duas análises correm neste workflow:
- **Análise filesystem** (`scan-type: fs`) — olha para `LawyerApp/` à
  procura de pacotes vulneráveis e lockfiles embebidos. Falha em
  CRITICAL/HIGH com correção.
- **Análise IaC / config Dockerfile** (`scan-type: config`) — olha para
  o repositório inteiro à procura de más configurações no Dockerfile
  (utilizador root, `USER` em falta, `FROM` sem versão, etc.) e
  quaisquer ficheiros IaC. Falha em CRITICAL/HIGH.

Ambas produzem SARIF carregado para *Security → Code scanning* nas
categorias `trivy-fs` e `trivy-config`.

### Linter complementar do Dockerfile

[`config-validation.yml`](../../../.github/workflows/config-validation.yml)
corre o **Hadolint** contra `LawyerApp/Dockerfile` primeiro como
verificação SARIF e depois novamente com `failure-threshold: error` —
a segunda invocação bloqueia o PR em ocorrências de nível error.

---

## 8. Software Bills of Materials (SBOMs)

**Workflow:** [`sbom.yml`](../../../.github/workflows/sbom.yml)

Dois SBOMs complementares são gerados em cada push e PR:

- **CycloneDX (solução .NET)** — job `cyclonedx-solution`. Usa a
  ferramenta global `CycloneDX` para percorrer cada referência de
  pacote em `LawyerApp.sln` e emitir um SBOM JSON. Carregado como
  `sbom-cyclonedx` (retenção 90 dias).
- **Syft (imagem Docker, SPDX)** — job `syft-image`. Constrói a imagem
  de produção e corre a `sbom-action` da Anchore para emitir SPDX-JSON.
- **Análise Grype** — job `grype-second-opinion`. Corre o analisador
  de vulnerabilidades da Anchore contra a mesma imagem como segunda
  opinião independente face ao Trivy. O SARIF é carregado na categoria
  `grype`.

## 9. Retenção de artefactos

Todos os relatórios de análise são carregados como artefactos do GitHub
Actions:

| Artefacto | Formato | Retenção |
|---|---|---|
| `sca-report` | Texto | 30 dias |
| `sast-report` | Texto | 30 dias |
| `trivy-report` | SARIF + JSON | 30 dias |
| `semgrep-report` | SARIF | 30 dias |
| `sbom-cyclonedx` | CycloneDX JSON | 90 dias |
| `lawyerapp-image-sbom.spdx.json` | SPDX JSON | 90 dias |
| `test-results` | TRX | 30 dias |
| `coverage-report` | Cobertura XML | 30 dias |
| `lawyerapp-publish` | Payload de publish .NET | 7 dias |

Os ficheiros SARIF também são carregados para o separador **GitHub
Security → Code scanning** para visibilidade persistente (categorias:
`trivy-image`, `trivy-fs`, `trivy-config`, `semgrep`, `hadolint`,
`grype`).
