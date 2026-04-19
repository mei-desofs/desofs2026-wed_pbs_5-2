# Threat ID

- STRIDE-per-element analysis
- Detailed attack vectors
- Abuse cases for threat agents

O que é o Stride?

STRIDE é um modelo usado em cibersegurança para identificar e classificar ameaças num sistema durante a fase de análise (threat modeling).

Cada letra representa um tipo de ameaça:

S — Spoofing (Falsificação de identidade):
- Um atacante finge ser outro utilizador (ex: roubo de credenciais, login falso).

T — Tampering (Manipulação):
- Alteração não autorizada de dados ou sistemas (ex: modificar logs ou pedidos).

R — Repudiation (Repúdio):
- Um utilizador nega ter realizado uma ação, sem prova de registo.

I — Information Disclosure (Divulgação de informação):
- Exposição de dados confidenciais a pessoas não autorizadas.

D — Denial of Service (Negação de serviço):
- Ataques que tornam o sistema indisponível ou lento.

E — Elevation of Privilege (Escalada de privilégios):
- Um utilizador obtém permissões superiores às que deveria ter.


<br><br><br>

## 1 Análise STRIDE - RF01 (Autenticação e RBAC)

Esta análise foca-se nos mecanismos de gestão de identidade, emissão de tokens de sessão e controlo de acessos baseado em perfis (Advogado, Assistente Jurídico e Cliente) da Lawyer App.  
Aplica-se o modelo STRIDE a cada elemento do DFD para identificar vetores de ataque e agentes de ameaça específicos.

![Lvl1_RF01](../Dataflow/lvl1RF01.png)

---

## Mapeamento STRIDE por Elemento

| ID | Elemento         | Elemento DFD            | STRIDE | Ameaça Identificada |
| :--- |:-----------------|:------------------------| :--- | :--- |
| **T1.1** | Processo         | 1.1 Validar Credenciais | **S, D** | **S:** Atacante utiliza *phishing* ou *brute force* para assumir identidade de Advogado. **D:** Ataque de *account lockout* para impedir o acesso legítimo. |
| **T1.2** | Processo         | 1.2 Gerar Token JWT     | **I, E** | **I:** Exposição da *Secret Key* do JWT por má configuração. **E:** Emissão de tokens com *claims* de privilégios elevados indevidos. |
| **T1.3** | Processo         | 1.3 Registar Auditoria  | **R, T** | **R:** Falha no registo de login impede a prova de ações. **T:** Manipulação do registo de log para ocultar acessos não autorizados. |
| **T1.4** | Processo         | 1.4 Validar Acessos     | **E** | **E:** Falha na validação do token permite que um "Cliente" execute funções de "Advogado" (IDOR/Bypass). |
| **T1.5** | Data Store       | D1 Base de Dados        | **T, I** | **T:** Alteração direta da tabela de permissões. **I:** Leitura de *hashes* de passwords por acesso não autorizado à BD. |
| **T1.6** | Data Store       | D2 HashiCorp Vault      | **I, E** | **I:** Fuga de segredos por falta de políticas de acesso (*Access Policies*) restritas. |
| **T1.7** | Entidade externa | Utilizador ↔ Web API    | **T, I** | **T:** Interceção do pedido de login para modificar dados. **I:** Captura de credenciais ou tokens em trânsito (Sniffing). |

---

## Detalhe das Ameaças e Abuse Cases

### A. Escalada de Privilégios via JWT (T1.4)

**Ameaça (Elevation of Privilege):** Um utilizador com o papel de "Cliente" manipula o token de sessão para obter permissões de "Advogado" e aceder a processos de terceiros.

**Agente de Ameaça:** 
- Utilizador autenticado malicioso (ex: Cliente da empresa).
- Atacante externo com acesso a um token válido.

**Vetor de Ataque:** 
- Modificação local do *payload* do JWT (alteração da claim `role`).
- Exploração de algoritmos de assinatura fracos (ex: alteração para `alg: none`).
- Tentativa de acesso a *endpoints* administrativos (`/api/admin/*`) sem verificação rigorosa no lado do servidor.

**Impacto:** 
- Acesso não autorizado a segredos de justiça e dados sensíveis de outros clientes.
- Capacidade de eliminar ou modificar documentos processuais.

