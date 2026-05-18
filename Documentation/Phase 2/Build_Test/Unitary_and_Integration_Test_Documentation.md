# LawyerApp — Documentação da Suite de Testes

## Visão Geral

O projecto `LawyerApp.Tests` contém os testes unitários e de integração do backend da LawyerApp. Os testes são escritos em **C# / .NET 8** utilizando o **xUnit** como executor de testes, o **FluentAssertions** para assertions legíveis e o **Moq** para substituição de dependências nos testes unitários. Os testes de integração recorrem ao **Microsoft.AspNetCore.Mvc.Testing** para arrancar o pipeline real do ASP.NET Core e ao **Microsoft.EntityFrameworkCore.InMemory** como substituto da base de dados PostgreSQL.

```
LawyerApp.Tests/
├── Helpers/
│   ├── CustomWebApplicationFactory.cs   # Pipeline HTTP completo com BD em memória
│   └── InMemoryDbContextFactory.cs      # Fábrica de DbContext para testes de repositório
├── Unit/
│   ├── Domain/
│   │   └── ClientTests.cs               # Testes unitários das entidades de domínio
│   ├── Security/
│   │   └── BCryptPasswordHasherTests.cs # Testes unitários de hashing de palavras-passe
│   └── Services/
│       └── ClientServiceTests.cs        # Testes unitários do serviço de aplicação
└── Integration/
    ├── API/
    │   └── ClientControllerTests.cs     # Testes de integração do controlador HTTP
    └── Repositories/
        └── UserRepositoryTests.cs       # Testes de integração do repositório
```

**Total de testes: 34**

---

## Índice

