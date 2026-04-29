# Risk Assessment

- Quantified risk scoring (DREAD/CVSS) used to justify mitigation priority for identified threats.

Neste Projeto, o sistema de pontuação escolhido foi o DREAD.
O DREAD é um sistema de pontuação utilizado para avaliar e priorizar riscos de segurança, onde é atribuido uma nota (geralmente de 1 a 10) a
cinco categorias para cada ameaça que é encontrada no sistema.

- D - Damage Potential (Dano): Se isto acontecer, o quão grave é? (Ex: Roubar todos os processos jurídicos = 10; Mudar a cor de um botão =
    1) .

- R - Reproducibility (Reprodutibilidade): É fácil fazer isto acontecer outra vez? (Ex: Acontece sempre que carrego num link = 10; Só
  acontece se o servidor falhar ao mesmo tempo que chove = 1) .

- E - Exploitability (Explorabilidade): É preciso ser um génio para atacar? (Ex: Qualquer pessoa com um browser consegue = 10; Preciso de
  meses de planeamento e ferramentas caras = 1) .

- A - Affected Users (Utilizadores Afetados): Quantas pessoas sofrem com isto? (Ex: Todos os advogados e clientes = 10; Apenas um utilizador
  específico = 1) .

- D - Discoverability (Descoberta): É fácil encontrar esta falha? (Ex: Está à vista de todos na página inicial = 10; Está escondida em
  código que ninguém vê = 1) .

A nota final é o resultado da média das 5 categorias!

## 1 RF01 (Autenticação e RBAC)

#### 1.1 Tabela da análise stride:

| ID   | Elemento   | Elemento DFD         | STRIDE | Ameaça Identificada                      |
|------|------------|----------------------|--------|------------------------------------------|
| T1.1 | Processo   | Validar Credenciais  | S / D  | Falsificação de Identidade e Brute Force |
| T1.2 | Processo   | Gerar Token JWT      | I / E  | Manipulação na Geração do Token          |
| T1.3 | Processo   | Registar Auditoria   | R / T  | Negação de Auditoria e Repúdio           |
| T1.4 | Processo   | Validar Acessos      | E      | Escalada de Privilégios via JWT          |
| T1.5 | Data Store | Base de Dados        | T / I  | Fuga da Base de Dados                    |
| T1.6 | Data Store | HashiCorp Vault      | I / E  | Fuga de Segredos do Vault                |
| T1.7 | Fluxo      | Utilizador ↔ Web API | T / I  | Interceção de Tráfego de Rede            |

#### 1.2 Tabela de Avaliação

| ID   | Ameaça                                   | D  | R  | E | A  | D | Total | Risco      | Justificação                                                                    |
|------|------------------------------------------|----|----|---|----|---|-------|------------|---------------------------------------------------------------------------------|
| T1.1 | Falsificação de Identidade e Brute Force | 8  | 10 | 8 | 6  | 8 | 40    | Muito Alto | Scripts automatizados são fáceis de criar e comprometem contas críticas.        |
| T1.2 | Manipulação na Geração do Token          | 10 | 8  | 6 | 10 | 4 | 38    | Alto       | Se a secret key for exposta, todo o sistema fica comprometido.                  |
| T1.3 | Negação de Auditoria e Repúdio           | 6  | 8  | 6 | 4  | 6 | 30    | Médio      | Impacto na auditoria forense se os logs de login falharem.                      |
| T1.4 | Escalada de Privilégios via JWT          | 8  | 10 | 8 | 8  | 8 | 42    | Muito Alto | Ferramentas comuns permitem forjar tokens se não houver validação rigorosa.     |
| T1.5 | Fuga da Base de Dados                    | 10 | 4  | 4 | 10 | 4 | 32    | Alto       | Fuga de dados massiva, mitigada pela dificuldade de executar SQLi num ORM.      |
| T1.6 | Fuga de Segredos do Vault                | 10 | 4  | 2 | 10 | 4 | 30    | Médio      | Impacto destrutivo, mas extremamente complexo de explorar a partir do exterior. |
| T1.7 | Interceção de Tráfego de Rede            | 8  | 6  | 6 | 6  | 6 | 32    | Alto       | Permite roubo de credenciais via MitM se o tráfego não for forçado a HTTPS.     |

---

#### 1.3 Conclusão do Risk Assessment