---

### B. Falsificação de Identidade e Brute Force (T1.1)

**Ameaça (Spoofing / Denial of Service):** Acesso indevido ao sistema através do roubo de credenciais ou automatização de tentativas de login.

**Agente de Ameaça:** 
- Bot automatizado.
- Atacante externo.

**Vetor de Ataque:**
- *Credential Stuffing* (uso de passwords descobertas de outros sites).
- *Password Spraying* contra contas de advogados conhecidos.
- Geração massiva de pedidos de login para sobrecarregar o serviço de autenticação e a base de dados.

**Impacto:** 
- Comprometimento total da conta de utilizadores privilegiados.
- Indisponibilidade do sistema para utilizadores legítimos devido ao bloqueio de contas ou carga na API.

---

### C. Negação de Auditoria e Repúdio (T1.3)

**Ameaça (Repudiation):** Um utilizador realiza uma ação crítica e nega tê-la feito, aproveitando falhas no registo de logs.

**Agente de Ameaça:** 
- Utilizador interno malicioso (Advogado ou Assistente).

**Vetor de Ataque:** 
- Execução de ações enquanto o serviço de auditoria está offline ou sobrecarregado.
- Exploração de falhas na lógica de registo onde o `UserID` não é corretamente associado ao evento de login.

**Impacto:** 
- Impossibilidade de realizar perícia forense após um incidente.
- Perda de validade jurídica das ações realizadas na plataforma.

---

### D. Manipulação na Geração do Token (T1.2)
**Ameaça (Information Disclosure / Elevation of Privilege):** Exposição da *Secret Key* em memória ou geração de tokens com dados forjados durante o processo de autenticação.

**Agente de Ameaça:** 
- Atacante externo explorando vulnerabilidades na aplicação.
- Atacante interno (ex: programador com acesso a *dumps* de memória do servidor).

**Vetor de Ataque:** 
- Injeção de dados anómalos durante a criação do *payload* do token que forcem o sistema a assumir um *Role* superior por defeito.
- Extração da *Secret Key* da memória da Web API (.NET) através de falhas de *buffer over-read* ou vulnerabilidades de dependências desatualizadas.

**Impacto:** 
- O atacante ganha a capacidade de forjar ("assinar") tokens JWT perfeitamente válidos para qualquer utilizador (incluindo administradores ou advogados seniores), comprometendo totalmente a integridade do sistema.

---

### E. Compromisso do Armazém de Dados (T1.5 - D1 Base de Dados)
**Ameaça (Tampering / Information Disclosure):** Acesso não autorizado à tabela de utilizadores para extrair ou alterar dados críticos.

**Agente de Ameaça:** 
- Atacante externo (explorando a API).
- Ameaça interna (ex: administrador de sistemas/DBA malicioso).
  
**Vetor de Ataque:** 
- Exploração de vulnerabilidades de *SQL Injection* (caso o Entity Framework Core não seja usado corretamente nalgum *endpoint* antigo).
- Acesso direto à rede da base de dados contornando a *Trust Boundary* por má configuração da *firewall* da Cloud.
  
**Impacto:** 
- **Information Disclosure:** Fuga massiva de emails e *hashes* de passwords (embora mitigado pelo uso de Argon2id, a lista de emails fica exposta para ataques de *phishing*).
- **Tampering:** O atacante altera diretamente a coluna `Role` de um cliente na base de dados para "Advogado", escalando privilégios de forma permanente e invisível para a aplicação.

---

### F. Fuga de Segredos no Cofre Digital (T1.6 - D2 HashiCorp Vault)
**Ameaça (Information Disclosure / Elevation of Privilege):** Obtenção indevida das chaves mestras e segredos da aplicação armazenados no Key Vault.

**Agente de Ameaça:** 
- Atacante externo avançado.
- Ex-colaborador com acessos não revogados à infraestrutura Cloud.
  
**Vetor de Ataque:** 
- Descoberta acidental do *Client ID* e *Client Secret* (Credenciais de acesso ao Key Vault) deixados no código-fonte no GitHub (*hardcoded secrets*).
- Má configuração do RBAC do HashiCorp, permitindo que utilizadores com privilégios de "Leitura" na Cloud consigam extrair o valor dos *Secrets*.
  
