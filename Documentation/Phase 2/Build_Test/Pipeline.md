# Pipeline CI/CD — LawyerApp

> Entregável do Sprint 2 para **Automação de Pipeline** e a vertente de
> pipeline de **Build & Test**. Referência operacional + documento de
> desenho.
>
> Todos os workflows estão em [`.github/workflows`](../../../.github/workflows)
> e disparam em cada pull request para `main`/`develop`, em cada push para
> esses ramos, e em agendamentos semanais para deteção de regressões.
>
> Documentos complementares: [Test_Plan.md](Test_Plan.md),
> [Artifact_Scanning.md](Artifact_Scanning.md),
> [Technical_Report.md](Technical_Report.md).

---

## Índice

1. [Visão executiva](#1-visão-executiva)
2. [Visão de desenho](#2-visão-de-desenho)
3. [Matriz de disparos](#3-matriz-de-disparos)
4. [Práticas adotadas](#4-práticas-adotadas)
5. [Análise dos workflows](#5-análise-dos-workflows)
6. [Ficheiros de configuração](#6-ficheiros-de-configuração)
7. [Ferramentas utilizadas e justificação](#7-ferramentas-utilizadas-e-justificação)
8. [Executar cada verificação localmente](#8-executar-cada-verificação-localmente)
9. [Interpretação das ocorrências](#9-interpretação-das-ocorrências)
10. [Resposta a uma verificação que falha](#10-resposta-a-uma-verificação-que-falha)
11. [Adicionar / modificar / suprimir verificações](#11-adicionar--modificar--suprimir-verificações)
12. [Resolução de problemas](#12-resolução-de-problemas)
13. [Definições de GitHub necessárias (fora do código)](#13-definições-de-github-necessárias-fora-do-código)
14. [SonarCloud (configuração opcional)](#14-sonarcloud-configuração-opcional)
15. [Mapeamento para o rubric do Sprint 2](#15-mapeamento-para-o-rubric-do-sprint-2)
16. [Manutenção e ciclo de vida](#16-manutenção-e-ciclo-de-vida)
17. [Glossário](#17-glossário)
18. [Referências](#18-referências)

---

## 1. Visão executiva

A pipeline é composta por **dez workflows do GitHub Actions** + uma
configuração do Dependabot + três ficheiros de política (`.gitleaks.toml`,
`.trivyignore`, `.gitignore`). Cada pull request para `main` ou `develop`
executa **todas** as verificações em paralelo. A possibilidade de fazer
merge é controlada por regras de status check na proteção de branch.

A pipeline responde a oito perguntas do SSDLC em cada PR:

| Pergunta | Respondida por |
|---|---|
| Compila? | `build-test.yml` |
| Os testes passam? | `build-test.yml` (xUnit + integração) |
| O código-fonte está livre de anti-padrões de segurança conhecidos? | `codeql.yml`, `semgrep.yml`, job `sast` em `security-scan.yml` |
| Alguma dependência tem CVE conhecido? | `dependency-review.yml`, job `sca` em `security-scan.yml`, Dependabot |
| A imagem de contentor tem CVEs que não suprimimos? | `security-scan.yml` (Trivy image), `trivy-config.yml`, job Grype em `sbom.yml` |
| Foi feito commit de algum segredo? | `secrets-scan.yml` (Gitleaks + TruffleHog) |
| O nosso Dockerfile / YAML / JSON está bem formado? | `config-validation.yml`, `trivy-config.yml` |
| Conseguimos produzir um inventário auditável daquilo que entra na build? | `sbom.yml` (CycloneDX + Syft SPDX) |

Tudo reporta SARIF para **Security → Code scanning** para que as
ocorrências sejam deduplicadas e persistam entre execuções. As falhas são
sumarizadas no separador de checks do PR.

## 2. Visão de desenho

A pipeline é decomposta em workflows focados que correm em paralelo em
cada pull request. Dividi-los mantém os ciclos de feedback rápidos,
facilita marcar cada um como obrigatório na proteção de branch e permite
que cada ferramenta publique SARIF para o **separador GitHub Security**
sem que um job bloqueie o outro.

```
                       ┌──────────────────────────────────────────────┐
                       │           Pull request para main             │
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
  │           Security Scan (SCA + SAST de analisadores .NET + Trivy image)         │
  └─────────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │                      Validação de Configuração                                   │
  │       Hadolint · actionlint · yamllint · JSON / appsettings · dotnet format      │
  └─────────────────────────────────────────────────────────────────────────────────┘
       │
       └───► Todas as ocorrências agregadas em Security → Code scanning (SARIF) ◄───
```

### Princípios de desenho

- **Workflows focados em vez de um só monolítico** — mais fácil marcar
  cada um como obrigatório na proteção de branch, mais fácil voltar a
  executar jobs individuais, mais fácil ler logs.
- **Falhar fechado, documentar aberto** — qualquer ocorrência HIGH/CRITICAL
  faz falhar o PR por defeito. Para fazer bypass, adicionar uma entrada de
  supressão documentada (`.trivyignore`, `.gitleaks.toml`) para que o
  revisor veja a justificação no diff.
- **Defesa em profundidade** — pelo menos duas ferramentas independentes
  cobrem cada área do rubric (SAST: CodeQL + Semgrep; SCA: Dependency
  Review + `dotnet list`; Imagem: Trivy + Grype; Segredos: Gitleaks +
  TruffleHog).
- **Versões fixadas** — todas as actions de terceiros têm tag de versão
  explícita. O Dependabot levanta PRs de atualização semanalmente para que
  as atualizações sejam visíveis e revistas, nunca silenciosas.
- **Privilégio mínimo** — todos os workflows declaram um bloco
  `permissions:` explícito (por defeito `contents: read`). Jobs que
  publicam para o Security tab adicionam `security-events: write`; o
  Dependency Review adiciona `pull-requests: write` para o sumário inline.
  Nenhum workflow usa o token permissivo por defeito.

## 3. Matriz de disparos

| Workflow | Pull request | Push | Agendamento (semanal) | Manual |
|---|:---:|:---:|:---:|:---:|
| [`build-test.yml`](../../../.github/workflows/build-test.yml) | sim | sim |  | sim |
| [`codeql.yml`](../../../.github/workflows/codeql.yml) | sim | sim | sim | sim |
| [`semgrep.yml`](../../../.github/workflows/semgrep.yml) | sim | sim | sim | sim |
| [`sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml) | (após configuração) | (após configuração) |  | sim |
| [`dependency-review.yml`](../../../.github/workflows/dependency-review.yml) | sim |  |  |  |
| [`security-scan.yml`](../../../.github/workflows/security-scan.yml) | sim | sim | sim | sim |
| [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml) | sim | sim |  | sim |
| [`sbom.yml`](../../../.github/workflows/sbom.yml) | sim | sim | sim | sim |
| [`secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml) | sim | sim | sim | sim |
| [`config-validation.yml`](../../../.github/workflows/config-validation.yml) | sim | sim |  | sim |

Os agendamentos semanais expõem advisórias que surgem **após** os
congelamentos de código — uma análise sem alterações ainda apanha CVEs
novos em dependências fixadas.

## 4. Práticas adotadas

### Build & Test
- Builds reprodutíveis: SDK .NET 8 fixado, cache NuGet indexada por
  `*.csproj`, configuração `Release`.
- Testes xUnit + Moq + FluentAssertions em cada PR (ver
  [Test_Plan.md](Test_Plan.md)).
- Cobertura TRX + Cobertura carregada como artefactos.
- Payload de produção carregado como `lawyerapp-publish`.

### SAST (Static Application Security Testing)
- **CodeQL** (conjunto de queries `security-and-quality`) — primária.
- **Semgrep** (`p/default p/security-audit p/csharp p/dockerfile p/secrets`)
  — adicional.
- **Analisadores .NET + Security Code Scan** — corre em build, promove
  regras `CA` selecionadas a erros.
- **SonarCloud** — opcional, em manual-trigger.

### SCA (Software Composition Analysis)
- **GitHub Dependency Review** — gate de CVE + licença sobre o diff do PR.
- **`dotnet list package --vulnerable --include-transitive`** — falha se
  existir qualquer pacote vulnerável, direto ou transitivo.
- **`--deprecated`/`--outdated`** — só visibilidade.
- **Dependabot** — PRs semanais agrupados para NuGet, Actions, Docker.
- **CycloneDX SBOM** + **Syft SPDX SBOM** — inventário de componentes por
  build.

### Análise de artefactos (contentor)
- **Análise Trivy à imagem** — CVEs de SO + bibliotecas, falha em
  CRITICAL/HIGH.
- **Análise Trivy a sistema de ficheiros + IaC** — lockfiles vulneráveis,
  más configurações do Dockerfile.
- **Grype** — análise independente, segunda opinião sobre a mesma imagem.

### Validação de configuração
- **Hadolint** — lint ao Dockerfile, falha em severidade error.
- **actionlint** — validação do YAML dos workflows do GitHub.
- **yamllint** — higiene genérica de YAML.
- **`jq` + guard regex** — sintaxe JSON + análise de segredos em texto
  para `appsettings*.json`.
- **`dotnet format --verify-no-changes`** — desvio de estilo / espaços
  em branco.

### Deteção de segredos
- **Gitleaks** — histórico git completo, com allowlist no `.gitleaks.toml`
  para falsos positivos documentados.
- **TruffleHog** — binário direto (sem action), modo verified-only; diff do PR em pull_request, histórico completo em push.

### Higiene da pipeline
- **Tokens de privilégio mínimo** em cada workflow (bloco `permissions:`).
- **Versões de actions fixadas**; o Dependabot levanta PRs de subida.
- **`timeout-minutes`** em cada job.
- **`workflow_dispatch:`** em cada workflow para reexecuções manuais.

## 5. Análise dos workflows

Cada workflow é descrito em três partes: **disparo**, **o que cada passo
faz** e **modos de falha**. Entradas e saídas são documentadas inline.

---

### 5.1 `build-test.yml` — Build & Test

**Caminho:** [`.github/workflows/build-test.yml`](../../../.github/workflows/build-test.yml)
**Disparos:** push/PR para `main`/`develop`, dispatch manual.
**Timeout:** 20 min.
**Permissões:** `contents: read`.

#### Job: `build-and-test`

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `Checkout source` | `actions/checkout@v4` — obtém o repositório no HEAD do PR. | Obrigatório em cada workflow. |
| 2 | `Setup .NET` | Instala SDK `8.0.x`. | Toolchain reprodutível — independente dos defaults da runner image. |
| 3 | `Cache NuGet packages` | `actions/cache@v4` indexada por `hashFiles('**/*.csproj')`. | Evita voltar a transferir dependências em cada execução — reduz uma build típica de ~3 min para ~30 s. |
| 4 | `Restore dependencies` | `dotnet restore LawyerApp/LawyerApp.sln`. | Transfere o grafo transitivo de NuGet. |
| 5 | `Build (Release)` | `dotnet build … -c Release --no-restore`. | Constrói na configuração que vamos enviar — `Release` ativa otimizações do compilador e é o que os artefactos de teste/publish partilham. |
| 6 | `Run tests with code coverage` | `dotnet test … --logger trx --collect:"XPlat Code Coverage"`. | Corre cada projeto de testes referenciado pela solução; emite TRX (formato de resultado de testes do Visual Studio) e XML de cobertura Cobertura. |
| 7 | `Upload test results` | `actions/upload-artifact@v4` → `test-results`. | TRX é abrível no Visual Studio ou via `dotnet-trx`. Retido 7 dias. |
| 8 | `Upload coverage report` | `actions/upload-artifact@v4` → `coverage-report`. | XML Cobertura é consumível pelo ReportGenerator, Codecov, SonarCloud, etc. |
| 9 | `Publish (production payload)` | `dotnet publish … -c Release --no-build`. | Produz o payload runtime que o Docker iria copiar para a imagem. O nome do passo é "production payload" porque é bit-a-bit aquilo que será enviado. |
| 10 | `Upload published artifact` | `actions/upload-artifact@v4` → `lawyerapp-publish`. | Permite ao revisor transferir a build exata que o PR produziu. Retido 7 dias. |

**Modos de falha:** qualquer erro de compilação ou falha de teste faz
falhar o job. Para depurar uma falha específica de teste, transfira o
artefacto `test-results` e abra o ficheiro `.trx`.

---

### 5.2 `codeql.yml` — SAST primária

**Caminho:** [`.github/workflows/codeql.yml`](../../../.github/workflows/codeql.yml)
**Disparos:** push/PR/semanal (segundas 03:17 UTC) / manual.
**Timeout:** 30 min.
**Permissões:** `contents: read`, `security-events: write`,
`actions: read`.

#### Job: `analyze` (matriz `language: [csharp]`)

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `Checkout` | Padrão. | — |
| 2 | `Setup .NET` | Instala SDK `8.0.x`. | O CodeQL precisa de **fazer build** do projeto para extrair informação semântica. |
| 3 | `Initialize CodeQL` | `github/codeql-action/init@v3` com `queries: security-and-quality`. | Escolhe o pacote de queries mais agressivo (default `security-extended` mais queries de qualidade) — preferível para um projeto avaliado em segurança. |
| 4 | `Build for CodeQL` | `dotnet restore` + `dotnet build … --configuration Release`. | Obrigatório para linguagens compiladas (C#). Sem uma build bem-sucedida, o CodeQL não consegue extrair símbolos. |
| 5 | `Perform CodeQL Analysis` | `github/codeql-action/analyze@v3`. | Corre o pacote de queries contra a base de dados extraída, carrega SARIF para o Security tab na categoria `/language:csharp`. |

**Modos de falha:** falha na build do passo 4 faz falhar o job.
Ocorrências acima do limiar predefinido do CodeQL aparecem em *Security →
Code scanning* mas **não** fazem falhar o workflow por si só (o GitHub
faz deduplicação e classificação no servidor).

---

### 5.3 `semgrep.yml` — SAST adicional

**Caminho:** [`.github/workflows/semgrep.yml`](../../../.github/workflows/semgrep.yml)
**Disparos:** push/PR/semanal (segundas 04:27 UTC) / manual.
**Timeout:** 20 min.
**Permissões:** `contents: read`, `security-events: write`.

#### Job: `semgrep`
Corre dentro da imagem de contentor oficial `semgrep/semgrep`.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `Checkout` | Padrão. | — |
| 2 | `Run Semgrep` | `semgrep ci --sarif --output=semgrep.sarif --config=p/default --config=p/security-audit --config=p/csharp --config=p/dockerfile --config=p/secrets`. | Cinco conjuntos de regras em camadas: qualidade geral de código, segurança alinhada com OWASP, específica de C#, Dockerfile, e segredos. Saída SARIF para o Security tab. |
| 3 | `Upload SARIF to GitHub` | `github/codeql-action/upload-sarif@v3` na categoria `semgrep`. | Ocorrências aparecem no Security tab a par do CodeQL — deduplicação via etiquetas de categoria. |
| 4 | `Upload Semgrep report` | `actions/upload-artifact@v4` → `semgrep-report` (30 d). | Permite aos revisores transferir o SARIF cru se o Security tab não estiver acessível. |

**Modos de falha:** O Semgrep sai com código não-zero em ocorrências dos
conjuntos `p/` classificadas como `ERROR`. Resultados de menor severidade
são reportados mas não falham. Para alterar, mudar para `--severity=ERROR`
apenas.

---

### 5.4 `sonarcloud.yml` — SAST opcional

**Caminho:** [`.github/workflows/sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml)
**Disparos:** apenas dispatch manual (até `SONAR_TOKEN` estar configurado).
**Timeout:** 30 min.
**Permissões:** `contents: read`, `pull-requests: read`.

Corre em `windows-latest` porque a ferramenta CLI dotnet-sonarscanner tem
melhor suporte em Windows e a solução da equipa compila limpamente lá.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `Checkout` (fetch-depth: 0) | Obtém o histórico completo. | O Sonar usa git blame para detetar "código novo". |
| 2 | `Setup .NET` | Igual à build-test. | A build é obrigatória para análise. |
| 3 | `Setup Java` | Temurin 17. | O SonarScanner é uma ferramenta Java. |
| 4 | `Cache SonarCloud packages` | Cache de `~\sonar\cache`. | Evita voltar a transferir o analisador. |
| 5 | `Cache SonarScanner` | Cache de `.\.sonar\scanner`. | Evita voltar a instalar a ferramenta global dotnet. |
| 6 | `Install SonarScanner` | `dotnet tool update dotnet-sonarscanner`. | Só corre se faltar à cache. |
| 7 | `Build & Analyze` | `sonarscanner begin … && dotnet build && sonarscanner end`. | Begin instala um hook de instrumentação, build recolhe dados, end carrega. Saltado se `SONAR_TOKEN` estiver vazio. |

**Configuração:** ver [§14](#14-sonarcloud-configuração-opcional).

---

### 5.5 `dependency-review.yml` — SCA sobre o diff do PR

**Caminho:** [`.github/workflows/dependency-review.yml`](../../../.github/workflows/dependency-review.yml)
**Disparos:** apenas pull_request.
**Timeout:** 5 min.
**Permissões:** `contents: read`, `pull-requests: write` (para o
comentário inline no PR em caso de falha).

#### Job: `dependency-review`

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `Checkout` | Padrão. | — |
| 2 | `Dependency Review` | `actions/dependency-review-action@v4` com `fail-on-severity: high`, `license-check: true`, `deny-licenses: GPL-2.0, GPL-3.0, AGPL-3.0`, `comment-summary-in-pr: on-failure`. | Compara o diff do lockfile / csproj do PR contra a GitHub Advisory Database. Bloqueia novas dependências com CVE HIGH ou licenças copyleft. Adiciona um comentário ao PR listando pacotes problemáticos apenas em caso de falha (mantém o PR limpo no resto dos casos). |

**Requer:** o **Dependency graph** do repositório tem de estar ativo em
*Settings → Code security*. Caso contrário, a action sai com erro
"Dependency review is not supported on this repository".

---

### 5.6 `security-scan.yml` — SCA + SAST em build + Trivy imagem

**Caminho:** [`.github/workflows/security-scan.yml`](../../../.github/workflows/security-scan.yml)
**Disparos:** push/PR/semanal (segundas 06:00 UTC) / manual.
**Permissões:** ao nível do workflow `contents: read`; o job `container-scan`
também requer `security-events: write` para carregar o SARIF.

#### Job 1 — `sca` (pacotes NuGet vulneráveis)
**Timeout:** 10 min.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `actions/checkout@v4` | Padrão. | — |
| 2 | `Setup .NET` | Instala SDK. | — |
| 3 | `Restore packages` | `dotnet restore`. | Necessário antes do `list package`. |
| 4 | `Check for vulnerable packages` | `dotnet list … package --vulnerable --include-transitive` redirecionado por `tee vulnerable-packages.txt`. Grep para o cabeçalho "has the following vulnerable packages" → exit 1 se presente. | Os dados de vulnerabilidade vêm do feed NuGet via Microsoft. Inclui dependências transitivas (as que os nossos pacotes diretos puxam). |
| 5 | `Upload SCA report` | Carrega `vulnerable-packages.txt`. | Persiste a tabela legível mesmo se não houver ocorrências. |

#### Job 2 — `sast` (analisadores .NET + Security Code Scan)
**Timeout:** 20 min.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | Checkout + setup .NET. | Padrão. | — |
| 2 | `Install Security Code Scan analyzer` | `dotnet tool install --global security-scan --version 5.6.7`. | Ferramenta SAST só de código-fonte, complementa o CodeQL. |
| 3 | `Restore & build with analyzers` | `dotnet build … /p:EnableNETAnalyzers=true /p:AnalysisMode=All /warnaserror:CA2100,CA3001..CA3012`. | Analisadores Roslyn correm durante a build; CAs específicos são promovidos a erros (SQL injection, XSS, path traversal, command injection, etc.). Saída redirecionada para `sast-output.txt`. |
| 4 | `Upload SAST output` | Carrega `sast-output.txt`. | — |

#### Job 3 — `container-scan` (Trivy imagem)
**Timeout:** 25 min.
**Permissões:** `contents: read`, `security-events: write`.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | Checkout. | — | — |
| 2 | `Build Docker image` | `docker build -t lawyerapp:${{ github.sha }} LawyerApp/`. | Build local, sem push para registo. |
| 3 | `Trivy — scan for CRITICAL/HIGH` | `aquasecurity/trivy-action@0.35.0` com `severity: "CRITICAL,HIGH"`, `exit-code: "1"`, `ignore-unfixed: true`, `trivyignores: LawyerApp/.trivyignore`, `limit-severities-for-sarif: true`. | Passo de gating. `ignore-unfixed: true` ignora CVEs sem patch disponível. `limit-severities-for-sarif: true` é a correção-chave — sem ele, a action expande severity para "all" para builds SARIF, pelo que MEDIUM/LOW fariam falhar o gate (ver [§12](#12-resolução-de-problemas)). |
| 4 | `Upload SARIF` | Carrega na categoria `trivy-image`. | — |
| 5 | `Trivy — full JSON report` | Mesma imagem, todas as severidades, sem exit-code. | Registo abrangente para análise offline. |
| 6 | `Upload full Trivy report` | Carrega SARIF + JSON juntos como artefacto `trivy-report` (30 d). | — |

---

### 5.7 `trivy-config.yml` — Análise FS + IaC

**Caminho:** [`.github/workflows/trivy-config.yml`](../../../.github/workflows/trivy-config.yml)
**Disparos:** push/PR/manual.
**Timeout:** 15 min.
**Permissões:** `contents: read`, `security-events: write`.

#### Job: `trivy-fs`

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | Checkout. | — | — |
| 2 | `Trivy — filesystem scan` | `scan-type: fs`, `scan-ref: LawyerApp/`, `severity: CRITICAL,HIGH`, `exit-code: 1`, `ignore-unfixed: true`. | Analisa lockfiles + manifestos dentro de `LawyerApp/`. Para .NET isto é sobretudo o `.csproj` / `project.assets.json`. |
| 3 | `Upload filesystem SARIF` | Categoria `trivy-fs`. | — |
| 4 | `Trivy — IaC / Dockerfile config scan` | `scan-type: config`, `scan-ref: .`, `severity: CRITICAL,HIGH`, `exit-code: 1`. | Faz lint ao Dockerfile (conjunto de regras DS-0001…DS-0030). Apanhado pelo rubric como "validação de configuração". |
| 5 | `Upload IaC SARIF` | Categoria `trivy-config`. | — |

---

### 5.8 `sbom.yml` — Geração de SBOM + Grype

**Caminho:** [`.github/workflows/sbom.yml`](../../../.github/workflows/sbom.yml)
**Disparos:** push/PR/semanal (segundas 05:37 UTC) / manual.
**Permissões:** ao nível do workflow `contents: read`; `grype-second-opinion`
adiciona `security-events: write`.

#### Job 1 — `cyclonedx-solution` (SBOM da solução .NET)
**Timeout:** 10 min.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | Checkout + setup .NET. | — | — |
| 2 | `Install CycloneDX tool` | `dotnet tool install --global CycloneDX`. | Gerador oficial de SBOM da Microsoft para .NET. |
| 3 | `Generate SBOM (JSON)` | `dotnet CycloneDX LawyerApp/LawyerApp.sln --output ./sbom --json`. | CycloneDX é o standard de SBOM apoiado pela OWASP. JSON é o formato mais portável. |
| 4 | `Upload SBOM` | Artefacto `sbom-cyclonedx`, retenção 90 dias. | Artefacto para auditor — prova que conseguimos produzir um inventário a pedido. |

#### Job 2 — `syft-image` (SBOM da imagem de contentor)
**Timeout:** 20 min.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | Checkout + Docker Buildx setup. | — | — |
| 2 | `Build image` | `docker/build-push-action@v6` com cache GHA. | Build local, sem push. |
| 3 | `Generate Syft SBOM (SPDX)` | `anchore/sbom-action@v0` com `format: spdx-json`. | SPDX é o standard de SBOM da Linux Foundation. Inclui cada pacote do SO e dependência da camada de linguagem. |

#### Job 3 — `grype-second-opinion` (análise de vulnerabilidades independente)
**Timeout:** 20 min.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1-3 | Mesma build de imagem que o job 2. | — | — |
| 4 | `Grype scan` | `anchore/scan-action@v4`, `severity-cutoff: high`, `output-format: sarif`. | Base de dados de CVE e motor independentes — apanha coisas que o Trivy pode deixar passar e vice-versa. |
| 5 | `Inspect Grype SARIF` | Script de salvaguarda — coloca `found=true` apenas se o SARIF não estiver vazio. | A action emite um SARIF vazio quando não há ocorrências acima do cutoff, o que faz falhar o passo de upload (ver [§12](#12-resolução-de-problemas)). |
| 6 | `Upload Grype SARIF` | Categoria `grype`. Só corre quando `found=true`. | — |

---

### 5.9 `secrets-scan.yml` — Gitleaks + TruffleHog

**Caminho:** [`.github/workflows/secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml)
**Disparos:** push/PR/semanal (segundas 07:07 UTC) / manual.
**Permissões:** `contents: read`, `security-events: write`.

#### Job 1 — `gitleaks`
**Timeout:** 10 min.

Corremos o **binário do gitleaks diretamente** em vez do
`gitleaks/gitleaks-action@v2` porque o v2 exige uma licença paga para
repositórios pertencentes a organizações (`mei-desofs` é uma
organização). O binário em si é licenciado MIT e gratuito.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `Checkout (full history)` | `fetch-depth: 0`. | Analisa cada commit, não apenas HEAD — um segredo que foi feito commit e depois removido continua a ser uma fuga. |
| 2 | `Install gitleaks` | curl da release + tar para `/usr/local/bin/`. Versão fixada `8.18.4`. | Evita a action licenciada. |
| 3 | `Run gitleaks (full history)` | `gitleaks detect --source . --config .gitleaks.toml --report-format sarif --report-path gitleaks.sarif --redact --exit-code 1 --verbose`. | O `.gitleaks.toml` contém a allowlist documentada (ver [§6.1](#61-gitleakstoml)). `--redact` mascara valores de segredos no relatório. |
| 4 | `Upload SARIF to GitHub Security` | Categoria `gitleaks`. Só se SARIF não vazio. | — |
| 5 | `Upload Gitleaks report artifact` | `gitleaks-report`, retenção 30 dias. | — |
| 6 | `Fail on findings` | Sai com 1 se o exit code do passo 3 foi não-zero. | Separado do passo de SARIF para que o artefacto seja carregado mesmo em falha. |

#### Job 2 — `trufflehog`
**Timeout:** 10 min.

Corremos o **binário trufflehog diretamente** em vez do
`trufflesecurity/trufflehog@main`. A action embrulha uma imagem Docker
em `ghcr.io` cuja transferência expira intermitentemente nos runners
GitHub-hosted (`docker: context deadline exceeded`). O binário em si
não tem dependências externas em runtime e instala em segundos.

| # | Passo | O que faz | Porquê |
|---|---|---|---|
| 1 | `Checkout (full history)` | `fetch-depth: 0`. | Necessário para `--since-commit` em PRs e para scan ao histórico completo em push. |
| 2 | `Install trufflehog` | curl da release `v3.95.3` + tar para `/usr/local/bin/`. | Evita a action e o pull da imagem em `ghcr.io`. |
| 3 | `TruffleHog (PR diff)` | `trufflehog git file://. --since-commit "$BASE_SHA" --branch "$HEAD_SHA" --results=verified --fail --no-update`. Só corre em pull_request. | Compara o diff entre base e head do PR; `--results=verified` significa que o TruffleHog confirmou a credencial contra a API do emissor (sem falsos positivos por definição). |
| 4 | `TruffleHog (full history)` | `trufflehog git file://. --results=verified --fail --no-update`. Corre em push / schedule / dispatch. | Análise ao histórico completo. Mantemos `--results=verified` para evitar ruído de placeholders em documentação (por exemplo, `Password=postgres` em exemplos). O Gitleaks corre em paralelo com o ruleset por defeito e cobre o caso "unknown". |

---

### 5.10 `config-validation.yml` — Validação de configuração

**Caminho:** [`.github/workflows/config-validation.yml`](../../../.github/workflows/config-validation.yml)
**Disparos:** push/PR/manual.
**Permissões:** `contents: read`, `security-events: write` (upload do
SARIF do Hadolint).

Cinco jobs independentes (~5 min cada):

#### `dockerfile-lint` — Hadolint
1. `Hadolint (SARIF output)` — usa `hadolint/hadolint-action@v3.1.0` com
   `no-fail: true` para que o SARIF seja carregado mesmo quando existem
   problemas.
2. `Upload Hadolint SARIF` — categoria `hadolint`.
3. `Hadolint (fail on error severity)` — segunda invocação com
   `failure-threshold: error` para fazer gate à build. Dividir as duas
   invocações é a forma mais limpa de carregar SARIF para warnings
   continuando a fazer gate em erros.

#### `workflow-lint` — actionlint
Transfere o binário do actionlint via o script upstream
`download-actionlint.bash` e executa `actionlint -color`. Apanha:
- Inputs / outputs de actions desconhecidos.
- Padrões de shell-injection em blocos `run:` (`${{ … }}` de origem não
  confiável).
- Expressões `if:` inválidas.
- Declarações `secrets:` em falta.

#### `yaml-lint` — yamllint
Higiene genérica de YAML na árvore `.github/`. Regras afinadas para
evitar warnings ruidosos sobre comprimento de linha e chaves `on:` entre
aspas.

#### `json-validation` — appsettings.json + guard de segredos em texto
1. `Install jq`.
2. `Validate appsettings*.json syntax` — `jq empty` em cada
   `LawyerApp/**/appsettings*.json`.
3. `Forbid plaintext secrets in appsettings*.json` — varredura regex de
   `(Password|Pwd|ApiKey|Secret|Token|ConnectionString)\s*=\s*[^"$ ]{4,}`.
   Coloca em allowlist placeholders de desenvolvimento (`Password=postgres`,
   `Server=localhost`) apenas quando o ficheiro se chama
   `*.Development.json`.

#### `dotnet-format` — consistência de estilo
`dotnet format … --verify-no-changes --severity warn`. `continue-on-error: true`
para que o desvio de estilo não falhe a build hoje, mas o warning fica
visível.

## 6. Ficheiros de configuração

### 6.1 `.gitleaks.toml`

**Caminho:** [`/.gitleaks.toml`](../../../.gitleaks.toml)
**Propósito:** allowlist de falsos positivos documentados.

```toml
[extend]
useDefault = true              # constrói sobre o ruleset por defeito

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

**Porquê o regex:** os valores `PublicKeyToken=<hex>` de assemblies .NET
são identificadores *públicos* de assemblies assinados (por exemplo,
`b03f5f7f11d50a3a` para `System.Web`). A regra `generic-api-key` do
Gitleaks classifica-os incorretamente como tokens aleatórios porque têm
entropia elevada.

**Porquê os paths:** `.vs/`, `bin/`, `obj/` são pastas de IDE / saída de
build. Mesmo depois de termos deixado de seguir esses ficheiros no commit
`7e33e6f`, a análise de histórico completo continua a expor entradas de
commits anteriores.

### 6.2 `LawyerApp/.trivyignore`

**Caminho:** [`LawyerApp/.trivyignore`](../../../LawyerApp/.trivyignore)
**Propósito:** IDs de CVE suprimidos com data de revisão e justificação
de uma linha.

```
CVE-2026-0861    # Revisto 2026-05-17: libc-bin / libc6 — pendente rebuild da imagem Microsoft
CVE-2026-4878    # Revisto 2026-05-17: libcap2 — pendente rebuild da imagem Microsoft
CVE-2026-29111   # Revisto 2026-05-17: libsystemd0 / libudev1 — pendente rebuild da imagem Microsoft
```

**Processo:** cada entrada **deve** incluir a data revista, o pacote, e
uma justificação de uma linha. Reauditar trimestralmente — se a imagem
upstream for reconstruída com a versão patched do pacote, **remover a
entrada** para que a supressão não sobreviva à sua razão de ser.

Ligado a ambos os passos do Trivy em `security-scan.yml` via o input
`trivyignores: LawyerApp/.trivyignore` da action.

### 6.3 `.github/dependabot.yml`

**Caminho:** [`.github/dependabot.yml`](../../../.github/dependabot.yml)
**Propósito:** PRs automáticos semanais de atualização de dependências.

Quatro ecossistemas configurados:

| Ecossistema | Diretório | Agendamento | Grupo |
|---|---|---|---|
| `nuget` | `/LawyerApp` | Segunda 03:00 Europe/Lisbon | `microsoft`, `ef-core`, `security` |
| `nuget` | `/LawyerApp.Tests` | Segunda 03:15 Europe/Lisbon | (nenhum) |
| `github-actions` | `/` | Segunda 03:30 Europe/Lisbon | (nenhum) |
| `docker` | `/LawyerApp` | Segunda 04:00 Europe/Lisbon | (nenhum) |

Prefixos de commit message (`chore(deps)`, `chore(test-deps)`,
`chore(ci)`, `chore(docker)`) facilitam a filtragem de PRs do Dependabot.

### 6.4 `.gitignore`

**Caminho:** [`/.gitignore`](../../../.gitignore)
**Propósito:** deixar de seguir saídas de build, estado do IDE e
relatórios de análise.

Adições notáveis face ao template original:
- `**/bin/`, `**/obj/` — saída de build .NET.
- `.vs/`, `*.user` — estado por-utilizador do Visual Studio.
- `coverage/`, `TestResults/`, `**/coverage.cobertura.xml`.
- `*.sarif`, `sbom/`, `*.spdx.json`, `*.cyclonedx.json`,
  `vulnerable-packages.txt`, `trivy-*.sarif`, `semgrep.sarif`,
  `hadolint.sarif`, `sast-output.txt` — todos os nomes de ficheiro de
  saída da pipeline.
- `appsettings.Local.json`, `secrets.json`, `*.pfx`, `*.key`, `*.pem`,
  `.env`, `.env.*` (`!.env.example` mantido).

## 7. Ferramentas utilizadas e justificação

Porque cada ferramenta foi escolhida, as alternativas consideradas e
onde estão os seus limites.

| Ferramenta | Categoria | Escolhida porque | Alternativas consideradas | Limite |
|---|---|---|---|---|
| **`dotnet` CLI** | Build/Test | First-party, única opção prática para .NET. | — | — |
| **CodeQL** | SAST | First-party, gratuita em repositórios públicos, análise semântica profunda. Integração SARIF é a melhor da classe. | SonarCloud, PVS-Studio, NDepend. | Mais lento do que o Semgrep; precisa de build (verdade para linguagens compiladas de qualquer forma). |
| **Semgrep** | SAST | Open-source, rápido (~1 min), conjuntos de regras amplos, não precisa de build. Apanha coisas que o CodeQL deixa passar (Dockerfile / secrets / padrões cross-language). | Bandit (só Python), gosec (só Go). | Baseado em padrões — menos preciso do que CodeQL para fluxos complexos. |
| **Security Code Scan** | SAST | Específica de .NET, corre em build como analisador Roslyn, apanha problemas classificados como CA em tempo de compilação. | Só `Microsoft.CodeAnalysis.NetAnalyzers`. | Última release 5.6.7 (um pouco velha); usamos como adicional, não primária. |
| **SonarCloud** | SAST | Melhor UX para quality gate (comentários em PR, hotspots, overlay de cobertura). | Nenhuma comparável. | Exige conta SaaS; configuração manual, daí opcional. |
| **GitHub Dependency Review** | SCA | First-party, corre só no diff do PR, feedback mais rápido para o contribuidor. | Snyk PR check. | Só reporta vulns novas introduzidas pelo PR — não apanha as pré-existentes. |
| **`dotnet list package --vulnerable`** | SCA | First-party, verificação mais simples possível, corre contra o feed NuGet. | OWASP Dependency-Check. | Exige `dotnet restore` primeiro; relatório é texto. |
| **Dependabot** | SCA | First-party, criação automática de PRs, regras de agrupamento, nativo do GitHub. | Renovate. | Agrupamento granular é menos expressivo que Renovate. |
| **CycloneDX** | SBOM | Standard apoiado pela OWASP, ferramentas .NET são first-party (Microsoft). | SPDX (usado noutro lado — ver Syft). | — |
| **Syft (Anchore)** | SBOM | De facto o standard para imagens de contentor, saída SPDX. | CycloneDX-cli. | — |
| **Trivy** | Contentor + IaC + filesystem | Ferramenta única que cobre três áreas do rubric, muito amplamente adotada. | Snyk, Clair, Anchore Engine. | Algumas peculiaridades à volta de filtragem de severidade — ver [§12](#12-resolução-de-problemas). |
| **Grype (Anchore)** | Contentor (segunda opinião) | Motor + BD independentes, leve, apanha o que o Trivy deixa passar. | Nenhuma adicionada — já temos Trivy. | — |
| **Hadolint** | Configuração (Dockerfile) | De facto o linter de Dockerfile, integra com SARIF. | Dockerlinter (menos maduro). | — |
| **actionlint** | Configuração (workflows) | Apanha bugs específicos de GitHub Actions (shell injection, inputs em falta). | yamllint sozinho deixa passar estes. | — |
| **yamllint** | Configuração (YAML) | Higiene genérica de YAML. | — | — |
| **`jq`** | Configuração (JSON) | Ferramenta CLI standard para JSON. | Python `json.tool`. | — |
| **Gitleaks** | Segredos | Open-source, madura, configurável. | TruffleHog (também na pipeline). | gitleaks-action@v2 precisa de licença paga para orgs — usamos o binário diretamente. |
| **TruffleHog** | Segredos | O modo "verified" valida credenciais contra a API do emissor. Taxa de falsos positivos muito mais baixa do que análise só por entropia. | Gitleaks sozinho. | Mais lenta do que o Gitleaks. A action upstream depende de uma imagem ghcr.io que falha por timeout — corremos o binário diretamente. |

## 8. Executar cada verificação localmente

Cada verificação em CI pode ser reproduzida localmente. Correr a partir
da raiz do repositório.

### Build & Test
```bash
dotnet restore LawyerApp/LawyerApp.sln
dotnet build LawyerApp/LawyerApp.sln -c Release
dotnet test  LawyerApp/LawyerApp.Tests/LawyerApp.Tests.csproj \
  -c Release --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet publish LawyerApp/LawyerApp.csproj -c Release --output ./publish /p:UseAppHost=false
```

### CodeQL
Usar o bundle CLI do CodeQL de <https://github.com/github/codeql-action/releases>
ou simplesmente confiar na execução em CI. Correr CodeQL localmente é
pesado (~15 min de setup); na prática ninguém o corre localmente para
alterações incrementais.

### Semgrep
```bash
# Instalar uma vez
brew install semgrep                 # macOS
pipx install semgrep                 # qualquer plataforma

semgrep ci --config=p/default --config=p/security-audit \
           --config=p/csharp --config=p/dockerfile --config=p/secrets .
```

### Analisadores .NET
```bash
dotnet build LawyerApp/LawyerApp.sln -c Release \
  /p:EnableNETAnalyzers=true /p:AnalysisMode=All \
  /warnaserror:CA2100,CA3001,CA3002,CA3003,CA3006,CA3007,CA3010,CA3011,CA3012
```

### SCA — pacotes NuGet vulneráveis / deprecated / outdated
```bash
dotnet restore LawyerApp/LawyerApp.sln
dotnet list LawyerApp/LawyerApp.sln package --vulnerable --include-transitive
dotnet list LawyerApp/LawyerApp.sln package --deprecated
dotnet list LawyerApp/LawyerApp.sln package --outdated
```

### SBOM CycloneDX
```bash
dotnet tool install --global CycloneDX
dotnet CycloneDX LawyerApp/LawyerApp.sln --output ./sbom --json
```

### Build de contentor + Trivy + Grype + Syft (precisa Docker)
```bash
# Construir a imagem localmente
docker build -t lawyerapp:local LawyerApp/

# Análise Trivy à imagem (igual à do CI)
docker run --rm -v "$(pwd):/repo" -w /repo aquasec/trivy:0.69.3 \
  image --severity CRITICAL,HIGH --ignore-unfixed \
  --ignorefile LawyerApp/.trivyignore lawyerapp:local

# Análise Trivy ao sistema de ficheiros
docker run --rm -v "$(pwd):/repo" -w /repo aquasec/trivy:0.69.3 \
  fs --severity CRITICAL,HIGH --ignore-unfixed LawyerApp/

# Análise Trivy à configuração do Dockerfile
docker run --rm -v "$(pwd):/repo" -w /repo aquasec/trivy:0.69.3 \
  config LawyerApp/Dockerfile

# SBOM Syft da imagem
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  anchore/syft lawyerapp:local -o spdx-json

# Análise Grype à imagem
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

### Gitleaks (histórico completo)
```bash
# Instalar uma vez
brew install gitleaks                # macOS
# ou descarregar o binário de https://github.com/gitleaks/gitleaks/releases

gitleaks detect --source . --config .gitleaks.toml --redact --verbose
```

### TruffleHog (diff do PR)
```bash
# Instalar uma vez
brew install trufflehog                # macOS
# ou descarregar o binário de https://github.com/trufflesecurity/trufflehog/releases

# Análise full-history (espelha o passo de CI)
trufflehog git "file://$PWD" --results=verified --fail --no-update

# Diff entre dois commits (espelha o passo de PR em CI)
trufflehog git "file://$PWD" \
  --since-commit "$(git merge-base origin/main HEAD)" \
  --branch HEAD --results=verified --fail --no-update
```

### Guard de JSON e segredos em texto em appsettings
```bash
for f in LawyerApp/**/appsettings*.json; do jq empty "$f"; done
grep -E -i '(Password|Pwd|ApiKey|Secret|Token|ConnectionString)\s*=\s*[^"$ ]{4,}' LawyerApp/**/appsettings*.json
```

### Verificação de estilo `dotnet format`
```bash
dotnet format LawyerApp/LawyerApp.sln --verify-no-changes --severity warn
```

## 9. Interpretação das ocorrências

As ocorrências aparecem em três locais, cada um com uma utilização
diferente:

### 9.1 Separador Checks do PR
- Uma linha por job de workflow.
- Click → visualizador de logs com saída de cada passo colapsável.
- Melhor para: perceber *porque* é que uma verificação falhou.

### 9.2 Security → Code scanning
- Agrega SARIF do CodeQL, Semgrep, Trivy (imagem / fs / config), Grype,
  Hadolint, Gitleaks.
- Cada ocorrência tem categoria (definida por `category:` no passo de
  upload), severidade, localização (ficheiro + linha), e fingerprint
  para deduplicação.
- Melhor para: triagem de backlog, marcar ocorrências como falso
  positivo ou won't-fix (essas decisões persistem entre execuções).

### 9.3 Actions → execução do workflow → Artifacts
- Um artefacto por analisador com uma cópia legível do relatório
  (`vulnerable-packages.txt`, `trivy-report`, `sbom-cyclonedx`, etc.).
- Melhor para: alimentar o resultado a uma ferramenta downstream,
  anexar a um ticket, ou revisão offline.

### Definições de severidade

| Severidade | Limiar para falhar a build | Exemplos |
|---|---|---|
| **CRITICAL** | Bloqueia sempre o merge. | RCE numa dependência, chave privada hardcoded. |
| **HIGH** | Bloqueia o merge a não ser que suprimida com justificação documentada. | Bypass de autenticação, SQL injection. |
| **MEDIUM** | Reportada, sem gate. | Vetor DoS, criptografia fraca. |
| **LOW** | Reportada, sem gate. | Estilo, aleatoriedade fraca em contexto não-segurança. |

## 10. Resposta a uma verificação que falha

Árvore de decisão para os cenários de falha mais comuns:

```
Verificação do PR vermelha
│
├── A falha é um erro de build?
│   └── Corrigir o código. O CI fica verde automaticamente.
│
├── É uma falha de teste?
│   ├── Reproduzir localmente:
│   │     dotnet test LawyerApp/LawyerApp.Tests/LawyerApp.Tests.csproj --filter "Name~Failing"
│   └── Corrigir e fazer push.
│
├── É um CVE HIGH/CRITICAL numa dependência?
│   ├── Conseguimos fazer upgrade?  Sim → atualizar o .csproj, push, feito.
│   ├── Sem correção disponível (ignore-unfixed já ativo)?  Investigar explorabilidade.
│   ├── Mitigado pelo contexto de deployment?
│   │     Adicionar CVE a LawyerApp/.trivyignore com data + justificação.
│   └── Caso contrário → bloquear o PR.
│
├── É um CVE HIGH/CRITICAL na imagem base?
│   ├── Nova imagem Microsoft disponível?  Subir a tag FROM (o Dependabot faz isto).
│   └── Caso contrário → adicionar CVE a .trivyignore com nota "pendente rebuild upstream".
│
├── É uma ocorrência SAST (CodeQL / Semgrep)?
│   ├── Verdadeiro positivo?  Corrigir o código.
│   ├── Falso positivo?  Adicionar comentário de supressão no código OU marcar como
│   │     descartado em Security → Code scanning (com motivo).
│
├── É uma ocorrência do Gitleaks?
│   ├── Segredo real?  Rodar imediatamente, revogar a credencial junto do emissor,
│   │     e usar BFG / git-filter-repo para limpar o histórico.
│   ├── Falso positivo?  Adicionar ao allowlist de .gitleaks.toml com comentário.
│
├── É uma falha do Dependency Review?
│   ├── CVE HIGH real?  Escolher uma versão diferente.
│   ├── Licença copyleft?  Escolher uma alternativa permissiva.
│
├── É uma falha do Hadolint / Trivy config?
│   └── Corrigir o Dockerfile ou workflow. Estas regras são fáceis de satisfazer.
│
└── O próprio workflow está partido (erro de config, mismatch de versão de action)?
    └── Ver resolução de problemas (§12).
```

## 11. Adicionar / modificar / suprimir verificações

### Adicionar um novo projeto de testes

1. `dotnet new xunit -o LawyerApp.Whatever.Tests`
2. Adicionar o projeto ao `LawyerApp.sln` (`dotnet sln add`).
3. O CI vai apanhá-lo automaticamente — o `dotnet test` corre cada
   projeto de testes na solução.

### Adicionar um novo analisador

1. Criar um novo ficheiro de workflow em `.github/workflows/<scanner>.yml`.
2. Reutilizar o esqueleto existente:
   - `permissions: contents: read` (mais `security-events: write` se for
     carregar SARIF).
   - `timeout-minutes:` em cada job.
   - Fixar a versão da action.
   - Adicionar `workflow_dispatch:` para reexecuções manuais.
3. Adicionar uma entrada à matriz de disparos ([§3](#3-matriz-de-disparos)).
4. Adicionar uma secção de análise ([§5](#5-análise-dos-workflows)).
5. Adicionar o badge correspondente ao [README](../../../README.md).
6. Após a primeira execução bem-sucedida, a nova verificação aparece no
   picker de branch protection. Marcar como obrigatória.

### Suprimir um CVE

`LawyerApp/.trivyignore`:
```
CVE-YYYY-NNNNN    # Revisto <data>: <pacote> — <motivo de uma linha>
```

Reauditar trimestralmente. Remover entradas quando a correção subjacente
estiver disponível.

### Suprimir um falso positivo do Gitleaks

`.gitleaks.toml`:
```toml
[allowlist]
regexes = [
    '''<o-teu-padrão>''',     # <comentário com motivo>
]
paths = [
    '''(^|/)caminho/para/ficheiro/''',
]
```

### Suprimir uma ocorrência do CodeQL / Semgrep

Duas opções, escolher uma:
- **Descartar no Security tab** com motivo escrito. Persiste entre
  execuções via fingerprint, mas só guardado no GitHub (não no histórico
  git).
- **Comentário inline** na localização da fonte:
  - CodeQL: `// codeql[<rule-id>]` na linha acima.
  - Semgrep: `// nosemgrep: <rule-id>  # motivo` na mesma linha.

Inline é preferível quando a ocorrência é permanente e conhecida pela
equipa; o descarte no Security tab serve para decisões pontuais de
triagem.

### Modificar um limiar de severidade

- Trivy: alterar `severity: "CRITICAL,HIGH"` para adicionar ou remover
  níveis.
- Dependency Review: alterar `fail-on-severity: high` para `critical`
  (mais permissivo) ou `moderate` (mais estrito).
- Grype: alterar `severity-cutoff: high`.

Documentar cada alteração na descrição do PR para que os revisores
saibam.

## 12. Resolução de problemas

Erros comuns que enfrentámos efetivamente, com a correção:

### `Unable to resolve action aquasecurity/trivy-action@X.Y.Z`
A tag não existe. Tags reais são `v0.X.Y` ou `0.X.Y` conforme o release.
Ver <https://github.com/aquasecurity/trivy-action/releases>. Fixámos
`0.35.0`.

### `[mei-desofs] is an organization. License key is required.` (Gitleaks)
O `gitleaks/gitleaks-action@v2` exige licença paga para repositórios de
organizações desde o relicenciamento de agosto de 2023. Correção: usar o
binário gitleaks diretamente (o `secrets-scan.yml` faz isto — instala via
curl, corre com `--config .gitleaks.toml`).

### `Dependency review is not supported on this repository`
Ativar **Dependency graph** em *Settings → Code security → Dependency
graph*.

### Análise Trivy à imagem sai com 1 apesar de não haver ocorrências CRITICAL/HIGH
O `aquasecurity/trivy-action` substitui o filtro de severidade por
todas-as-severidades quando `format: sarif`, pelo que a mesma execução
que produz o SARIF abrangente também falha em ocorrências MEDIUM/LOW.
Correção: pôr `limit-severities-for-sarif: true`. O relatório completo
MEDIUM/LOW é preservado no passo separado `Trivy — full report (JSON)`.

### `Invalid SARIF. JSON syntax error: Unexpected end of JSON input` (Grype)
O `anchore/scan-action@v4` emite um SARIF vazio quando não há
ocorrências acima do cutoff de severidade. Correção: proteger o passo de
upload com `if: always() && steps.sarif_check.outputs.found == 'true'`,
onde `sarif_check` é um bloco `run:` que testa `[ -s "$path" ]`.

### O Hadolint passa localmente mas a análise Trivy IaC falha no Dockerfile
O `aquasecurity/trivy-action@0.35.0` para `scan-type: config` não
respeita o filtro `severity` — falha em qualquer ocorrência, mesmo LOW.
Correção: satisfazer a regra (por exemplo, `HEALTHCHECK NONE` para
DS-0026). Se a regra não puder ser corrigida, adicionar o ID da regra a
`.trivyignore`.

### TruffleHog falha em push para main com "BASE and HEAD commits are the same"
Quando se faz push para a default branch, o input `base` e o `head` da
action são o mesmo commit, pelo que a action recusa correr. Correção
inicial: dividir o job em dois passos com `if:`. Correção definitiva
(aplicada): substituir a action pelo binário diretamente (ver erro
seguinte).

### TruffleHog: `docker: Head https://ghcr.io/v2/.../manifests/latest: context deadline exceeded`
A action `trufflesecurity/trufflehog@main` faz pull de uma imagem
Docker em `ghcr.io` que intermitentemente expira nos runners
GitHub-hosted (timeout de cliente). Correção: deixar de usar a action,
instalar e correr o binário diretamente:

```yaml
- name: Install trufflehog
  env:
    TRUFFLEHOG_VERSION: 3.95.3
  run: |
    curl -sSfL -o trufflehog.tar.gz \
      "https://github.com/trufflesecurity/trufflehog/releases/download/v${TRUFFLEHOG_VERSION}/trufflehog_${TRUFFLEHOG_VERSION}_linux_amd64.tar.gz"
    tar -xzf trufflehog.tar.gz trufflehog
    sudo mv trufflehog /usr/local/bin/

- name: TruffleHog (full history)
  run: trufflehog git "file://${PWD}" --results=verified --fail --no-update
```

### TruffleHog falha em push para main com `unverified_secrets: N`
Mudámos da action para o binário; o binário, sem `--since-commit`,
analisa o histórico completo. Findings antigos em ficheiros de
documentação (por exemplo, `Password=postgres` num exemplo) são
classificados como "unknown" pelo detector SQLServer. Correção:
filtrar para `--results=verified` apenas — credenciais que o TruffleHog
consegue confirmar contra a API do emissor (sem falsos positivos por
definição). O Gitleaks corre em paralelo com o ruleset por defeito e
cobre o caso "unknown".

### `git push` 403 (conta GitHub errada)
O `gh auth setup-git` adicionou um credential helper específico para
`https://github.com` baseado em `gh` que faz bypass ao picker do Git
Credential Manager. Correção:
```bash
git config --global --unset-all credential.https://github.com.helper
```

### `dotnet format --verify-no-changes` falha
Correr `dotnet format LawyerApp/LawyerApp.sln` localmente para aplicar
as correções, commit, push.

## 13. Definições de GitHub necessárias (fora do código)

Estas não podem ser automatizadas a partir do repositório — são
definições por repositório.

### Settings → Code security
1. **Dependabot alerts** — ligado.
2. **Dependabot security updates** — ligado (abre PRs para CVEs
   conhecidos).
3. **Dependency graph** — ligado (obrigatório para `dependency-review.yml`).
4. **Code scanning** — ligado (para que o SARIF do CodeQL / Semgrep /
   Trivy / Grype / Hadolint / Gitleaks apareça em *Code scanning alerts*).
5. **Secret scanning** + **Push protection** — ligados (complementam o
   Gitleaks e o TruffleHog com o próprio analisador do GitHub; o push
   protection bloqueia segredos novos *antes* de ficarem commitados).

### Settings → Branches → branch protection para `main`
1. Exigir um pull request antes de fazer merge.
2. Exigir que os status checks passem (após a primeira execução, estes
   nomes aparecem no picker):
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
3. Exigir que os ramos estejam atualizados antes do merge.
4. Bloquear push direto para `main`.

### GitHub Advanced Security (apenas se o repositório for privado)
O CodeQL, secret scanning e dependency review são gratuitos em
repositórios **públicos**. Se o repositório for privado, um proprietário
da org tem de ativar o GitHub Advanced Security para a equipa — caso
contrário, esses três workflows avisam mas não falham.

## 14. SonarCloud (configuração opcional)

O SonarCloud dá quality gates por PR com overlays de code-smell,
security-hotspot, duplicação e cobertura — bom de ter, não obrigatório
pelo rubric.

1. Iniciar sessão em <https://sonarcloud.io> com a conta GitHub da
   equipa.
2. *+ → Analyze new project*, escolher este repositório, escolher o
   **plano gratuito para projetos públicos**.
3. Anotar a **organisation key** e a **project key**.
4. Em SonarCloud *My Account → Security* gerar um token.
5. Em GitHub *Settings → Secrets and variables → Actions* criar o
   secret `SONAR_TOKEN`.
6. Editar `.github/workflows/sonarcloud.yml`:
   - Substituir os placeholders `<org>` e `<project-key>`.
   - Descomentar os disparos `pull_request:` e `push:`.

## 15. Mapeamento para o rubric do Sprint 2

| Critério do rubric | Evidência neste repositório |
|---|---|
| **Inventário de componentes** | SBOMs em [`sbom.yml`](../../../.github/workflows/sbom.yml) (CycloneDX para a solução, SPDX para a imagem). |
| **Execução dos planos de teste** | [`build-test.yml`](../../../.github/workflows/build-test.yml) corre testes xUnit unitários + integração em cada PR; plano documentado em [Test_Plan.md](Test_Plan.md). |
| **Análise estática (SAST)** | [`codeql.yml`](../../../.github/workflows/codeql.yml), [`semgrep.yml`](../../../.github/workflows/semgrep.yml), job `sast` em [`security-scan.yml`](../../../.github/workflows/security-scan.yml). Opcional: [`sonarcloud.yml`](../../../.github/workflows/sonarcloud.yml). |
| **Análise de composição de software (SCA)** | [`dependency-review.yml`](../../../.github/workflows/dependency-review.yml), job `sca` em [`security-scan.yml`](../../../.github/workflows/security-scan.yml), [`dependabot.yml`](../../../.github/dependabot.yml). |
| **Análise de artefactos** | Job `container-scan` em [`security-scan.yml`](../../../.github/workflows/security-scan.yml), [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml), Grype em [`sbom.yml`](../../../.github/workflows/sbom.yml). |
| **Validação de configuração** | [`config-validation.yml`](../../../.github/workflows/config-validation.yml) (Hadolint + actionlint + yamllint + guard JSON / segredos) e [`trivy-config.yml`](../../../.github/workflows/trivy-config.yml) (modo IaC). |
| **Deteção de segredos** | [`secrets-scan.yml`](../../../.github/workflows/secrets-scan.yml) (Gitleaks + TruffleHog). |
| **Análise dinâmica (DAST)** | Fora do âmbito da Automação de Pipeline no Sprint 2 — pertence ao âmbito de Security Testing. |
| **Automação de pipeline** | Todas as práticas acima correm em cada PR sem qualquer passo manual. |

## 16. Manutenção e ciclo de vida

- **Cadência de atualização**: o Dependabot levanta PRs de
  action / NuGet / base Docker todas as segundas de manhã. Fazer merge
  pelo mesmo fluxo com gate de PR.
- **Adicionar um novo projeto de testes**: largar um `*.Tests.csproj` ao
  lado da solução e adicioná-lo ao `LawyerApp.sln` — o comando
  `dotnet test` existente corre a solução inteira, pelo que será
  apanhado automaticamente.
- **Afinar limiares de falha**: cada analisador expõe um botão de
  severidade — `fail-on-severity` (Dependency Review), `severity-cutoff`
  (Grype), a lista `severity:` do Trivy, etc. Começar estrito
  (CRITICAL/HIGH falham), relaxar apenas com exceção documentada.
- **Auditoria de supressões**: rever `.trivyignore` e `.gitleaks.toml`
  trimestralmente. Remover entradas que já não são necessárias (correção
  aterrou upstream, segredo foi rodado).
- **Atualizações de versões de actions**: o Dependabot abre um PR para
  cada major. Ler o CHANGELOG da action antes do merge; algumas subidas
  alteram defaults.
- **Tempo de execução da pipeline**: o objetivo é manter o conjunto de
  gates do PR abaixo de ~10 minutos wall-clock. Se algum workflow passar
  os 15 min, considerar dividi-lo.

## 17. Glossário

- **SAST** — *Static Application Security Testing*. Analisa código-fonte
  (ou artefactos construídos) sem o executar. Exemplos aqui: CodeQL,
  Semgrep, analisadores .NET, Security Code Scan, SonarCloud.
- **DAST** — *Dynamic Application Security Testing*. Testa uma
  instância em execução da aplicação (por exemplo, OWASP ZAP, Burp).
  Fora do âmbito desta pipeline do Sprint 2; vive no âmbito de Security
  Testing.
- **IAST** — *Interactive Application Security Testing*. Híbrido; um
  agente dentro da aplicação em execução observa fluxos de dados em
  runtime. Também fora do âmbito do Sprint 2.
- **SCA** — *Software Composition Analysis*. Identifica versões
  conhecidas como vulneráveis de dependências de terceiros. Ferramentas
  aqui: GitHub Dependency Review, `dotnet list --vulnerable`, Dependabot.
- **SBOM** — *Software Bill of Materials*. Inventário legível por máquina
  de todos os componentes incluídos numa build. Standards: CycloneDX
  (OWASP), SPDX (Linux Foundation).
- **SARIF** — *Static Analysis Results Interchange Format*. Schema JSON
  standard da OASIS para ocorrências de ferramentas de segurança. Todos
  os analisadores nesta pipeline emitem SARIF para que a UI Code Scanning
  do GitHub os possa renderizar.
- **CVE** — *Common Vulnerabilities and Exposures*. O catálogo mantido
  pela MITRE de vulnerabilidades publicamente divulgadas
  (`CVE-AAAA-NNNNN`).
- **CWE** — *Common Weakness Enumeration*. Classificação de mais alto
  nível dos tipos de vulnerabilidade (por exemplo, CWE-79 = XSS). A
  maioria das ocorrências SAST cita um CWE.
- **Trust boundary** — Onde os dados atravessam de um nível de
  privilégio/confiança para outro. As ameaças são frequentemente
  analisadas nos trust boundaries (STRIDE).
- **PR-time gate** — Uma verificação que corre em cada pull request e
  tem de passar antes do merge.
- **SSDLC** — *Secure Software Development Lifecycle*. Modelo de
  processo que este projeto segue.
- **OWASP ASVS** — *Application Security Verification Standard*.
  Catálogo da OWASP de requisitos de segurança testáveis. A pipeline
  mapeia para V14 (Configuração), V10 (Código Malicioso), V14.2
  (Dependência).
- **SSDF** — NIST SP 800-218 *Secure Software Development Framework*.
  Catálogo de práticas de mais alto nível. Esta pipeline automatiza
  PW.4 (reutilizar orientação de segurança), PW.5 (criar software bem
  protegido), PW.7 (rever software à procura de vulnerabilidades),
  PW.8 (testar software), RV.1 (identificar vulnerabilidades de forma
  contínua).
- **STRIDE** — Taxonomia de modelação de ameaças da Microsoft:
  Spoofing, Tampering, Repudiation, Information disclosure, Denial of
  service, Elevation of privilege. Usada na modelação de ameaças da
  Fase 1.

## 18. Referências

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
- Registo Semgrep — <https://semgrep.dev/explore>
- Documentação Trivy — <https://trivy.dev/docs/>
- Anchore (Syft + Grype) — <https://github.com/anchore/syft>, <https://github.com/anchore/grype>
- Gitleaks — <https://github.com/gitleaks/gitleaks>
- TruffleHog — <https://github.com/trufflesecurity/trufflehog>
- Hadolint — <https://github.com/hadolint/hadolint>
- actionlint — <https://github.com/rhysd/actionlint>
- CycloneDX — <https://cyclonedx.org/>
- SPDX — <https://spdx.dev/>