A aplicação da metodologia DREAD permitiu identificar e priorizar de forma objetiva as ameaças mais críticas ao sistema de autenticação.
Destacam-se três níveis principais de risco:

- **Muito Alto**:
    - *Escalada de Privilégios via JWT (42)*
    - *Falsificação de Identidade e Brute Force (40)*

Estas ameaças são as mais críticas do RF01 devido à facilidade de exploração (uso de *bots* e manipuladores de JWT públicos) e impacto
direto no acesso não autorizado a processos jurídicos.

- **Alto**:
    - *Manipulação na Geração do Token (38)*
    - *Fuga da Base de Dados (32)*
    - *Interceção de Tráfego de Rede (32)*

Ameaças focadas na obtenção indevida de dados de acesso e segredos criptográficos (fugas de
*hashes* ou *sniffing* na rede).

- **Médio**:
    - *Negação de Auditoria e Repúdio (30)*
    - *Fuga de Segredos do Vault (30)*

Riscos cuja exploração exige acesso privilegiado interno prévio ou que afetam principalmente a camada de rastreabilidade.

#### 1.4 Priorização de Mitigações

Com base nesta análise, as medidas de mitigação devem ser implementadas pela seguinte ordem de prioridade:

1. **Gestão de Sessões e Integridade de Tokens**
    - Validação absoluta de assinaturas JWT
    - Isolamento da chave secreta da aplicação

2. **Proteção de Autenticação e Fronteira**
    - Rate limiting no endpoint de login
    - Políticas de bloqueio (Account Lockout)

3. **Cifragem de Dados e Comunicação Segura**
    - Hashing forte (Argon2id)
    - Imposição de HTTPS (TLS 1.2/1.3 e HSTS)

4. **Auditoria de Acessos**
    - Registo centralizado e protegido de tentativas de login

#### 1.5 Mitigações Associadas às Ameaças Prioritárias

Com base nos resultados do DREAD, foram definidas mitigações específicas alinhadas com as ameaças de maior risco do RF01:

| Ameaça                               | Mitigação                                                                                                             |
|--------------------------------------|-----------------------------------------------------------------------------------------------------------------------|
| Escalada de Privilégios via JWT      | Validação mandatória da assinatura JWT, impedimento do algoritmo `none` e verificação estrita de *Claims* (Role).     |
| Falsificação de Ident. e Brute Force | Implementação de *Rate Limiting* por IP, *Account Lockout* progressivo e mensagens de erro genéricas.                 |
| Manipulação na Geração do Token      | Armazenamento da *JWT Secret Key* no HashiCorp Vault (acesso apenas via *Managed Identities*).                        |
| Compromisso do Armazém de Dados      | Passwords armazenadas usando *Hashing* forte (**Argon2id** com *salts*). Acesso exclusivo via ORM (Entity Framework). |
| Interceção de Tráfego de Rede        | Imposição obrigatória de HTTPS (TLS 1.2/1.3) e configuração de HSTS na Web API.                                       |
| Negação de Auditoria                 | Geração de logs de segurança com IP e Timestamp a cada tentativa de login, gravados numa tabela protegida.            |



<br><br><br>

## 2 RF02 (Gestão Documental)

#### 2.1 Tabela da análise STRIDE:

| ID    | Elemento                   | Elemento DFD     | STRIDE    | Ameaça Identificada                                                                           |
|-------|----------------------------|------------------|-----------|-----------------------------------------------------------------------------------------------|
| P2.1  | Receção do Pedido          | Processo         | S / T / D | JWT falsificado ou expirado; pedidos mal formados para causar erro; flood de pedidos para DoS |
| P2.2  | Controlo de Autorização    | Processo         | S / E / R | IDOR / BOLA via Process ID; mudança de role; ausência de log de acessos negados               |
| P2.3  | Tratamento do Ficheiro     | Processo         | T / I / D | Upload de ficheiro malicioso; falha de cifra; tamanho do documento pode causar DoS           |
| P2.4  | Persistência / Recuperação | Processo         | T / R / I | SQL Injection via Process ID ou filename; ausência de atomicidade; falta de log de operações |
| DS2.1 | SQL Server (Documentos)    | Data Store       | T / I / R | Acesso direto à BD sem passar pela API; ausência de cifra adequada expõe conteúdo            |
| DS2.2 | SQL Server (Utilizadores)  | Data Store       | S / T / I | Passwords em plaintext; modificação direta de role na BD; roubo de credenciais               |
| EE2.1 | Atores do Sistema          | Entidade Externa | S / D / E | Credenciais comprometidas; flood massivo de uploads/downloads                                |
| EE2.2 | Emissor de JWT             | Entidade Externa | S / T     | Emissor comprometido; algoritmo `none` aceite por má configuração                            |

