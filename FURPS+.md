# FURPS+ - Functional Requirement


## RF03 - Gestão Documental 

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