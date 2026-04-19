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