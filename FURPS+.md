# FURPS+ - Functional Requirement

## RF01 - Gestão de Perfis de Utilizador (RBAC)

## Descrição
- O sistema deve permitir a autenticação segura de utilizadores e garantir um controlo de acessos rigoroso, baseado em roles (Role-Based Access Control - RBAC).
- A aplicação deve assegurar que cada perfil acede estritamente aos recursos e ações que lhe são permitidos, garantindo o isolamento da informação.

---

## Atores
- Utilizador não autenticado:
  - Pode efetuar a tentativa de autenticação (*login*).
- Utilizador autenticado:
  - **Advogado:** Possui permissões elevadas para gerir clientes, criar processos jurídicos e gerir a totalidade dos documentos associados aos seus casos.
  - **Assistente Jurídico:** Possui permissões intermédias, podendo aceder aos processos e documentos dos advogados a que está alocado, para efeitos de consulta e organização de processo (sem permissões destrutivas).
  - **Cliente:** Possui o nível de acesso mais restrito, podendo apenas consultar e descarregar documentos dos processos em que é explicitamente o cliente titular.

---

## *Inputs*
- **Credenciais de acesso**: *Email* e *Password* submetidos no *endpoint* de autenticação.
- **Token de Sessão**: *Token* (JWT) enviado através do cabeçalho (*Header* de *Authorization*) dos pedidos HTTP subsequentes.

---

## *Outputs*
- **Autenticação com sucesso**: Emissão e devolução de um *Token* de Autenticação válido contendo as *claims* (role e ID do utilizador).
- **Autenticação falhada**: Mensagem genérica de erro informando que as credenciais são inválidas.
- **Autorização negada**: Resposta HTTP de erro do tipo *403 Forbidden* (se não tiver permissões para a ação) ou *401 Unauthorized* (se o token for inválido/expirado).

---

## Pré-condições
- O utilizador deve estar previamente registado e ativo na base de dados relacional do sistema.
- A base de dados e a Web API devem estar operacionais e conectadas.

---

## Pós-condições
- O sistema estabelece uma sessão segura e identificada, permitindo ao utilizador invocar as ações permitidas para o seu *role*.
- O evento de *login* (com sucesso ou falhado) é registado nos *logs* do sistema.

---

## Regras de negócio
- O acesso do sistema obedece estritamente ao princípio do privilégio mínimo (*Principle of Least Privilege*).
- O sistema não deve expor se a falha no *login* foi devido ao *email* inexistente ou à *password* incorreta (para evitar enumeração de contas).
- Em caso de múltiplas tentativas de autenticação falhadas consecutivas, a conta do utilizador deve ser bloqueada temporariamente.

---

## Non-Functional Requirements (FURPS+)

### Segurança (6 Security Pillars)

| Pilar | Requisito de Segurança e Justificação | Referência ASVS |
| :--- | :--- | :--- |
| **1. Auth (Autenticação)** | **Hashing de Passwords:** As passwords devem ser protegidas com **Argon2id** ou **BCrypt**. *Justificação: Prevenir a exposição de credenciais em caso de fuga da base de dados.* | V2.4.1 |
| **2. Access (Acesso)** | **Enforcement no Back-end:** O controlo de acesso (RBAC) deve ser verificado em cada pedido à API, independentemente do front-end. *Justificação: Mitigar Bypass de Autorização via manipulação de pedidos.* | V4.1.1 |
| **3. Data (Dados)** | **Proteção de Tokens:** O segredo (Secret Key) do JWT deve ser armazenado num cofre de chaves. *Justificação: Garantir a integridade dos tokens e evitar a falsificação de identidades.* | V6.2.1 |
| **4. Input (Entrada)** | **Sanitização de Credenciais:** Limitação de caracteres e sanitização de campos de login. *Justificação: Prevenir ataques de SQL/NoSQL Injection no processo de autenticação.* | V5.1.1 |
| **5. 3rd Party (Terceiros)** | **Verificação de Vulnerabilidades (SCA):** Todas as bibliotecas NuGet de autenticação (ex: `System.IdentityModel.Tokens.Jwt`) devem ser monitorizadas via **GitHub Dependabot** ou `dotnet list package --vulnerable`. *Justificação: Garantir que a lógica de segurança não depende de componentes com vulnerabilidades conhecidas (CVEs).* | V14.2.1 |
| **6. Logging (Registos)** | **Auditoria de Eventos de Acesso:** Devem ser registados sucessos e falhas de login, incluindo IP e timestamp. *Justificação: Permitir deteção de ataques de força bruta e suporte a análise forense.* | V7.1.1 |

### Suportabilidade
- Integração com o sistema de auditoria para manter *logs* detalhados de quem fez login, de onde, e a que horas.

---

