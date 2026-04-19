# 1. Project Summary
---

## 2. Evaluation Mapping Table
The following table maps the grading criteria to the specific evidence and documentation located within this repository.

| Criteria | Weight | Requirements Addressed | Repository Reference | Key Highlights |
| :--- | :--- | :--- | :--- | :--- |
| **Organization & Language** | **5%** | Document Structure, Links, Grammar | [README.md](../../README.md) | Centralized repository with logical folder hierarchy; all components linked for easy navigation. |
| **Analysis** | **10%** | System Overview, Domain Model, Architecture | [Analysis.md](../../Analysis.md)   | Full tech stack description and domain model identifying all major system components. |
| **Data Flow** | **15%** | Level 0 & 1 DFDs, Trust Boundaries |                              | DFDs using standard notation; Level 2+ provided for complex logic; clear Trust Boundaries. |
| **Threat ID** | **20%** | STRIDE Analysis, Abuse Cases |                              | STRIDE-per-element analysis; detailed attack vectors and abuse cases for threat agents. |
| **Risk Assessment** | **10%** | Methodology, Prioritisation |                              | Quantified risk scoring (DREAD/CVSS) used to justify mitigation priority for identified threats. |
| **Mitigations** | **10%** | Clear & Feasible Mitigations |                              | Specific architectural counters linked to high-priority threats (e.g., Argon2id, TLS 1.3). |
| **Requirements** | **20%** | Security Reqs (Auth, Data, Input) | [FURPS+.md](../../FURPS+.md)       | Justified requirements covering the 6 core pillars (Auth, Access, Data, Input, 3rd Party, Logging). |
| **Security Testing** | **10%** | Methodology, ASVS, Traceability |                              | Full ASVS Level 2 assessment; test cases mapped directly to threats and abuse cases. |
---
