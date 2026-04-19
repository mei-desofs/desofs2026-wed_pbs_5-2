# Risk Assessment

- Quantified risk scoring (DREAD/CVSS) used to justify mitigation priority for identified threats.

Neste Projeto, o sistema de pontuação escolhido foi o DREAD.
O DREAD é um sistema de pontuação utilizado para avaliar e priorizar riscos de segurança, onde é atribuido uma nota (geralmente de 1 a 10) a cinco categorias para cada ameaça que é encontrada no sistema.

- D - Damage Potential (Dano): Se isto acontecer, o quão grave é? (Ex: Roubar todos os processos jurídicos = 10; Mudar a cor de um botão = 1) .

- R - Reproducibility (Reprodutibilidade): É fácil fazer isto acontecer outra vez? (Ex: Acontece sempre que carrego num link = 10; Só acontece se o servidor falhar ao mesmo tempo que chove = 1) .

- E - Exploitability (Explorabilidade): É preciso ser um génio para atacar? (Ex: Qualquer pessoa com um browser consegue = 10; Preciso de meses de planeamento e ferramentas caras = 1) .

- A - Affected Users (Utilizadores Afetados): Quantas pessoas sofrem com isto? (Ex: Todos os advogados e clientes = 10; Apenas um utilizador específico = 1) .

- D - Discoverability (Descoberta): É fácil encontrar esta falha? (Ex: Está à vista de todos na página inicial = 10; Está escondida em código que ninguém vê = 1) .

A nota final é o resultado da média das 5 categorias!

## 4 RF04 (Auditoria e Logging)

#### 4.1 Tabela da análise stride:

 
| ID | Elemento | Elemento DFD | STRIDE | Ameaça Identificada |
|----|----------|--------------|--------|---------------------|
| T4.1 | Captura de Evento | Processo | S / T / R | Interceção ou forja de eventos antes do registo |
| T4.2 | Tratamento do Evento | Processo | T / R / I | Injeção de caracteres maliciosos ou fuga de dados sensíveis |
| T4.3 | Persistência (PostgreSQL) | Data Store | T / R / I | Modificação ou eliminação de registos de auditoria |
| T4.4 | Atores do Sistema | Entidade Externa | S / D / T | Falsificação de Identidade e sobrecarga do Sistema |


#### 4.2 Tabela de Avaliação

Para maior visibilidade, usei o DREAD com as métricas de 1 a 5.

| ID   | Ameaça                                   | D | R | E | A | D | Total | Risco       | Justificação |
|------|------------------------------------------|---|---|---|---|---|-------|-------------|-------------|
| T4.1 | Interceção de Fluxo                      | 3 | 3 | 3 | 4 | 3 | 16    | Médio       | Mitigado por TLS, mas ainda relevante em redes internas |
| T4.2 | Log Injection                            | 4 | 3 | 3 | 2 | 4 | 16    | Alto        | Compromete a integridade da auditoria e dificulta análise forense |
| T4.3 | Manipulação da DB de Logs                | 5 | 2 | 2 | 5 | 2 | 16    | Alto        | Impacto crítico na persistência; pode eliminar rastos de ataques |
| T4.4 | Falsificação de Identidade (Spoofing)    | 4 | 3 | 3 | 4 | 3 | 17    | Alto        | Permite ações maliciosas em nome de utilizadores legítimos |
| T4.4 | Abuso de Funcionalidades                 | 4 | 4 | 4 | 5 | 4 | 21    | Muito Alto  | Exploração de permissões legítimas não restringidas |
| T4.4 | Denial of Service (DoS / Flood)          | 5 | 5 | 4 | 5 | 5 | 24    | Crítico     | Alta automatização; pode indisponibilizar o sistema |
| T4.4 | Brute Force / Automation                 | 3 | 5 | 4 | 5 | 4 | 21    | Alto        | Ataques contínuos devido à facilidade de automatização |
| T4.4 | Escalada via Conta Comprometida          | 5 | 2 | 2 | 5 | 2 | 16    | Muito Alto  | Impacto máximo se contas privilegiadas forem comprometidas |

---

#### 4.3 Conclusão do Risk Assessment

A aplicação da metodologia DREAD permitiu identificar e priorizar de forma objetiva as ameaças mais críticas ao sistema. Destacam-se três níveis principais de risco:

- **Crítico**:  
  - *DoS / Flood (24)* — representa a maior ameaça devido à sua facilidade de execução e impacto direto na disponibilidade do sistema.

- **Muito Alto**:  
  - *Abuso de Funcionalidades (21)*  
  - *Escalada via Conta Comprometida (16, mas com impacto elevado)*  
  Estes cenários exploram falhas no controlo de acesso e no modelo de permissões, podendo comprometer totalmente a integridade do sistema.

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

| Ameaça | Mitigação |
|--------|----------|
| DoS / Flood | Rate Limiting, API Gateway throttling, Web Application Firewall (WAF) |
| Abuso de Funcionalidades | RBAC (Role-Based Access Control), validação de regras de negócio no backend |
| Escalada via Conta Comprometida | MFA, gestão segura de sessões, rotação de tokens |
| Brute Force | Rate limiting por IP, CAPTCHA, lockout progressivo |
| Spoofing | Autenticação forte, tokens assinados (JWT com assinatura segura) |
| Log Injection | Sanitização de inputs, encoding de logs |
| Manipulação de Logs | Logs imutáveis (append-only), hashing ou storage seguro (ex: WORM storage) |
| Interceção de Fluxo | TLS 1.3, HSTS, validação de certificados |