# DESOFS 2026 — Group Wed PBS 5-2

**Project:** Lawyer App — Secure back-end for a legal consultancy firm, built following SSDLC principles.

---

## Repository Structure

```
.
├── Deliverables/
│   └── Phase 1 Threat Modeling/
│       └── Phase1_Deliverable.md   # Grading criteria → evidence mapping
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

## Phase 1 — Threat Modeling Deliverables

The full evaluation mapping (criteria → file) is
in [Phase1_Deliverable.md](Deliverables/Phase%201%20Threat%20Modeling/Phase1_Deliverable.md).

| Criteria                | Weight | Document                                                                                                                                                                                                                              |
|:------------------------|:------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Organization & Language |   5%   | This README                                                                                                                                                                                                                           |
| Analysis                |  10%   | [Analysis.md](Documentation/Analysis/Analysis.md)                                                                                                                                                                                     |
| Data Flow               |  15%   | [Dataflow.md](Documentation/Dataflow/Dataflow.md)                                                                                                                                                                                     |
| Threat Identification   |  20%   | [ThreatId.md](Documentation/ThreatId/ThreatId.md)                                                                                                                                                                                     |
| Risk Assessment         |  10%   | [Risk_Assessment.md](Documentation/Risk_Assessment/Risk_Assessment.md)                                                                                                                                                                |
| Mitigations             |  10%   | [Mitigations.md](Documentation/Mitigations/Mitigations.md)                                                                                                                                                                            |
| Requirements            |  20%   | [FURPS+.md](Documentation/Requirements/FURPS+.md)                                                                                                                                                                                     |
| Security Testing        |  10%   | [RF01.xlsx](Documentation/Security%20Testing/RF01.xlsx) · [RF02.xlsx](Documentation/Security%20Testing/RF02.xlsx) · [RF03.xlsx](Documentation/Security%20Testing/RF03.xlsx) · [RF04.xlsx](Documentation/Security%20Testing/RF04.xlsx) |

## Phase 2 - Development and Testing

### Running the application

This guide explains how to set up, run, and interact with the LawyerApp during the development and testing phase.

## 1. Configure Secrets to access hashicorp vault
Run these commands inside `src/LawyerApp.API`:

```bash
dotnet user-secrets init
dotnet user-secrets set "VaultSettings:ServerUri" "SECRET"
dotnet user-secrets set "VaultSettings:Token" "your_provided_token"
dotnet user-secrets set "VaultSettings:MountPoint" "your_mountpoint"
dotnet user-secrets set "VaultSettings:SecretPath" "your_secretPath"
```

## 2. Run the Application

- .NET CLI

```bash
dotnet run
```