**Impacto:** 
- Desastre total de segurança. O atacante não só pode forjar logins (roubando a *JWT Secret Key*), como também poderá ter acesso às chaves AES-256 usadas para cifrar os documentos confidenciais dos processos jurídicos.

---

### G. Interceção de Tráfego de Rede (T1.7 - Fluxo Utilizador ↔ Web API)
**Ameaça (Tampering / Information Disclosure):** Escuta e modificação dos pacotes de dados enquanto viajam pela internet entre o cliente e o servidor.

**Agente de Ameaça:** 
- Atacante local posicionado na mesma rede do utilizador (ex: Wi-Fi público de um tribunal ou café).
- ISP ou interveniente malicioso na infraestrutura de rede.
  
**Vetor de Ataque:** 
- Ataque *Man-in-the-Middle* (MitM) através de envenenamento de ARP (ARP Spoofing) em redes locais.
- Ataques de *TLS Stripping* para forçar a vítima a comunicar via HTTP em vez de HTTPS.
- Captura de tráfego de rede (Packet Sniffing).
  
**Impacto:** 
- Roubo das credenciais (email e password) em texto limpo durante o momento exato em que o utilizador tenta fazer login.
- Roubo do Token JWT (*Session Hijacking*), permitindo ao atacante aceder à aplicação em nome do utilizador sem necessitar da password.

---

**Mitigações sugeridas (Traceability RF01):** 
- Uso de **Argon2id** para proteção robusta de credenciais na D1.
- Autenticação no HashiCorp Vault (D2) via *Managed Identities*, eliminando credenciais no código.
- Configuração de **Rate Limiting** e **Lockout** para mitigar ataques de força bruta no login.
- Configuração rigorosa de **HSTS** (HTTP Strict Transport Security) na Web API para impedir comunicações não cifradas e forçar o uso exclusivo de **TLS 1.2/1.3**.
- Validação rigorosa de assinaturas JWT no servidor para impedir manipulação de papéis (RBAC).
- Uso exclusivo de ORM (Entity Framework) com *queries* parametrizadas na Base de Dados.

<br><br><br>
## 3 Análise STRIDE - RF03 (Organização de Sistema de Ficheiros)

Esta análise foca-se nos processos de criação automática de estrutura de diretórios da Lawyer App.  
Aplica-se o modelo STRIDE a cada elemento do DFD para identificar vetores de ataque e agentes de ameaça específicos.

![Lvl1_RF03](../Dataflow/lvl1RF03.png)

---

## Mapeamento STRIDE por Elemento

| ID | Elemento | Elemento DFD | STRIDE | Ameaça Identificada |
|----|----------|--------------|--------|---------------------|
| P3.1 | Captura do Evento de Criação | Processo | S / T / R | Falsificação de pedidos de criação ou forja de identidade |
| P3.2 | Criação de Estrutura de Diretórios | Processo | T / I / D | Path traversal, race conditions, ou negação de serviço |
| P3.3 | Persistência do Evento | Processo | T / R / I | Manipulação de metadados ou perda de rastreabilidade |
| DS3.1 | Sistema de Ficheiros | Data Store | T / I / D | Acesso direto ao filesystem, modificação ou eliminação de estruturas |
| DS3.2 | Base de Dados | Data Store | T / R / I | SQL Injection ou manipulação de registos de processos |
| EXT3.1 | Advogado | Entidade Externa | S / D | Falsificação de identidade ou abuso de criação massiva |

---

## Detalhe das Ameaças e Vetores de Ataque

### A. Processo: Criação de Estrutura de Diretórios (P3.2)

**Ameaça (Tampering / Information Disclosure):** Path Traversal Attack  

**Agente de Ameaça:**  
- Atacante externo com credenciais comprometidas  
- Utilizador interno malicioso (Advogado comprometido)

**Vetor de Ataque:**  
Manipulação do Process ID ou tentativa de injeção de caracteres especiais no path:
- `../../../etc/passwd`
- `..\..\..\windows\system32`
- Null bytes (`%00`)
- Unicode encoding bypass