## Tópicos adicionais
- Integração obrigatória com comunicação cifrada em trânsito (HTTPS).
- As definições de RBAC aqui parametrizadas são a base funcional para o controlo de acessos transversal, refletindo-se diretamente na "Organização de Sistema de Ficheiros" (RF03) e "Gestão Documental" (RF02).

## 6 Pilares de Segurança


## RF02 - Gestão Documental 

## Descrição
- O sistema deve permitir o *upload, download*, edição e listagem de documentos relacionados com um processo jurídico em que o utilizador, autenticado previamente, esteja envolvido.
---

## Atores
- Utilizador autenticado:
    - **Advogado** pode executar todas as ações mencionadas.
    - **Assistente Jurídico** pode executar todas as tarefas que não alterem os dados.
    - **Cliente** pode consultar e descarregar todos os documentos dos casos que seja o cliente.

---

## *Inputs*
- ***Process ID***: Identificador do processo.
- **Ficheiro existente pretendido**: Documento que vai ser consultado/descarregado.
- **Novo ficheiro**: Novo documento que irá ser carregado para o sistema.

---

## *Outputs*
- **Upload**: Confirmação de sucesso/erro  
- **Download** e/ou consulta: Ficheiro solicitado  
- **Listagem**: Lista de documentos associados ao processo

---

## Pré-condições
- O processo deve existir e o ator deve estar envolvido no mesmo 
    - Ex.: Advogado deve apenas ter acesso aos ficheiros dos seus casos.
- O utilizador deve estar autenticado e tem de existir no sistema.

---

## Pós-condições
- O documento fica associado ao processo após *upload*.  
- O documento pode ser recuperado *posteriormente*.
- Consultas, *dowloads, uploads* e edições devem ficar registadas no formato de *Logs*.  

---

## Regras de negócio
- Apenas ficheiros PDF e DOCX são permitidos.  
- Nome do ficheiro deve ser único por processo. 
- Processos podem ter um ou mais Advogados ou Assistentes Jurídicos. 
- Processos apenas podem ter um cliente.
---

## Non-Functional Requirements (FURPS+)

### Segurança
- Validação de tipo de ficheiro  
- Todos os documentos armazenados no servidor devem ser cifrados para garantir a confidencialidade.
- Um cliente só deve ter permissão para visualizar processos e documentos que lhe pertencem
- Advogado deve ter acesso apenas aos dados dos seus clientes
- Assistente Jurídico deve ter acesso apenas aos dados dos clientes do Advogado a que está atríbuído

### Suportabilidade
- Logs de operações  

---

## Tópicos adicionais
- Aplicação utiliza uma base de dados relacional externa e não é permitido alojamento local de dados.


<br><br><br><br>


## RF03 - Organização de Sistema de Ficheiros 

## Descrição
- O sistema deve criar e gerir automaticamente uma estrutura de diretórios única para cada processo jurídico.
- Cada processo é identificado por um *GUID*, garantindo isolamento, segurança e rastreabilidade dos dados.
- Os ficheiros são armazenados fora da raiz web e apenas acedidos via *stream* controlado.

---

## Atores
- Utilizador autenticado:
    - **Advogado** tem controlo total sobre os diretórios dos seus processos.
    - **Assistente Jurídico** pode aceder aos diretórios dos processos associados.
    - **Cliente** pode apenas consultar ficheiros dos seus processos.

---

## *Inputs*
- ***Criação de processo***: Pedido via API para criação de novo processo.

---

## *Outputs*
- **Process ID (GUID)**: Identificador único do processo  
- **Criação de diretórios**: Estrutura de ficheiros associada ao processo  
- **Log**: Registo da criação no sistema de auditoria  

---

## Pré-condições
- O utilizador deve estar autenticado.  
- O pedido de criação de processo deve ser válido.  

---

## Pós-condições
- É criada uma estrutura de diretórios única para o processo.  
- O identificador (*GUID*) é devolvido ao cliente.  
- A operação é registada em logs de auditoria.  

---

## Regras de negócio
- Cada processo tem um diretório único identificado por GUID.  
- Os diretórios não são acessíveis diretamente via HTTP.  
- Os paths são sempre gerados internamente (nunca com input do utilizador).  
- A estrutura deve ser criada de forma atómica (com rollback em caso de erro).  

---

## Non-Functional Requirements (FURPS+)

### Segurança
- Proteção contra *path traversal* usando `Path.Combine()` e `GetFullPath()`.  
- Ficheiros armazenados fora da raiz web.  
- Validação de ficheiros (extensão e *magic bytes*).  
- Limite de tamanho de ficheiros: 20 MB.  
- Headers seguros no download (*Content-Disposition* sanitizado).  
- Controlo de acessos baseado em RBAC.  

### Suportabilidade
- Logs de criação de diretórios e acessos.  
- Integração com sistema de auditoria (RF04).  

### Desempenho
- Operações de ficheiros realizadas de forma eficiente e segura.  

---

