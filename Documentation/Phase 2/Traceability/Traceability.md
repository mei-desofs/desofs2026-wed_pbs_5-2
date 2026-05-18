# Requisitos de Segurança vs Testes

## 1. Objetivo

Este documento resume quais requisitos de segurança foram considerados no Sprint 1 da Phase 2 e a evidência associada aos testes realizados. 

## 2. Âmbito

- Requisito principal considerado: **RF01 – Gestão de Perfis de Utilizador (RBAC)**
- O foco desta matriz é ligar requisitos de segurança a testes existentes e a evidências do DAST/Integração Contínua.

## 3. Requisitos de segurança considerados

### 3.1 RF01 – Gestão de Perfis de Utilizador (RBAC)
- Hashing de passwords com **BCrypt**
- Controlo de acesso baseado em roles no backend
- Proteção de secrets e credenciais (planeado via HashiCorp Vault)
- Comunicação segura em trânsito (HTTPS)
- Registos de eventos de autenticação/autorização

### 3.2 Outros itens de segurança relevantes para este sprint
- Validação de cabeçalhos e respostas da API
- Segurança de dependências e pipeline de SCA
- Configurações de registos e erros no backend

## 4. Mapa de requisitos de segurança vs testes

| Requisito de segurança | Teste realizado | Evidência / código | Estado Sprint 1 |
|---|---|---|---|
| Hashing de passwords | Teste unitário de hashing BCrypt | `LawyerApp.Tests/Unit/Security/BCryptPasswordHasherTests.cs`; `LawyerApp/Infrastructure/Security/BCryptPasswordHasher.cs` | Implementado |
| Criação de cliente com password | Teste de integração do endpoint do cliente | `LawyerApp.Tests/Integration/API/ClientControllerTests.cs`; `LawyerApp/API/Client/ClientController.cs`; `ClientService.cs` | Parcial |
| Autenticação e RBAC no backend | Revisão de código: `Program.cs` + domínio RBAC | [FURPS.md](../../Phase%201/Requirements/FURPS+.md); `Program.cs`; `Domain/Aggregates/UserAggregate` | Parcial / em desenvolvimento |
| Proteção de secrets e dependências | Documentação de pipeline e SCA | [Artifact_Scanning.md](../Build_Test/Artifact_Scanning.md); [Pipeline.md](../Build_Test/Pipeline.md); [Technical_Report.md](../Build_Test/Technical_Report.md) | Implementado |
| Comunicação segura (HTTPS) | DAST OWASP ZAP identificou uso de HTTP only | [DAST-ZAP-Report.md](../DAST/DAST-ZAP-Report.md); [zap-active-report.pdf](../DAST/zap-active-report.pdf) | Identificado (correção na Sprint 2) |
| Cabeçalhos de cache e segurança | DAST OWASP ZAP identificou falta de headers | [DAST-ZAP-Report.md](../DAST/DAST-ZAP-Report.md) | Identificado |
| Logging e auditoria | Revisão de código e análise de logs existentes | `ClientController.cs`; [Code_Review_Report.md](../Development/Code_Review_Report.md) | Parcial |

## 5. Relacionamento com ASVS

- O ficheiro [ASVS_analysis.xlsx](../ASVS/ASVS_analysis.xlsx) contém as referências ASVS para os controles de segurança.
- Nesta matriz, mantemos as referências ao ASVS como suporte, mas o foco principal é ligar requisitos de segurança aos testes e resultados existentes.