**Impacto:**  
- Acesso a ficheiros fora do diretório esperado  
- Leitura de ficheiros sensíveis do sistema operativo  
- Escrita em diretórios críticos  
- Comprometimento total do servidor  

---

### B. Processo: Criação de Estrutura de Diretórios (P3.2)

**Ameaça (Denial of Service):** Filesystem Exhaustion Attack  

**Agente de Ameaça:**  
- Atacante com credenciais válidas  
- Script automatizado  
- Utilizador malicioso interno

**Vetor de Ataque:**  
- Criação massiva de processos em curto espaço de tempo  
- Cada processo cria múltiplos subdiretórios  
- Esgotamento de inodes ou espaço em disco

**Impacto:**  
- Sistema de ficheiros cheio  
- Impossibilidade de criar novos processos  
- Degradação de performance  
- Potencial crash do sistema

---

### C. Data Store: Sistema de Ficheiros (DS3.1)

**Ameaça (Tampering / Information Disclosure):** Acesso direto ao filesystem  

**Agente de Ameaça:**  
- Atacante que conseguiu RCE (Remote Code Execution)  
- Processo comprometido no servidor  
- Administrador malicioso com acesso SSH

**Vetor de Ataque:**  
- Bypass da aplicação e acesso direto via shell  
- Leitura de ficheiros sem passar pelo RBAC  
- Modificação ou eliminação de estruturas de diretórios  
- Criação de symlinks maliciosos

**Impacto:**  
- Exposição de documentos confidenciais  
- Perda de integridade dos dados  
- Violação de GDPR/confidencialidade  
- Impossibilidade de confiar nos dados

---

### D. Processo: Captura do Evento de Criação (P3.1)

**Ameaça (Spoofing):** Falsificação de pedido de criação  

**Agente de Ameaça:**  
- Atacante com token JWT roubado  
- Session hijacking  
- Man-in-the-middle (se TLS comprometido)

**Vetor de Ataque:**  
- Replay attack com pedido de criação interceptado  
- Uso de token válido mas expirado  
- Forja de claims no JWT (se secret comprometido)

**Impacto:**  
- Criação de processos em nome de outro utilizador  
- Quebra de não-repúdio  
- Atribuição incorreta de responsabilidade  
- Auditoria comprometida

---

### E. Processo: Criação de Estrutura de Diretórios (P3.2)

**Ameaça (Tampering):** Race Condition / TOCTOU  

**Agente de Ameaça:**  
- Atacante sofisticado  
- Script de automação malicioso

**Vetor de Ataque:**  
- Time-of-Check to Time-of-Use (TOCTOU)  
- Duas threads tentam criar o mesmo processo simultaneamente  
- Criação de symlink entre verificação e criação

**Impacto:**  
- Criação de estruturas duplicadas  
- Corrupção de dados  
- Possível escalada para path traversal  
- Inconsistência entre BD e filesystem

---

### F. Data Store: Base de Dados (DS3.2)

**Ameaça (Tampering / Repudiation):** SQL Injection  

**Agente de Ameaça:**  
- Atacante externo  
- Exploração de vulnerabilidade em input validation

**Vetor de Ataque:**  
- Injeção de SQL no nome do processo (se aceitar input do user)  
- Payload: `'; DROP TABLE Processos; --`  
- Extração de dados via UNION-based injection

**Impacto:**  
- Perda total de dados  
- Exposição de informação confidencial  
- Modificação de metadados de processos  
- Escalada de privilégios

---

### G. Entidade Externa: Advogado (EXT3.1)

**Ameaça (Denial of Service / Spoofing):** Abuso de criação massiva de processos  

**Agente de Ameaça:**  
- Advogado com credenciais comprometidas  
- Bot automatizado  
- Insider threat (funcionário descontente)

**Vetor de Ataque:**  
- Script que cria centenas de processos por minuto  
- Uso de credenciais válidas (bypass de autenticação)  
- Exploração de ausência de rate limiting

**Impacto:**  
- Esgotamento de recursos (CPU, disco, inodes)  
- Degradação de performance para utilizadores legítimos  
- Custos elevados de storage  
- Logs poluídos (dificulta análise forense)