## Tópicos adicionais
- Estrutura de diretórios:

/processos/ <br>
└── {processId-GUID}/<br>
├── documentos/<br>
│ ├── peticoes/<br>
│ ├── contratos/<br>
│ └── outros/<br>
├── correspondencia/<br>
└── temp/


- Ficheiros cifrados com AES-256-GCM antes de armazenamento.  
- Chaves geridas via Azure Key Vault.  
- Permissões de sistema de ficheiros restritas ao backend.  
- Integração com:
    - **RF01 (RBAC)** – controlo de acessos  
    - **RF02 (Gestão Documental)** – operações sobre ficheiros  
    - **RF04 (Auditoria)** – registo de operações  


<br><br><br><br>

## RF04 - Auditoria de Acessos e Logging

## Descrição
- O sistema deve registar de forma persistente, cronológica e imutável todas as ações críticas realizadas no back-end para garantir a segurança do sistema.  
- O objetivo é assegurar a rastreabilidade total das interações com dados sensíveis, incluindo acessos a documentos, operações sobre processos jurídicos e ações no sistema de ficheiros.

---

## Atores
- Utilizador autenticado:
  - **Advogado:** Pode consultar logs associados aos seus processos e ações sob sua responsabilidade.
  - **Assistente Jurídico:** Pode consultar logs dos processos a que está associado, sem acesso a eventos administrativos ou globais.
  - **Cliente:** Não possui acesso direto aos logs (apenas rastreabilidade indireta via sistema).
- **Sistema (Automático):** Responsável pela geração autónoma dos registos durante a execução das operações.

---

## *Inputs*
- **Metadados de Sessão:**
  - Identificador do utilizador
  - Role (Advogado, Assistente Jurídico, Cliente)
  - Endereço IP

- **Contexto da Operação:**
  - Identificador do processo (*GUID*)
  - Tipo de operação (CRUD, autenticação, acesso a ficheiros, etc.)
  - Recursos afetados (documentos, diretórios, processos)

- **Dados de Execução:**
  - Resultado da operação (sucesso/erro)
  - Código de estado HTTP

---

## *Outputs*
- **Registos de Auditoria:**
  - Persistência em base de dados relacional (obrigatório)
  - Histórico completo e ordenado cronologicamente

- **Logs de Segurança:**
  - Tentativas de acesso não autorizado
  - Falhas de autenticação
  - Violações de regras de negócio

---

## Pré-condições
- O utilizador deve estar autenticado (quando aplicável).
- A base de dados relacional deve estar disponível e operacional.
- O sistema de logging deve estar ativo e integrado com a API.

---

## Pós-condições
- Cada ação relevante é registada antes da resposta ao cliente.
- O log contém informação suficiente para auditoria e análise forense.
- Os registos ficam disponíveis para consulta conforme permissões RBAC.

---

## Regras de Negócio
- **Imutabilidade:** Logs não podem ser alterados nem eliminados.
- **Confidencialidade:** Dados sensíveis não devem ser armazenados em texto plano.
- **Privilégio Mínimo:**  
  - Advogado → logs dos seus processos  
  - Assistente Jurídico → logs dos processos atribuídos  
  - Cliente → sem acesso direto  
- **Rastreabilidade Total:** Todas as ações de RF01, RF02 e RF03 devem gerar logs.

---

## Non-Functional Requirements (FURPS+)

### Segurança (6 Security Pillars)

| Pilar            | Requisito de Segurança e Justificação                                                                 | Referência ASVS |
|------------------|------------------------------------------------------------------------------------------------------|------------------|
| **1. Autenticação** | Cada log deve estar associado a um utilizador autenticado (não-repúdio).     | V7.1.1           |
| **2. Acesso**        | RBAC aplicado aos logs conforme RF01 (restrição por role e contexto de processo).                | V7.1.2           |
| **3. Dados**         | Persistência em base de dados relacional com controlo de integridade e permissões restritas.     | V7.3.1           |
| **4. Entrada**       | Sanitização de inputs antes de registo para prevenir log injection.                              | V5.3.4           |
| **5. Terceiros**     | Monitorização de bibliotecas de logging e dependências externas.                                 | V14.2.1          |
| **6. Registos**      | Uso de timestamps em UTC e correlação de eventos entre serviços.                                 | V7.1.3           |

### Suportabilidade
- Integração com ferramentas de monitorização e SIEM.
- Capacidade de análise forense e correlação de eventos entre RF01, RF02 e RF03.
- Registo de falhas do sistema operativo (ex: erros na criação de ficheiros ou diretórios).

---

## Tópicos adicionais
- Integração direta com:
  - **RF01 (RBAC)** – controlo de acesso aos logs  
  - **RF02 (Gestão Documental)** – auditoria de operações sobre documentos  
  - **RF03 (Sistema de Ficheiros)** – auditoria de operações no storage  