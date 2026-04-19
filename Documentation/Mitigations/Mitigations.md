# Mitigations

Este documento detalha as contramedidas arquiteturais e técnicas propostas para mitigar as ameaças identificadas durante a fase de análise.
O foco recai sobre as ameaças de prioridade **Crítica**, **Muito Alta** e **Alta**, garantindo a resiliência do sistema através do princípio
de *Defense in Depth*, mas abordando também riscos médios relevantes para a integridade da auditoria e infraestrutura.

---

## 1. RF01 - Autenticação e RBAC

O plano de mitigação para o sistema de gestão de identidades e acessos baseia-se em padrões da indústria (OWASP ASVS) e na utilização de
funcionalidades nativas de segurança do ecossistema .NET e HashiCorp Vault.

| ID Ameaça | Nível de Risco | Ameaça Principal                  | Mitigação Arquitetural e Técnica (Contramedida)                                                                                                                                                                                                                                                    | Componente Responsável |
|:----------|:---------------|:----------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:-----------------------|
| **T1.4**  | **Muito Alto** | Escalada de Privilégios via JWT   | **Validação Estrita de JWT:** Configuração do middleware `JwtBearer` para validar assinatura, expiração (`exp`) e audiência (`aud`). Rejeição mandatória do algoritmo `none`. Verificação de *Roles* no servidor em cada pedido.                                                                   | Web API (Auth Layer)   |
| **T1.1**  | **Muito Alto** | Brute Force e Credential Stuffing | **Rate Limiting & Lockout:** Implementação de limite de pedidos por IP e conta no endpoint de login. Bloqueio progressivo de conta (*Account Lockout*) após 5 tentativas falhadas para mitigar ataques automatizados.                                                                              | Web API / Database     |
| **T1.2**  | **Alto**       | Fuga da *Secret Key* do JWT       | **Isolamento de Segredos:** A chave de assinatura do token é armazenada exclusivamente no **HashiCorp Vault**. A API acede ao segredo via *Managed Identity*, eliminando credenciais no código-fonte.                                                                                              | HashiCorp Vault        |
| **T1.5**  | **Alto**       | Fuga da Base de Dados             | **Proteção de Credenciais:** Utilização de **Argon2id** com *salt* único para o *hashing* de passwords. Acesso a dados via ORM (Entity Framework) para prevenir injeções de SQL.                                                                                                       | Database               |
| **T1.7**  | **Alto**       | Interceção de Tráfego (Sniffing)  | **Cifragem em Trânsito:** Imposição de comunicações cifradas via **TLS 1.2/1.3**. Implementação de **HSTS** (*HTTP Strict Transport Security*) para prevenir ataques de *downgrade* de protocolo.                                                                                                  | Infraestrutura / API   |
| **T1.3**  | **Médio**      | Negação de Auditoria              | **Logging de Segurança:** Registo imutável de todos os eventos de login (sucesso/falha) com IP, UserID e Timestamp para garantir o não-repúdio e suporte a perícia forense.                                                                                                                        | Web API / Database     |
| **T1.6**  | **Médio**      | Fuga de Segredos do Vault         | **Políticas de Acesso Restritas (RBAC HashiCorp Vault):** Configuração do *HashiCorp Vault* para aceitar ligações exclusivamente da *Managed Identity* da Web API, removendo permissões de leitura/listagem de segredos a contas de utilizadores humanos ou programadores no ambiente de produção. | Infraestrutura         |

### Viabilidade Técnica (Feasibility)

As mitigações propostas utilizam bibliotecas standard do .NET (como `Microsoft.AspNetCore.Authentication.JwtBearer`). 
Isto garante que a implementação na Fase 2 é viável, não requer criptografia customizada e minimiza a superfície de ataque ao
utilizar ferramentas amplamente testadas pela comunidade de segurança.


<br><br><br>

## RF03 - Organização de Sistema de Ficheiros

O plano de mitigação para o sistema de criação automática de estruturas de diretórios baseia-se em padrões da indústria (OWASP ASVS, CWE Top 25) e na utilização de funcionalidades nativas de segurança do ecossistema .NET e Azure.