---

<br><br><br>

## 4 Análise STRIDE - RF04 (Auditoria e Logging)

Esta análise foca-se nos processos internos do sistema de auditoria da Lawyer App.  
Aplica-se o modelo STRIDE a cada elemento do DFD para identificar vetores de ataque e agentes de ameaça específicos.

![Lvl1_RF04](../Dataflow/lvl1RF04.png)

---

### 4.1 Mapeamento STRIDE por Elemento

| ID | Elemento | Elemento DFD | STRIDE | Ameaça Identificada |
|----|----------|--------------|--------|---------------------|
| T4.1 | Captura de Evento | Processo | S / T / R | Interceção ou forja de eventos antes do registo |
| T4.2 | Tratamento do Evento | Processo | T / R / I | Injeção de caracteres maliciosos ou fuga de dados sensíveis |
| T4.3 | Persistência (PostgreSQL) | Data Store | T / R / I | Modificação ou eliminação de registos de auditoria |
| T4.4 | Atores do Sistema | Entidade Externa | S / D / T | Falsificação de Identidade e sobrecarga do Sistema |
---

### 4.2 Detalhe das Ameaças e Vetores de Ataque

### A. Processo: Tratamento do Evento (T4.2)

**Ameaça (Tampering / Repudiation):** Log Injection  

**Agente de Ameaça:**  
Utilizador interno (ex: Assistente Jurídico malicioso)

**Vetor de Ataque:**  
Inserção de caracteres especiais ou sequências de escape em inputs da API, como:
- `\n`
- `\r`
- payloads maliciosos

**Impacto:**  
- Corrupção da integridade dos logs  
- Dificuldade ou impossibilidade de análise forense  
- Possível ocultação de ações maliciosas  

---

### B. Data Store: Persistência do Evento (T4.3)

**Ameaça (Information Disclosure / Tampering):** Acesso indevido à base de dados de logs  

**Agente de Ameaça:**  
- Atacante externo  
- Utilizador com permissões de Cliente mal configuradas  

**Vetor de Ataque:**  
- SQL Injection no backend  
- Falhas de RBAC (controlo de acessos incorreto)  

**Impacto:**  
- Exposição de metadados sensíveis  
- Comprometimento de processos jurídicos  
- Violação de confidencialidade  

---

### C. Processo: Captura de Evento (T4.1)

**Ameaça (Spoofing):** Forja de eventos de sistema  

**Agente de Ameaça:**  
Atacante que comprometeu sessão ou token de autenticação  

**Vetor de Ataque:**  
- Envio direto de requests falsificados para o serviço de logging  
- Simulação de ações realizadas por outros utilizadores  

**Impacto:**  
- Quebra de não-repúdio  
- Atribuição incorreta de ações  
- Perda de confiança nos logs  

---

### D. Entidade Externa: Atores do Sistema (T4.4)

**Ameaça (Spoofing / Denial of Service / Tampering):**  
Falsificação de identidade e abuso de funcionalidades legítimas do sistema

**Agente de Ameaça:**  
- Utilizador externo malicioso  
- Cliente com credenciais comprometidas  
- Utilizador autenticado (Advogado ou Assistente Jurídico malicioso)  
- Bot automatizado  

**Vetor de Ataque:**  
- Uso de credenciais roubadas para simular ações legítimas (login, acesso a processos ou documentos)  
- Execução massiva de operações válidas (upload/download/logging flood)  
- Exploração de permissões legítimas para gerar carga excessiva no sistema  
- Tentativas de acesso repetido a endpoints protegidos (brute force / automation)  

**Impacto:**  
- Quebra de autenticidade dos utilizadores (Spoofing)  
- Sobrecarga da API e do sistema de auditoria (Denial of Service)  
- Geração excessiva de logs, afetando performance da base de dados  
- Possível escalada de ataques internos via utilizadores válidos comprometidos  

**Mitigações sugeridas:**  
- Autenticação forte com JWT + expiração curta  
- Rate limiting por utilizador e por IP  
- Deteção de comportamento anómalo (anomaly detection)  
- Bloqueio automático após tentativas suspeitas  
- Separação de privilégios entre roles (RF01 RBAC)  