- [Infraestrutura de Testes](#infraestrutura-de-testes)
  - [`CustomWebApplicationFactory`](#customwebapplicationfactory)
  - [`InMemoryDbContextFactory`](#inmemorydbcontextfactory)
- [Testes Unitários](#testes-unitários)
  - [`ClientTests`](#clienttests--testes-unitários-das-entidades-de-domínio)
  - [`BCryptPasswordHasherTests`](#bcryptpasswordhasher--testes-unitários-de-segurança)
  - [`ClientServiceTests`](#clientservicetests--testes-unitários-do-serviço-de-aplicação)
- [Testes de Integração](#testes-de-integração)
  - [`UserRepositoryTests`](#userrepositorytests--testes-de-integração-do-repositório)
  - [`ClientControllerTests`](#clientcontrollertests--testes-de-integração)
- [Dependências](#dependências)
- [Execução dos Testes](#execução-dos-testes)

---

## Infraestrutura de Testes

### `CustomWebApplicationFactory`

> 📄 [`Helpers/CustomWebApplicationFactory.cs`](/LawyerApp.Tests/Helpers/CustomWebApplicationFactory.cs)

Localizado em `Helpers/CustomWebApplicationFactory.cs`.

Estende `WebApplicationFactory<Program>` para arrancar a aplicação ASP.NET Core completa nos testes de integração ao nível HTTP. Substitui o registo real do `DbContext` para PostgreSQL por um **fornecedor EF Core em memória**, permitindo que os testes corram sem qualquer dependência de base de dados externa.

Comportamento principal:
- Define o ambiente como `"Testing"` para suprimir a página de excepções de desenvolvimento.
- Remove o registo existente de `DbContextOptions<LawyerAppDbContext>`
- Invoca `EnsureCreated()` no arranque para que o esquema esteja imediatamente disponível.

### `InMemoryDbContextFactory`

> 📄 [`Helpers/InMemoryDbContextFactory.cs`](/LawyerApp.Tests/Helpers/InMemoryDbContextFactory.cs)

Localizado em `Helpers/InMemoryDbContextFactory.cs`.

Cria um `LawyerAppDbContext` autónomo, suportado por uma base de dados em memória com nome único completamente isolada do pipeline HTTP.

```csharp
_context = InMemoryDbContextFactory.Create();
```

---

## Testes Unitários

### `ClientTests` — Testes Unitários das Entidades de Domínio

> 📄 [`Unit/Domain/ClientTests.cs`](/LawyerApp.Tests/Unit/Domain/ClientTests.cs)

**Ficheiro:** `Unit/Domain/ClientTests.cs`  
**Classe testada:** `LawyerApp.Domain.Aggregates.UserAggregate.Client`

Testa a entidade de domínio `Client` quanto ao comportamento correto de construção

| Teste | Descrição |
|-------|-----------|
| `Client_Constructor_SetsAllProperties` | Verifica que os cinco parâmetros do construtor (`Name`, `Email`, `PasswordHash`, `BillingAddress`, `PhoneNumber`) são correctamente atribuídos ao instanciar um `Client` |
| `Client_CreatedAt_IsSetToUtcNow` | Confirma que `CreatedAt` é automaticamente definido para a hora UTC actual no momento da construção |
| `Client_IsSubclassOfUser` | Assegura que `Client` herda da classe base `User`. |

**Classes adicionais testadas no mesmo ficheiro:**

#### `LegalProcessTests`

| Teste | Descrição |
|-------|-----------|
| `LegalProcess_Constructor_GeneratesUniqueGuids` | Duas instâncias de `LegalProcess` criadas com os mesmos IDs de advogado e cliente devem receber `ProcessId` distintos. |
| `LegalProcess_InitialStatus_IsOpen` | Verifica que um `LegalProcess` recém-criado tem sempre o estado inicial `ProcessStatus.Open`. |

#### `DocumentTests`

| Teste | Descrição |
|-------|-----------|
| `Document_Constructor_GeneratesUniqueStoredFileName` | Dois `Document` com o mesmo nome de ficheiro original devem produzir valores de `StoredFileName` diferentes. |
| `Document_StoredFileName_PreservesFileExtension` | O `StoredFileName` gerado deve preservar a extensão original do ficheiro. |

---

### `BCryptPasswordHasher` — Testes Unitários de Segurança

> 📄 [`Unit/Security/BCryptPasswordHasherTests.cs`](/LawyerApp.Tests/Unit/Security/BCryptPasswordHasherTests.cs)

**Ficheiro:** `Unit/Security/BCryptPasswordHasherTests.cs`  
**Classe testada:** `LawyerApp.Infrastructure.Security.BCryptPasswordHasher`

Testa directamente a implementação de hashing de palavras-passe baseada em BCrypt, sem mocks — a biblioteca real `BCrypt.Net` é exercitada.

| Teste | Descrição |
|-------|-----------|
| `HashPassword_ReturnsNonEmptyHash` | O hashing de qualquer palavra-passe produz um resultado não nulo. |
| `HashPassword_OutputDiffersFromInput` | O valor do hash deve ser diferente do texto em claro fornecido como entrada. |
| `HashPassword_TwoCallsProduceDifferentHashes` | Duas invocações com a mesma palavra-passe produzem hashes distintos. |
| `VerifyPassword_WithCorrectPassword_ReturnsTrue` | `VerifyPassword` retorna `true` quando fornecida a palavra-passe que foi originalmente submetida a hashing. |
| `VerifyPassword_WithWrongPassword_ReturnsFalse` | `VerifyPassword` retorna `false` para uma palavra-passe diferente confrontada com o hash armazenado. |
| `VerifyPassword_WithEmptyPassword_ReturnsFalse` | Uma string vazia não é verificada com sucesso contra nenhum hash real. |
| `HashPassword_ProducesBCryptFormatHash` | O resultado corresponde à expressão regular do formato BCrypt `^\$2[ab]\$`. |

---

### `ClientServiceTests` — Testes Unitários do Serviço de Aplicação

> 📄 [`Unit/Services/ClientServiceTests.cs`](/LawyerApp.Tests/Unit/Services/ClientServiceTests.cs)

**Ficheiro:** `Unit/Services/ClientServiceTests.cs`  
**Classe testada:** `LawyerApp.Application.Services.UserAggregate.ClientService`

Testa o serviço da camada de aplicação com as dependências `IUserRepository` e `IPasswordHasher` substituídas por mocks.

#### Testes de `CreateClientAsync`

| Teste | Descrição |
|-------|-----------|
| `CreateClientAsync_WhenEmailIsNew_ReturnsClientDto` | Quando `EmailExistsAsync` retorna `false`, o serviço cria o cliente e devolve um `ClientDto` com os quatro campos mapeados. |
| `CreateClientAsync_WhenEmailAlreadyExists_ThrowsException` | Quando `EmailExistsAsync` retorna `true`, é lançada uma `Exception`. |
| `CreateClientAsync_HashesPasswordBeforePersisting` | Verifica que `AddClientAsync` é invocado com um `Client` cujo `PasswordHash` é igual ao valor devolvido pelo mock do hasher — e não a palavra-passe em plain text. |
| `CreateClientAsync_NeverStoresPlainTextPassword` | Verifica via `Times.Never` que `AddClientAsync` nunca é invocado com um `Client` cujo `PasswordHash` corresponde ao plain text. |

#### Testes de `GetAllClientsAsync`

| Teste | Descrição |
|-------|-----------|
| `GetAllClientsAsync_ReturnsMappedDtos` | Quando o repositório devolve duas entidades `Client`, o serviço retorna uma lista com dois objectos `ClientDto` correctamente mapeados. |
| `GetAllClientsAsync_WhenNoClients_ReturnsEmptyList` | Quando o repositório devolve uma lista vazia, o serviço também retorna uma lista vazia. |
| `GetAllClientsAsync_DoesNotExposePasswordHash` | Utiliza reflexão para confirmar que `ClientDto` não possui uma propriedade `PasswordHash`, garantindo que o hash da palavra-passe nunca é exposto. |

---

## Testes de Integração

Os testes de integração testam o flow da aplicação completa usando uma base de dados real em memória. Detectam problemas de ligação entre componentes, erros de serialização, comportamento do middleware.

---

### `UserRepositoryTests` — Testes de Integração do Repositório

> 📄 [`Integration/Repositories/UserRepositoryTests.cs`](/LawyerApp.Tests/Integration/Repositories/UserRepositoryTests.cs)

**Ficheiro:** `Integration/Repositories/UserRepositoryTests.cs`  
**Classe testada:** `LawyerApp.Infrastructure.Persistence.Repositories.UserRepository`

Cada teste recebe uma **base de dados em memória nova e isolada** via `InMemoryDbContextFactory.Create()`. Os testes utilizam `IDisposable` para garantir que o `DbContext` é correctamente eliminado após cada teste.

#### Testes de `AddClientAsync`

| Teste | Descrição |
|-------|-----------|
| `AddClientAsync_PersistsClientToDatabase` | Após invocar `AddClientAsync`, o DbSet `Users` contém exactamente um utilizador com o email esperado. |
| `AddClientAsync_ReturnsPersistedClient` | O método devolve a entidade `Client` guardada com o campo `Name` intacto. |

#### Testes de `GetAllClientsAsync`

| Teste | Descrição |
|-------|-----------|
| `GetAllClientsAsync_ReturnsOnlyClients` | Quando a base de dados contém um `Client` e um `Lawyer`, apenas o `Client` é retornado — confirmando que o filtro `OfType<Client>()` funciona correctamente. |

#### Testes de `EmailExistsAsync`

| Teste | Descrição |
|-------|-----------|
| `EmailExistsAsync_WhenEmailExists_ReturnsTrue` | Retorna `true` após um cliente com esse email ter sido persistido. |
| `EmailExistsAsync_WhenEmailDoesNotExist_ReturnsFalse` | Retorna `false` para um email que nunca foi guardado. |
| `EmailExistsAsync_IsCaseSensitive` | Verifica que uma correspondência exacta em termos de capitalização retorna `true`, confirmando a comparação de strings `Ordinal` por defeito do fornecedor EF Core em memória. |

#### Testes de `GetByEmailAsync`

| Teste | Descrição |
|-------|-----------|
| `GetByEmailAsync_WhenEmailExists_ReturnsUser` | Devolve a entidade `User` correcta com o `Name` esperado. |
| `GetByEmailAsync_WhenEmailDoesNotExist_ReturnsNull` | Devolve `null` em vez de lançar uma excepção quando não é encontrada nenhuma correspondência. |

---

### `ClientControllerTests` — Testes de Integração

> 📄 [`Integration/API/ClientControllerTests.cs`](/LawyerApp.Tests/Integration/API/ClientControllerTests.cs)

**Ficheiro:** `Integration/API/ClientControllerTests.cs`  
**Classe testada:** `LawyerApp.API.ClientController`

Utiliza `IClassFixture<CustomWebApplicationFactory>` para partilhar uma factory entre todos os testes da classe. Um `HttpClient` é obtido a partir da fábrica e utilizado para efectuar chamadas HTTP com a aplicação em execução.

#### `GET /api/client/get/all`

| Teste | Descrição |
|-------|-----------|
| `GetAll_WhenNoClients_ReturnsEmptyArray` | Um GET a `/api/client/get/all` sobre uma base de dados vazia retorna HTTP 200 com um array JSON vazio. |
| `GetAll_AfterCreatingClient_ReturnsCreatedClient` | Após um POST bem-sucedido a `/api/client/create`, um GET subsequente retorna HTTP 200. |

#### `POST /api/client/create`

| Teste | Descrição |
|-------|-----------|
| `Create_WithValidData_ReturnsCreatedClient` | Um POST com dados válidos de `CreateClientDto` retorna HTTP 200. |
| `Create_ResponseDoesNotContainPasswordHash` | O corpo da resposta em bruto não deve conter as strings `"passwordHash"`, `"PasswordHash"` nem `"password_hash"`, confirmando que o hash da palavra-passe nunca é serializado na resposta HTTP. |
| `Create_WithDuplicateEmail_ReturnsError` | Um segundo POST com o mesmo email retorna HTTP 200 (resultado depende do tratamento de erros do ASP.NET Core no ambiente `"Testing"`). |

---

## Dependências

| Pacote | Versão | Finalidade |
|--------|--------|------------|
| `xunit` | 2.9.2 | Executor de testes e atributos `[Fact]` |
| `xunit.runner.visualstudio` | 2.8.2 | Descoberta de testes no Visual Studio / CLI |
| `Moq` | 4.20.72 | Framework de mocking para testes unitários |
| `FluentAssertions` | 6.12.2 | Biblioteca de assertions legíveis |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0.11 | Servidor HTTP de testes em processo |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.11 | Fornecedor EF Core em memória |
| `coverlet.collector` | 6.0.2 | Recolha de dados de cobertura de código |
| `Microsoft.NET.Test.Sdk` | 17.11.1 | Integração de testes com o MSBuild |

---

## Execução dos Testes

```bash
# A partir da raiz do repositório
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj

dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj --logger "console;verbosity=normal"

dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj --collect:"XPlat Code Coverage"
```

Todos os testes são completamente auto-suficientes, isto é, não necessitam de serviços externos.