#### 2.2 Tabela de Avaliação

| ID    | Ameaça                            | D  | R  | E  | A  | D  | Total | Risco      | Justificação                                                                                                          |
|-------|-----------------------------------|----|----|----|----|----|-------|------------|-----------------------------------------------------------------------------------------------------------------------|
| EE2.1 | Flood de uploads / DoS            | 8  | 9  | 9  | 10 | 8  | 44    | Crítico    | Script trivial com credenciais válidas. Sem quotas nem rate limiting esgota o SQL Server rapidamente.                 |
| P2.2  | IDOR / BOLA via Process ID        | 10 | 8  | 7  | 10 | 8  | 43    | Crítico    | Qualquer utilizador autenticado pode iterar Process IDs sem verificação de ownership. Viola sigilo profissional.      |
| P2.3  | Upload de ficheiro malicioso      | 10 | 7  | 7  | 9  | 6  | 39    | Muito Alto | DOCX com XXE é trivial de construir. Zip-bomb esgota SQL Server. Malware distribuído a todos os utilizadores.         |
| EE2.1 | Credenciais comprometidas         | 8  | 6  | 6  | 8  | 6  | 34    | Alto       | JWT com expiração longa ou passwords fracas aumentam a janela de ataque. Brute force sem lockout facilita o acesso.  |
| P2.4  | SQL Injection                     | 10 | 4  | 4  | 10 | 4  | 32    | Alto       | Impacto total na BD com documentos jurídicos sensíveis. Mitigado pelo Entity Framework, mas o risco persiste.        |
| P2.1  | JWT forjado / algoritmo `none`    | 10 | 3  | 4  | 10 | 5  | 32    | Alto       | Impacto catastrófico se a chave for comprometida. Probabilidade baixa com configuração correta do ASP.NET Core.      |
| DS2.2 | Aumento de role via BD            | 8  | 3  | 3  | 8  | 4  | 26    | Alto       | Acesso direto ao SQL Server permite modificar roles. Mitigado por permissões de BD restritas.                        |
| DS2.1 | Metadados sensíveis em ficheiros  | 5  | 8  | 8  | 7  | 8  | 36    | Médio      | PDF e DOCX frequentemente contêm nomes de autores e histórico de revisões — fácil de explorar.                       |
| P2.4  | Log injection                     | 6  | 6  | 6  | 5  | 7  | 30    | Médio      | Nomes de ficheiro com caracteres especiais podem corromper logs. Dificulta análise forense.                           |
| P2.4  | Ausência de atomicidade no upload | 6  | 5  | 4  | 6  | 4  | 25    | Médio      | Falhas na transação deixam estado inconsistente na BD (ficheiro sem metadados ou o contrário).                       |
| DS2.1 | Ausência de TLS na ligação à BD   | 7  | 3  | 3  | 8  | 3  | 24    | Médio      | Intercetação da ligação API → SQL Server expõe dados em trânsito. Mitigado com `Encrypt=True` na connection string.  |

---

#### 2.3 Conclusão do Risk Assessment

A aplicação da metodologia DREAD permitiu identificar e priorizar de forma objetiva as ameaças mais críticas ao sistema de gestão documental. Destacam-se quatro níveis principais de risco:

- **Crítico**:
    - *Flood / DoS por uploads massivos (44)*
    - *IDOR / BOLA via Process ID (43)*

Estas duas ameaças são as mais críticas do RF02. O Flood é trivialmente explorável com credenciais válidas e um script simples, esgotando os recursos do SQL Server. O IDOR compromete o sigilo profissional cliente-advogado e é facilmente automatizável com iteração sequencial de Process IDs — sem verificação de ownership na BD, o impacto é total.

- **Muito Alto**:
    - *Upload de ficheiro malicioso (39)*

Esta ameaça pode escalar para comprometimento total do servidor via XXE no parser .NET de DOCX, ou para distribuição de malware a todos os utilizadores que descarreguem o ficheiro afetado.

