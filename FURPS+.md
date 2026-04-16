# FURPS+ - Functional Requirement


## RF02 - Gestão Documental 

## Descrição
- O sistema deve permitir o *upload, download*, edição e listagem de documentos relacionados com um processo jurídico em que o utilizador, autenticado previamente, esteja envolvido.
---

## Atores
- Utilizador autenticado:
    - **Advogado** pode executar todas as ações mencionadas.
    - **Assistente Legal** pode executar todas as tarefas que não alterem os dados.
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
- Assistente Legal deve ter acesso apenas aos dados dos clientes do Advogado a que está atríbuído

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
    - **Assistente Legal** pode aceder aos diretórios dos processos associados.
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