| ID Ameaça | Nível de Risco | Ameaça Principal                        | Mitigação Arquitetural e Técnica (Contramedida)                                                                                                                                                                                                                                                                                      | Componente Responsável        |
|:----------|:---------------|:----------------------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------|
| **T3.7**  | **Crítico**    | Abuso de Criação Massiva de Processos   | **Rate Limiting & Anomaly Detection:** Implementação de limite de 10 criações de processos por hora por utilizador através de *sliding window counter*. Sistema de deteção de anomalias que bloqueia automaticamente contas com padrões suspeitos (>50 criações/dia). CAPTCHA após 5 criações consecutivas em <10 minutos.             | Web API / Redis Cache         |
| **T3.2**  | **Muito Alto** | Filesystem Exhaustion (DoS)             | **Quotas de Disco & Monitoring:** Configuração de quotas de armazenamento de 500GB por utilizador no sistema de ficheiros. Alertas automáticos quando ocupação atinge 80%. Cleanup automático de processos inativos há mais de 90 dias. Throttling de API para operações de criação de diretórios.                                     | Filesystem / Monitoring       |
| **T3.3**  | **Muito Alto** | Acesso Direto ao Filesystem             | **Cifra em Repouso (Encryption at Rest):** Todos os ficheiros são cifrados com **AES-256-GCM** antes de serem armazenados. Chaves de cifra geridas exclusivamente via **Azure Key Vault** com acesso restrito via *Managed Identity*. Ficheiros armazenados fora da raiz web (`/var/data/processos/`). Permissões filesystem (chmod 700). | Azure Key Vault / Filesystem  |
| **T3.4**  | **Alto**       | Falsificação de Pedido de Criação       | **Gestão Segura de Sessões:** Implementação de *refresh tokens* com rotação automática. Expiração de JWT curta (15 minutos). Sistema de *anti-replay* usando nonce único por pedido. Validação rigorosa de *claims* (userId, role, issuer) em cada pedido de criação.                                                                 | Web API (Auth Layer)          |
| **T3.1**  | **Alto**       | Path Traversal Attack                   | **Validação Rigorosa de Paths:** Utilização mandatória de `Path.Combine()` e `Path.GetFullPath()` para construção de caminhos. GUID gerado server-side (não aceita input do utilizador). Verificação que o path final está sempre dentro do diretório base esperado. Rejeição de caracteres especiais (`../`, `..\\`, null bytes).   | Web API (File Operations)     |
| **T3.6**  | **Alto**       | SQL Injection                           | **ORM & Queries Parametrizadas:** Acesso exclusivo à base de dados via ORM (Entity Framework Core) com queries parametrizadas. GUID gerado server-side elimina input malicioso. SAST automático no pipeline CI/CD para detetar uso indevido de `FromSqlRaw`. Princípio do menor privilégio (user DB sem DROP/TRUNCATE).              | Database / CI/CD Pipeline     |
| **T3.5**  | **Médio**      | Race Condition / TOCTOU                 | **Transações Atómicas:** Uso de transações explícitas do Entity Framework para garantir atomicidade na criação (estrutura de diretórios + registo na BD). Locks pessimistas para prevenir criações simultâneas do mesmo processo. Rollback automático em caso de erro. Verificação de integridade pós-criação.                         | Database / Web API            |

### Viabilidade Técnica (Feasibility)

As mitigações propostas utilizam componentes standard do ecossistema .NET e Azure:

| Componente                  | Tecnologia                                   | Maturidade | Complexidade |
|-----------------------------|----------------------------------------------|------------|--------------|
| Rate Limiting               | `Microsoft.AspNetCore.RateLimiting` (built-in) | Stable     | Baixa        |
| Cifra AES-256-GCM           | `Azure.Security.KeyVault.Keys`               | Production | Média        |
| Path Validation             | `System.IO.Path` (built-in)                  | Stable     | Baixa        |
| ORM (EF Core)               | `Microsoft.EntityFrameworkCore`              | Production | Baixa        |
| Anti-Replay Nonce           | Redis Cache + JWT Claims                     | Proven     | Média        |
| Transações Atómicas         | Entity Framework Transactions                | Stable     | Baixa        |
| SAST                        | GitHub Advanced Security / SonarQube         | Production | Baixa        |

**Conclusão:** Todas as mitigações são implementáveis com bibliotecas standard e testadas pela comunidade. Não requer desenvolvimento de criptografia customizada ou componentes experimentais.