- **Alto**:
    - *Credenciais comprometidas / Spoofing (34)*
    - *SQL Injection (32)*
    - *JWT forjado (32)*
    - *Aumento de role via BD (26)*

Ameaças com impacto catastrófico mas probabilidade mitigada pelas tecnologias escolhidas (.NET + Entity Framework + JWT). Mantêm-se em prioridade alta pelo potencial de comprometimento total da base de dados, do sistema de autenticação e do controlo de acessos.

- **Médio**:
    - *Metadados sensíveis em ficheiros (36)*
    - *Log injection (30)*
    - *Ausência de atomicidade no upload (25)*
    - *Ausência de TLS na ligação à BD (24)*

Riscos cuja exploração exige condições específicas ou que afetam principalmente a camada de auditoria e infraestrutura sem comprometer diretamente o conteúdo dos processos jurídicos.

#### 2.4 Priorização de Mitigações

Com base nesta análise, as medidas de mitigação devem ser implementadas pela seguinte ordem de prioridade:

1. **Disponibilidade e proteção contra automação**
    - Rate limiting por utilizador
    - Quotas de tamanho ficheiros por processo
    - Throttling de API para operações de upload

2. **Controlo de acesso e autorização**
    - Verificação de ownership do processo em cada pedido (anti-IDOR)
    - RBAC por role no ASP.NET Core
    - Validação JWT com rejeição do algoritmo `none`
    - Verificação do role do utilizador na BD em cada pedido

3. **Segurança no tratamento de ficheiros**
    - Validação de magic bytes além da extensão
    - Proteção contra zip-bomb e XXE em DOCX
    - Remoção de macros e metadados antes de guardar
    - Antivírus no pipeline de upload

4. **Proteção da base de dados**
    - Queries parametrizadas com Entity Framework
    - Transações atómicas no upload
    - Cifra AES-256-GCM dos documentos em repouso
    - TLS na ligação API - SQL Server
5. **Auditoria e integridade dos logs**
    - Logging estruturado com sanitização de inputs
    - Logs append-only em sistema separado

#### 2.5 Mitigações Associadas às Ameaças Prioritárias

Com base nos resultados do DREAD, foram definidas mitigações específicas alinhadas com as ameaças de maior risco do RF02:

| Ameaça                            | Mitigação                                                                                                                                                                                                |
|-----------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Flood / DoS                       | Rate limiting por utilizador via. Quotas de tamanho máximo de ficheiros por processo. Alertas quando SQL Server atinge 80% de capacidade.          |
| IDOR / BOLA via Process ID        | Verificação na BD em cada pedido de que o utilizador do JWT pertence ao processo solicitado. Registo de todos os acessos negados.                |
| Upload de ficheiro malicioso      | Validação de magic bytes. Verificação do tamanho descomprimido. Remoção de macros via Open XML SDK. Antivírus no pipeline. |
| Credenciais comprometidas         | JWT com expiração curta. Deteção de comportamento anómalo. Rate limiting no login. Bloqueio progressivo após tentativas falhadas.                                                                         |
| SQL Injection                     | Acesso exclusivo via Entity Framework com queries parametrizadas. Conta de BD com menor privilégio (sem DROP/TRUNCATE). SAST no CI/CD.                        |
| JWT forjado                       | `ValidateIssuer`, `ValidateAudience` e `ValidateLifetime` ativos no `TokenValidationParameters`. Allowlist de algoritmos com rejeição do `none`. Chave em local seguro.                               |
| Aumento de role via BD            | Verificar sempre o role do utilizador na BD a partir do JWT. Separar conta de aplicação da conta de administração. SQL Server Audit para acessos diretos fora da API.                                    |
| Ausência de atomicidade           | `TransactionScope` ou `DbTransaction` no Entity Framework para garantir que ficheiro e metadados são criados ou revertidos em conjunto.                                                                   |
| Log injection                     | Serilog para template de logs. Sanitização de `\n`, `\r` e null bytes antes de escrever nos logs. Logs enviados para sistema separado.                                                            |
| Metadados sensíveis em ficheiros  | Remoção de metadados de autor e histórico de revisões de PDF/DOCX via Open XML SDK e iTextSharp antes de cifrar e guardar no SQL Server.                                                                 |
| Ausência de TLS na ligação à BD   | Adicionar `Encrypt=True;TrustServerCertificate=False` à connection string do SQL Server.                                                                                                                 |



