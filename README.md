# DESOFS 2026 — Grupo Wed PBS 5-2

**Projeto:** Lawyer App — Back-end seguro para uma consultora jurídica, construído segundo os princípios do SSDLC.

[![Build & Test](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/build-test.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/build-test.yml)
[![CodeQL](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/codeql.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/codeql.yml)
[![Semgrep](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/semgrep.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/semgrep.yml)
[![Security Scan](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/security-scan.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/security-scan.yml)
[![Trivy FS/IaC](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/trivy-config.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/trivy-config.yml)
[![SBOM](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/sbom.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/sbom.yml)
[![Secrets Scan](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/secrets-scan.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/secrets-scan.yml)
[![Config Validation](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/config-validation.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/config-validation.yml)
[![Deploy](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/deploy.yml/badge.svg)](https://github.com/mei-desofs/desofs2026-wed_pbs_5-2/actions/workflows/deploy.yml)

---

## Estrutura do Repositório

```
.
├── Deliverables/
│   └── Phase 1 Threat Modeling/
│       └── Phase1_Deliverable.md   # Critérios de avaliação → mapeamento de evidências
└── Documentation/
    ├── Analysis/
    │   ├── Analysis.md
    │   ├── Architecture_Diagram.svg
    │   ├── domain_model.plantuml
    │   └── domain_model.png
    ├── Dataflow/
    │   ├── Dataflow.md
    │   ├── lvl0Sistema.jpeg
    │   ├── lvl1RF01.png
    │   ├── lvl1RF02.png
    │   ├── lvl1RF03.png
    │   └── lvl1RF04.png
    ├── Mitigations/
    │   └── Mitigations.md
    ├── Requirements/
    │   └── FURPS+.md
    ├── Risk_Assessment/
    │   └── Risk_Assessment.md
    ├── Security Testing/
    │   ├── RF01.xlsx
    │   ├── RF02.xlsx
    │   ├── RF03.xlsx
    │   └── RF04.xlsx
    └── ThreatId/
        └── ThreatId.md
```

---

## Fase 1 — Entregáveis de Modelação de Ameaças

O mapeamento completo da avaliação (critérios → ficheiro) está
em [Phase1_Deliverable.md](Deliverables/Phase%201%20Threat%20Modeling/Phase1_Deliverable.md).

| Critério                  | Peso  | Documento                                                                                                                                                                                                                              |
|:--------------------------|:-----:|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Organização e Linguagem   |  5%   | Este README                                                                                                                                                                                                                            |
| Análise                   | 10%   | [Analysis.md](Documentation/Analysis/Analysis.md)                                                                                                                                                                                      |
| Fluxo de Dados            | 15%   | [Dataflow.md](Documentation/Dataflow/Dataflow.md)                                                                                                                                                                                      |
| Identificação de Ameaças  | 20%   | [ThreatId.md](Documentation/ThreatId/ThreatId.md)                                                                                                                                                                                      |
| Avaliação de Risco        | 10%   | [Risk_Assessment.md](Documentation/Risk_Assessment/Risk_Assessment.md)                                                                                                                                                                 |
| Mitigações                | 10%   | [Mitigations.md](Documentation/Mitigations/Mitigations.md)                                                                                                                                                                             |
| Requisitos                | 20%   | [FURPS+.md](Documentation/Requirements/FURPS+.md)                                                                                                                                                                                      |
| Testes de Segurança       | 10%   | [RF01.xlsx](Documentation/Security%20Testing/RF01.xlsx) · [RF02.xlsx](Documentation/Security%20Testing/RF02.xlsx) · [RF03.xlsx](Documentation/Security%20Testing/RF03.xlsx) · [RF04.xlsx](Documentation/Security%20Testing/RF04.xlsx)   |

## Fase 2 — Desenvolvimento e Testes

### Pipeline CI/CD

O desenho da pipeline, os analisadores adotados, o mapeamento para o
rubric e a checklist de configuração no GitHub estão documentados em
[Documentation/Phase 2/Build_Test/Pipeline.md](Documentation/Phase%202/Build_Test/Pipeline.md).
Os workflows estão em [`.github/workflows`](.github/workflows) e correm
em cada pull request, push para `main` / `develop` e em agendamentos
semanais.
Ver também: [Test_Plan.md](Documentation/Phase%202/Build_Test/Test_Plan.md)
e [Artifact_Scanning.md](Documentation/Phase%202/Build_Test/Artifact_Scanning.md).

### Executar a aplicação

Este guia explica como configurar, executar e interagir com o LawyerApp durante a fase de desenvolvimento e testes.

## 1. Configurar segredos para aceder ao HashiCorp Vault
Executar estes comandos dentro de `src/LawyerApp.API`:

```bash
dotnet user-secrets init
dotnet user-secrets set "VaultSettings:ServerUri" "SECRET"
dotnet user-secrets set "VaultSettings:Token" "your_provided_token"
dotnet user-secrets set "VaultSettings:MountPoint" "your_mountpoint"
dotnet user-secrets set "VaultSettings:SecretPath" "your_secretPath"
dotnet user-secrets set "Jwt:SecretKey" "your_secre_Key"
```
The Vault Setting contain the information necessary to connect and retrieve secrets from the hashicorp cloud server.
The respective secrets are the connections string to the postgreSQL database.

The Jwt:SecretKey is the key used to create a cryptograph signature in the jwt token.

## 2. Executar a aplicação

- .NET CLI

```bash
dotnet run
```
