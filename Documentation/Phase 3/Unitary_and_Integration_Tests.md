# Documentação da Suite de Testes — LawyerApp

## Índice

- [Visão Geral](#visão-geral)
- [Estrutura do Projecto](#estrutura-do-projecto)
- [Infraestrutura de Testes](#infraestrutura-de-testes)
  - [CustomWebApplicationFactory](#customwebapplicationfactory)
  - [InMemoryDbContextFactory](#inmemorydbcontextfactory)
- [Testes Unitários](#testes-unitários)
  - [Domínio](#domínio)
  - [Segurança](#segurança-unitária)
  - [Serviços](#serviços)
  - [Utilitários Partilhados](#utilitários-partilhados)
- [Testes de Integração](#testes-de-integração)
  - [Repositórios](#repositórios)
  - [API](#api)
  - [Segurança](#segurança-integração)

---

## Número de Testes executados

| Métrica | Valor |
|---|---|
| Total de testes | 127 |
| Testes unitários | 94 |
| Testes de integração | 33 |
| Taxa de aprovação | 100% |

---

## Estrutura do Projecto

```
LawyerApp.Tests/
├── Helpers/
│   ├── CustomWebApplicationFactory.cs
│   └── InMemoryDbContextFactory.cs
├── Unit/
│   ├── Domain/
│   │   ├── ClientTests.cs
│   │   └── DomainExtendedTests.cs
│   ├── Security/
│   │   ├── BCryptPasswordHasherTests.cs
│   │   └── JwtProviderTests.cs
│   ├── Services/
│   │   ├── ClientServiceTests.cs
│   │   └── LoginServiceTests.cs
│   └── Shared/
│       └── ResultTests.cs
└── Integration/
    ├── Repositories/
    │   ├── UserRepositoryTests.cs
    │   ├── DocumentRepositoryTests.cs
    │   └── LegalProcessRepositoryTests.cs
    ├── API/
    │   ├── ClientControllerTests.cs
    │   └── LoginControllerTests.cs
    └── Security/
        └── AuthorizationSecurityTests.cs
```

---

## Testes Unitários

### Domínio

#### ClientTests

`Unit/Domain/ClientTests.cs` — **7 testes** — Classe testada: `Client`, `LegalProcess`, `Document`

Verifica o comportamento de construção das entidades de domínio principais.

| Teste | Descrição |
|---|---|
| `Client_Constructor_SetsAllProperties` | Os cinco parâmetros do construtor são correctamente atribuídos |
| `Client_CreatedAt_IsSetToUtcNow` | `CreatedAt` é definido para a hora UTC actual |
| `Client_IsSubclassOfUser` | `Client` herda de `User` |
| `LegalProcess_Constructor_GeneratesUniqueGuids` | Duas instâncias produzem `ProcessId` distintos |
| `LegalProcess_InitialStatus_IsOpen` | Estado inicial é sempre `ProcessStatus.Open` |
| `Document_Constructor_GeneratesUniqueStoredFileName` | Dois documentos com o mesmo nome original geram `StoredFileName` diferentes |
| `Document_StoredFileName_PreservesFileExtension` | A extensão do ficheiro original é preservada no `StoredFileName` |

#### DomainExtendedTests

`Unit/Domain/DomainExtendedTests.cs` — **21 testes** — Classes testadas: `Lawyer`, `LegalAssistant`, `Client`, `LegalProcess`, `Document`

Complementa os testes de domínio cobrindo as restantes entidades e comportamentos.

**`LawyerTests`** — Construção, papel (`Roles.Lawyer`), herança de `User`, timestamp `CreatedAt`.

**`LegalAssistantTests`** — Construção, papel (`Roles.LegalAssistant`), herança de `User`.

**`ClientRoleTests`** — Confirma que o papel atribuído é `Roles.Client`.

**`LegalProcessExtendedTests`** — `OpenedAt` em UTC, atribuição de `LawyerId`/`ClientId`, título, descrição, `ProcessId` não vazio, transição de estado para `InAnalysis` e `Closed`.

**`DocumentExtendedTests`** — Nome, tamanho, tipo de conteúdo, categoria, `LegalProcessId`, `UploadedAt` em UTC, `StoredFileName` não vazio com segmento GUID.

---

### Testes Unitários de Segurança

#### BCryptPasswordHasherTests

`Unit/Security/BCryptPasswordHasherTests.cs` — **7 testes** — Classe testada: `BCryptPasswordHasher`

Testa directamente a implementação de hashing sem mocks — a biblioteca `BCrypt.Net` é exercitada na íntegra.

| Teste | Descrição |
|---|---|
| `HashPassword_ReturnsNonEmptyHash` | O resultado não é nulo nem vazio |
| `HashPassword_OutputDiffersFromInput` | O hash é diferente da palavra-passe original |
| `HashPassword_TwoCallsProduceDifferentHashes` | O sal aleatório garante hashes distintos para a mesma entrada |
| `VerifyPassword_WithCorrectPassword_ReturnsTrue` | Verificação bem-sucedida com a palavra-passe correcta |
| `VerifyPassword_WithWrongPassword_ReturnsFalse` | Rejeição com palavra-passe errada |
| `VerifyPassword_WithEmptyPassword_ReturnsFalse` | String vazia não é aceite |
| `HashPassword_ProducesBCryptFormatHash` | O resultado corresponde ao formato `$2[ab]$` |

#### JwtProviderTests

`Unit/Security/JwtProviderTests.cs` — **11 testes**

Dado que `JwtProvider` é `internal sealed`, os tokens são gerados directamente com o mesmo algoritmo e chave configurados em `appsettings.json`, verificando o contrato do token sem depender do pipeline HTTP.

| Teste | Descrição |
|---|---|
| `Generate_ReturnsNonEmptyToken` | O token não é vazio |
| `Generate_ReturnsWellFormedJwt` | O token tem exactamente três segmentos Base64Url |
| `Generate_TokenContainsSubjectClaim` | O `sub` corresponde ao `Id` do utilizador |
| `Generate_TokenContainsEmailClaim` | O claim de email está presente e correcto |
| `Generate_TokenContainsRoleClaim` | O claim de papel (`ClaimTypes.Role`) está presente |
| `Generate_ClientRole_IsSetToClient` | Utilizadores do tipo `Client` recebem o papel `"Client"` |
| `Generate_LawyerRole_IsSetToLawyer` | Utilizadores do tipo `Lawyer` recebem o papel `"Lawyer"` |
| `Generate_TokenIsNotExpiredImmediately` | `ValidTo` é no futuro |
| `Generate_TokenExpiresInApproximately30Minutes` | Expiração entre 25 e 35 minutos |
| `Generate_TokenPayloadDoesNotContainPassword` | O payload não contém `"password"` nem `"passwordHash"` |
| `Generate_TwoDifferentUsers_ProduceDifferentTokens` | Dois utilizadores distintos geram tokens diferentes |

---

### Serviços

#### ClientServiceTests

`Unit/Services/ClientServiceTests.cs` — **7 testes** — Classe testada: `ClientService`

As dependências `IUserRepository` e `IPasswordHasher` são substituídas por mocks (Moq).

| Teste | Descrição |
|---|---|
| `CreateClientAsync_WhenEmailIsNew_ReturnsClientDto` | Criação bem-sucedida devolve `ClientDto` com os dados correctos |
| `CreateClientAsync_WhenEmailAlreadyExists_ReturnsFailure` | Email duplicado devolve `Result` de falha com mensagem de erro |
| `CreateClientAsync_HashesPasswordBeforePersisting` | O repositório recebe o DTO com a palavra-passe já em hash |
| `CreateClientAsync_NeverStoresPlainTextPassword` | O repositório nunca é chamado com a palavra-passe em texto claro |
| `GetAllClientsAsync_ReturnsMappedDtos` | Devolve lista correctamente mapeada para `ClientDto` |
| `GetAllClientsAsync_WhenNoClients_ReturnsEmptyList` | Lista vazia devolvida quando não existem clientes |
| `GetAllClientsAsync_DoesNotExposePasswordHash` | `ClientDto` não possui propriedade `PasswordHash` |

#### LoginServiceTests

`Unit/Services/LoginServiceTests.cs` — **7 testes** — Classe testada: `LoginService`

As dependências `IUserRepository`, `IPasswordHasher`, `IJwtProvider` e `IClient` são substituídas por mocks.

| Teste | Descrição |
|---|---|
| `Login_WhenUserDoesNotExist_ReturnsFailureWith401` | Utilizador inexistente devolve falha com código 401 |
| `Login_WhenPasswordIsWrong_ReturnsFailureWith401` | Palavra-passe errada devolve falha com código 401 |
| `Login_WithValidCredentials_ReturnsSuccessWithToken` | Credenciais correctas devolvem o token JWT |
| `Login_WithValidCredentials_ReturnsUserRole` | O papel do utilizador é incluído na resposta |
| `Login_NeverCallsJwtProvider_WhenUserNotFound` | `IJwtProvider.Generate` nunca é invocado quando o utilizador não existe |
| `Login_NeverCallsJwtProvider_WhenPasswordIsWrong` | `IJwtProvider.Generate` nunca é invocado com palavra-passe errada |
| `Login_WithLawyerAccount_ReturnsLawyerRole` | Contas de advogado devolvem o papel `"Lawyer"` |

---

### Utilitários Partilhados

#### ResultTests

`Unit/Shared/ResultTests.cs` — **14 testes** — Classes testadas: `Result<T>`, `Result`, `Error`

Verifica o padrão `Result` usado transversalmente em toda a aplicação para encapsular sucesso e falha.

**`ResultTests`** — `Success` define `IsSuccess`/`IsFailure`, expõe o valor, e o erro é `Error.None`; `Failure` define `IsFailure`, expõe código e mensagem, e o valor é `null`; `Ok` é equivalente a `Success`; versão não-genérica funciona correctamente.

**`ErrorTests`** — `Error.None` tem código e mensagem vazios; erros guardam código e mensagem; igualdade por valor (record).

---

## Testes de Integração

### Repositórios

#### UserRepositoryTests

`Integration/Repositories/UserRepositoryTests.cs` — **9 testes** — Classe testada: `UserRepository`

Cada teste recebe uma base de dados em memória isolada via [InMemoryDbContextFactory](#inmemorydbcontextfactory).

Cobre: `AddClientAsync` (persistência e devolução), `GetAllClientsAsync` (filtra por tipo `Client` excluindo `Lawyer`), `EmailExistsAsync` (existente, inexistente, sensível a maiúsculas), `GetByEmailAsync` (encontrado e não encontrado).

#### DocumentRepositoryTests

`Integration/Repositories/DocumentRepositoryTests.cs` — **12 testes** — Classe testada: `DocumentRepository`

Cobre: `AddAsync` (persistência e devolução), `GetByIdAsync` (encontrado e não encontrado), `GetByStoredFileNameAsync` (encontrado e não encontrado), `GetDocumentsByProcessIdAsync` (filtragem por processo e colecção vazia), `UpdateAsync` (persistência de alterações), `DeleteAsync` (remoção e tolerância a ID inexistente).

#### LegalProcessRepositoryTests

`Integration/Repositories/LegalProcessRepositoryTests.cs` — **16 testes** — Classe testada: `LegalProcessRepository`

Cobre: `AddAsync`, `GetByIdAsync`, `GetAllAsync`, `GetByLawyerIdAsync`, `GetByClientIdAsync`, `UserHasAccessToProcessAsync` (advogado tem acesso, cliente tem acesso, utilizador sem relação não tem acesso, processo inexistente devolve falso), `UpdateAsync` (mudança de estado), `DeleteAsync` (remoção e tolerância a ID inexistente).

---

### API

#### ClientControllerTests

`Integration/API/ClientControllerTests.cs` — **3 testes** — Rota testada: `POST /api/client/create`

| Teste | Descrição |
|---|---|
| `Create_WithValidData_ReturnsOk` | Dados válidos devolvem HTTP 200 |
| `Create_ResponseDoesNotContainPasswordHash` | A resposta não expõe o hash da palavra-passe |
| `Create_ResponseContainsEmail` | O email do cliente criado está presente na resposta |

#### LoginControllerTests

`Integration/API/LoginControllerTests.cs` — **5 testes** — Rotas testadas: `POST /api/auth/register`, `POST /api/auth/login`

| Teste | Descrição |
|---|---|
| `Register_WithValidData_Returns200` | Registo com dados válidos devolve HTTP 200 |
| `Register_ResponseDoesNotContainPasswordHash` | A resposta de registo não expõe o hash |
| `Login_WithNonExistentEmail_ReturnsBadRequest` | Email inexistente devolve HTTP 400 |
| `Login_WithWrongPassword_ReturnsBadRequest` | Palavra-passe errada devolve HTTP 400 |
| `Login_ResponseNeverContainsPasswordHash` | A resposta de login nunca expõe o hash |

---

### Segurança Integração

#### AuthorizationSecurityTests

`Integration/Security/AuthorizationSecurityTests.cs` — **7 testes**

Combina geração directa de JWT (para testes de estrutura de token sem dependência HTTP) com chamadas reais ao pipeline para verificar protecção de dados sensíveis.

| Teste | Descrição |
|---|---|
| `Token_DoesNotContainPasswordInPayload` | O payload do JWT não contém `"password"` nem `"passwordHash"` |
| `Token_PayloadContainsExpectedClaims` | O payload contém os claims `sub` e `email` |
| `Token_HasFutureExpiration` | O token expira no futuro |
| `Register_ResponseNeverLeaksPasswordHash` | O endpoint de registo nunca expõe o hash |
| `Login_WithInvalidCredentials_ReturnsBadRequest` | Credenciais inválidas devolvem HTTP 400 |
| `Login_NonExistentEmail_ReturnsSameStatusAsWrongPassword` | Ambos os casos devolvem o mesmo código HTTP, impedindo enumeração de emails |
| `Login_ErrorResponse_DoesNotLeakPasswordHash` | Respostas de erro não expõem dados de hashing |

---