<br><br><br>

## 3 RF03 (Organização de Sistema de Ficheiros)

### 3.1 Tabela da análise STRIDE:

| ID   | Elemento                          | Elemento DFD | STRIDE    | Ameaça Identificada                                           |
|------|-----------------------------------|--------------|-----------|---------------------------------------------------------------|
| T3.1 | Criação de Estrutura de Diretórios | Processo     | T / I     | Path Traversal Attack                                         |
| T3.2 | Criação de Estrutura de Diretórios | Processo     | D         | Filesystem Exhaustion (DoS)                                   |
| T3.3 | Sistema de Ficheiros              | Data Store   | T / I     | Acesso Direto ao Filesystem                                   |
| T3.4 | Captura do Evento de Criação      | Processo     | S         | Falsificação de Pedido de Criação                             |
| T3.5 | Criação de Estrutura de Diretórios | Processo     | T         | Race Condition / TOCTOU                                       |
| T3.6 | Base de Dados                     | Data Store   | T / R     | SQL Injection                                                 |
| T3.7 | Advogado                          | Entidade Externa | S / D  | Abuso de Criação Massiva de Processos                         |

---

### 3.2 Tabela de Avaliação

| ID   | Ameaça                                  | D  | R  | E  | A  | D  | Total | Risco      | Justificação                                                                                          |
|------|-----------------------------------------|----|----|----|----|----|----|------------|-------------------------------------------------------------------------------------------------------|
| T3.1 | Path Traversal Attack                   | 10 | 4  | 3  | 10 | 4  | 31 | Alto       | Comprometimento total do servidor se explorado, mas GUID server-side e `Path.Combine()` mitigam.     |
| T3.2 | Filesystem Exhaustion (DoS)             | 8  | 8  | 7  | 9  | 7  | 39 | Muito Alto | Script simples esgota disco rapidamente. Cada processo cria 7+ subdiretórios.                         |
| T3.3 | Acesso Direto ao Filesystem             | 10 | 3  | 2  | 10 | 3  | 28 | Muito Alto | Exposição total de documentos se RCE ocorrer. Ficheiros fora da raiz web reduzem probabilidade.       |
| T3.4 | Falsificação de Pedido de Criação       | 7  | 6  | 6  | 8  | 6  | 33 | Alto       | JWT roubado permite criação em nome de outro utilizador. Quebra de não-repúdio.                       |
| T3.5 | Race Condition / TOCTOU                 | 5  | 3  | 3  | 4  | 4  | 19 | Médio      | Duplicação de estruturas ou inconsistências. Transações do EF mitigam parcialmente.                   |
| T3.6 | SQL Injection                           | 10 | 3  | 3  | 10 | 3  | 29 | Alto       | Perda total de dados se explorado. ORM e GUID server-side tornam exploração improvável.               |
| T3.7 | Abuso de Criação Massiva de Processos   | 8  | 9  | 9  | 10 | 9  | 45 | Crítico    | Credenciais válidas + script trivial. Sem rate limiting. Esgota recursos e polui logs rapidamente.    |

---

### 3.4 Conclusão do Risk Assessment

A aplicação da metodologia DREAD permitiu identificar e priorizar de forma objetiva as ameaças mais críticas ao sistema de organização de ficheiros. Destacam-se três níveis principais de risco:

**Crítico:**
    - *Abuso de Criação Massiva de Processos (45)* — representa a maior ameaça devido à facilidade de execução (script de 5 linhas) e impacto direto na disponibilidade e custos operacionais. Sem rate limiting, qualquer utilizador com credenciais válidas pode esgotar recursos.

**Muito Alto:**
    - *Filesystem Exhaustion / DoS (39)*
    - *Acesso Direto ao Filesystem (28, mas com Damage=10)*

Estes cenários comprometem a disponibilidade ou confidencialidade total do sistema. O Filesystem Exhaustion é altamente reprodutível, enquanto o Acesso Direto tem impacto catastrófico apesar de baixa probabilidade.

**Alto:**
    - *Falsificação de Pedido (33)*
    - *Path Traversal (31)*
    - *SQL Injection (29)*

