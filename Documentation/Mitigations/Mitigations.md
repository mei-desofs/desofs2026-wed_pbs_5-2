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