Ameaças que comprometem integridade, autenticação ou podem escalar para comprometimento total. Path Traversal e SQL Injection já têm mitigações robustas (GUID server-side, ORM), mas o impacto potencial mantém-nas em prioridade alta.

**Médio:**
    - *Race Condition / TOCTOU (19)* — baixa probabilidade e impacto limitado. Transações do Entity Framework já mitigam parcialmente.

---

### 3.5 Priorização de Mitigações

Com base nesta análise, as medidas de mitigação devem ser implementadas pela seguinte ordem de prioridade:

**Proteção contra Abuso e Disponibilidade**
- Rate limiting (10 criações/hora por utilizador)
- Sliding window counter para prevenir bypass
- Anomaly detection (alertas para padrões suspeitos)

**Defense-in-Depth para Acesso a Ficheiros**
- Cifra de ficheiros em repouso (AES-256-GCM)
- Key Vault para gestão de chaves
- Permissões de filesystem restritas (chmod 700)

**Gestão de Recursos e Quotas**
- Quotas de disco por utilizador (500GB)
- Monitoring de espaço disponível (alertas >80%)
- Cleanup automático de processos antigos (>90 dias)

**Validação e Integridade**
- Code review focado em file operations
- Testes de penetração para path traversal
- SAST no pipeline CI/CD para SQL injection
- Unit tests para validações de path

**Autenticação e Sessões**
- Refresh tokens com rotação
- Expiração JWT curta (15 minutos)
- Anti-replay nonce

**Atomicidade e Consistência**
- Transações explícitas para criação de processos
- Rollback automático em caso de erro
- Integrity checks pós-criação

---

### 3.6 Mitigações Associadas às Ameaças Prioritárias

Com base nos resultados do DREAD, foram definidas mitigações específicas alinhadas com as ameaças de maior risco do RF03:

| Ameaça                                | Mitigação                                                                                                                                        |
|---------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| Abuso de Criação Massiva              | Rate limiting (10/hora), sliding window counter, anomaly detection com bloqueio automático, CAPTCHA após threshold                               |
| Filesystem Exhaustion (DoS)           | Quotas de disco por utilizador (500GB), monitoring de espaço (alerta >80%), cleanup automático (processos >90 dias), throttling de API           |
| Acesso Direto ao Filesystem           | Cifra AES-256-GCM em repouso, Key Vault para chaves, ficheiros fora da raiz web (`/var/data/processos/`), permissões filesystem (chmod 700) |
| Falsificação de Pedido                | Refresh tokens com rotação, expiração JWT curta (15 min), anti-replay nonce, validação rigorosa de claims (userId, role, issuer)                 |
| Path Traversal                        | `Path.Combine()` + `Path.GetFullPath()`, GUID gerado server-side (não aceita input), validação que path final está dentro do diretório base      |
| SQL Injection                         | ORM (Entity Framework) com queries parametrizadas, GUID server-side elimina input malicioso, SAST automático no CI/CD, least privilege na BD     |
| Race Condition / TOCTOU               | Transações explícitas com locks pessimistas, rollback automático em erro, verification pós-criação (integrity check)                              |




<br><br><br>

## 4 RF04 (Auditoria e Logging)

#### 4.1 Tabela da análise stride:

| ID   | Elemento                  | Elemento DFD     | STRIDE    | Ameaça Identificada                                         |
|------|---------------------------|------------------|-----------|-------------------------------------------------------------|
| T4.1 | Captura de Evento         | Processo         | S / T / R | Interceção ou forja de eventos antes do registo             |
| T4.2 | Tratamento do Evento      | Processo         | T / R / I | Injeção de caracteres maliciosos ou fuga de dados sensíveis |
| T4.3 | Persistência (PostgreSQL) | Data Store       | T / R / I | Modificação ou eliminação de registos de auditoria          |
| T4.4 | Atores do Sistema         | Entidade Externa | S / D / T | Falsificação de Identidade e sobrecarga do Sistema          |

#### 4.2 Tabela de Avaliação

Para maior visibilidade, usei o DREAD com as métricas de 1 a 5.

#### 4.2 Tabela de Avaliação (Escala 1–10)

| ID   | Ameaça                                | D  | R  | E  | A  | D  | Total | Risco      | Justificação                                                      |
|------|---------------------------------------|----|----|----|----|----|-------|------------|-------------------------------------------------------------------|
| T4.1 | Interceção de Fluxo                   | 6  | 6  | 6  | 8  | 6  | 32    | Médio      | Mitigado por TLS, mas ainda relevante em redes internas           |
| T4.2 | Log Injection                         | 8  | 6  | 6  | 4  | 8  | 32    | Alto       | Compromete a integridade da auditoria e dificulta análise forense |
| T4.3 | Manipulação da DB de Logs             | 10 | 4  | 4  | 10 | 4  | 32    | Alto       | Impacto crítico na persistência; pode eliminar rastos de ataques  |
| T4.4 | Falsificação de Identidade (Spoofing) | 8  | 6  | 6  | 8  | 6  | 34    | Alto       | Permite ações maliciosas em nome de utilizadores legítimos        |
| T4.4 | Abuso de Funcionalidades              | 8  | 8  | 8  | 10 | 8  | 42    | Muito Alto | Exploração de permissões legítimas não restringidas               |
| T4.4 | Denial of Service (DoS / Flood)       | 10 | 10 | 8  | 10 | 10 | 48    | Crítico    | Alta automatização; pode indisponibilizar o sistema               |
| T4.4 | Brute Force / Automation              | 6  | 10 | 8  | 10 | 8  | 42    | Alto       | Ataques contínuos devido à facilidade de automatização            |
| T4.4 | Escalada via Conta Comprometida       | 10 | 4  | 4  | 10 | 4  | 32    | Muito Alto | Impacto máximo se contas privilegiadas forem comprometidas        |
---

#### 4.3 Conclusão do Risk Assessment

A aplicação da metodologia DREAD permitiu identificar e priorizar de forma objetiva as ameaças mais críticas ao sistema. Destacam-se três
níveis principais de risco:

- **Crítico**:
    - *DoS / Flood (48)* — representa a maior ameaça devido à sua facilidade de execução e impacto direto na disponibilidade do sistema.

- **Muito Alto**:
    - *Abuso de Funcionalidades (42)*
    - *Escalada via Conta Comprometida (32, mas com impacto elevado)*  
      Estes cenários exploram falhas no controlo de acesso e no modelo de permissões, podendo comprometer totalmente a integridade do
      sistema.

- **Alto**:
    - *Spoofing*, *Log Injection*, *Manipulação de Logs*, *Brute Force*  
      Estas ameaças afetam sobretudo a integridade, autenticação e auditabilidade do sistema.

- **Médio**:
    - *Interceção de Fluxo* — mitigada parcialmente por TLS, mas ainda relevante em cenários específicos.

#### 4.4 Priorização de Mitigações

Com base nesta análise, as medidas de mitigação devem ser implementadas pela seguinte ordem de prioridade:

1. **Disponibilidade e proteção contra automação**
    - Rate limiting
    - Proteção contra DoS (e.g., throttling, WAF)

2. **Controlo de acesso e autenticação**
    - MFA (Multi-Factor Authentication)
    - Gestão de sessões segura
    - Princípio do menor privilégio

3. **Integridade e auditoria**
    - Sanitização de inputs (prevenção de Log Injection)
    - Proteção e imutabilidade dos logs

4. **Monitorização e deteção**
    - Alertas de comportamento anómalo
    - Sistemas de deteção de intrusão

#### 4.5 Mitigações Associadas às Ameaças Prioritárias

Com base nos resultados do DREAD, foram definidas mitigações específicas alinhadas com as ameaças de maior risco:

| Ameaça                          | Mitigação                                                                   |
|---------------------------------|-----------------------------------------------------------------------------|
| DoS / Flood                     | Rate Limiting, API Gateway throttling, Web Application Firewall (WAF)       |
| Abuso de Funcionalidades        | RBAC (Role-Based Access Control), validação de regras de negócio no backend |
| Escalada via Conta Comprometida | MFA, gestão segura de sessões, rotação de tokens                            |
| Brute Force                     | Rate limiting por IP, CAPTCHA, lockout progressivo                          |
| Spoofing                        | Autenticação forte, tokens assinados (JWT com assinatura segura)            |
| Log Injection                   | Sanitização de inputs, encoding de logs                                     |
| Manipulação de Logs             | Logs imutáveis (append-only), hashing ou storage seguro (ex: WORM storage)  |
| Interceção de Fluxo             | TLS 1.3, HSTS, validação de certificados                                    |
