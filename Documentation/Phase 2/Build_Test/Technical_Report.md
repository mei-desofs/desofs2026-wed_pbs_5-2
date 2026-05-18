# Relatório Técnico — LawyerApp

**Versão:** 1.0
**Data:** 2026-05-12
**Autores:** Grupo Wed PBS 5-2
**Projeto:** LawyerApp — Back-end Seguro de Gestão Legal (DESOFS 2026)

---

## 1. Visão geral do projeto

O LawyerApp é uma REST API em .NET 8 / ASP.NET Core que implementa um
back-end seguro para uma consultora jurídica. Segue os princípios de
Clean Architecture e SSDLC (Secure Software Development Lifecycle).

### Stack tecnológico

| Componente | Tecnologia |
|---|---|
| Runtime | .NET 8 / ASP.NET Core 8 |
| Linguagem | C# 12 |
| Arquitetura | Clean Architecture (Domain · Application · Infrastructure · API) |
| Base de dados | PostgreSQL (cloud gerido Aiven) |
| ORM | Entity Framework Core 8 (Npgsql) |
| Hashing de palavra-passe | BCrypt.Net-Next (work factor 10) |
| Gestão de segredos | HashiCorp Vault (via VaultSharp) |
| Documentação da API | Swagger / Swashbuckle |
| Contentorização | Docker (build multi-stage) |

---

## 2. Arquitetura

```
LawyerApp/
├── API/                        # Controladores — fronteira HTTP
│   └── Client/
│       └── ClientController.cs
├── Application/                # Serviços de casos de uso (sem deps de framework)
│   ├── Interfaces/User/
│   │   └── IClient.cs
│   └── Services/UserAggregate/
│       └── ClientService.cs
├── Domain/                     # Entidades e interfaces do domínio nuclear
│   ├── Aggregates/
│   │   ├── UserAggregate/      # User (abstrato), Client, Lawyer, LegalAssistant
│   │   ├── DocumentAggregate/  # Document, DocCategory
│   │   └── LegalProcessAggregate/ # LegalProcess, ProcessStatus
│   └── Interfaces/Security/
│       └── IPasswordHasher.cs
├── Infrastructure/             # EF Core, repositórios, BCrypt, Vault
│   ├── HashiCorp/
│   ├── Persistence/            # LawyerAppDbContext + Repositórios
│   └── Security/
├── Migrations/                 # Histórico de migrations do EF Core
├── LawyerApp.Tests/            # Projeto de testes (xUnit)
├── Dockerfile
└── appsettings.json
```

### Resumo do modelo de domínio

- **User** (abstrato, discriminador TPH): classe base com `Name`,
  `Email`, `PasswordHash`, `CreatedAt`.
  - **Client**: acrescenta `BillingAddress`, `PhoneNumber`.
  - **Lawyer**: acrescenta `LicenseNumber`, `Specialization`.
  - **LegalAssistant**: função de staff especializado.
- **LegalProcess**: identificado por um GUID (gerado no domínio para
  garantir a disponibilidade do ID antes da criação de pastas no SO).
  Ligado a um `Lawyer` e a um `Client`.
- **Document**: ligado a um `LegalProcess`. Armazena um
  `StoredFileName` aleatório (baseado em GUID) para prevenir path
  traversal e enumeração de nomes de ficheiros.

---

## 3. Controlos de segurança

| Controlo | Implementação |
|---|---|
| Hashing de palavra-passe | BCrypt com salt aleatório por hash (`BCryptPasswordHasher`) |
| Sem credenciais em texto na BD | `ClientService.CreateClientAsync` aplica hash antes de persistir |
| Sem credenciais nas respostas da API | `ClientDto` não tem propriedade `PasswordHash` |
| IDs de recurso imprevisíveis | `LegalProcess.ProcessId` e `Document.StoredFileName` usam `Guid.NewGuid()` |
| Gestão de segredos | Connection string da BD obtida do HashiCorp Vault no arranque |
| Imposição de HTTPS | `app.UseHttpsRedirection()` |
| Análise de dependências | `dotnet list package --vulnerable` em CI |
| Análise de contentor | Trivy (CRITICAL/HIGH, ignore-unfixed) em CI |
| Deteção de fugas de segredos | Gitleaks sobre o histórico git completo em CI |

---

## 4. Executar a aplicação

### Pré-requisitos

| Requisito | Versão |
|---|---|
| .NET SDK | 8.0.x |
| Docker e Docker Compose | 20.x+ |
| PostgreSQL (opcional para dev local) | 15+ |

### 4.1 Opção A — .NET CLI local (com Aiven ou PostgreSQL local)

**Passo 1 — Configurar a connection string da base de dados**

A aplicação lê a connection string da chave de configuração
`ConnectionString:PostgreSQL`. Defini-la via variável de ambiente
(recomendado) ou `appsettings.Development.json`.

```bash
# Variável de ambiente (recomendado — nunca fazer commit de credenciais)
export ConnectionString__PostgreSQL="Host=<host>;Port=5432;Database=lawyerapp;Username=<user>;Password=<password>;SSL Mode=Require"
```

Ou adicionar a `LawyerApp/appsettings.Development.json` (nunca fazer
commit deste ficheiro):

```json
{
  "ConnectionString": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=lawyerapp;Username=postgres;Password=postgres"
  }
}
```

**Passo 2 — (Opcional) Configurar segredos do HashiCorp Vault**

Se usar o Vault para obter a connection string, definir os user-secrets
(estes são guardados fora do repositório):

```bash
cd LawyerApp
dotnet user-secrets init
dotnet user-secrets set "VaultSettings:ServerUri"  "<vault_uri>"
dotnet user-secrets set "VaultSettings:Token"      "<vault_token>"
dotnet user-secrets set "VaultSettings:MountPoint" "<mount_point>"
dotnet user-secrets set "VaultSettings:SecretPath" "<secret_path>"
```

Em seguida descomentar o bloco do Vault em `Program.cs` e comentar a
linha de connection string direta.

**Passo 3 — Aplicar migrations da base de dados**

```bash
cd LawyerApp
dotnet ef database update
```

**Passo 4 — Executar a aplicação**

```bash
dotnet run
```

A API arranca em `https://localhost:7xxx` e `http://localhost:5xxx`
(portas exatas em `Properties/launchSettings.json`). A UI do Swagger
está disponível em `https://localhost:<port>/swagger`.

---

### 4.2 Opção B — Docker

**Passo 1 — Construir a imagem**

```bash
cd LawyerApp
docker build -t lawyerapp:latest .
```

**Passo 2 — Executar o contentor**

Passar a connection string como variável de ambiente:

```bash
docker run -p 8080:8080 \
  -e "ConnectionString__PostgreSQL=Host=<host>;Port=5432;Database=lawyerapp;Username=<user>;Password=<password>;SSL Mode=Require" \
  lawyerapp:latest
```

A API ficará disponível em `http://localhost:8080`.

---

### 4.3 Opção C — Docker Compose (recomendado para desenvolvimento local)

Criar um `docker-compose.yml` na raiz do repositório:

```yaml
version: "3.9"
services:
  db:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: lawyerapp
      POSTGRES_USER: dev
      POSTGRES_PASSWORD: devpassword
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U dev"]
      interval: 5s
      timeout: 5s
      retries: 5

  api:
    build:
      context: LawyerApp/
    environment:
      ConnectionString__PostgreSQL: "Host=db;Port=5432;Database=lawyerapp;Username=dev;Password=devpassword"
    ports:
      - "8080:8080"
    depends_on:
      db:
        condition: service_healthy
```

```bash
docker compose up --build
```

Depois correr as migrations a partir do host:

```bash
cd LawyerApp
dotnet ef database update
```

---

## 5. Executar testes

### 5.1 Todos os testes

```bash
cd LawyerApp
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj -v normal
```

**Não é necessária ligação a base de dados ou rede.** Os testes de
integração usam um provider InMemory do EF Core isolado por execução.

### 5.2 Com cobertura de código

```bash
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

Os relatórios de cobertura são produzidos em formato Cobertura XML em
`./coverage/`.

### 5.3 Filtrar por categoria

```bash
# Só testes unitários
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Unit"

# Só testes de integração
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Integration"
```

---

## 6. Endpoints da API

| Método | Caminho | Descrição | Body do pedido | Resposta |
|---|---|---|---|---|
| `GET` | `/api/client/get/all` | Lista todos os clientes registados | — | `ClientDto[]` |
| `POST` | `/api/client/create` | Regista um novo cliente | `CreateClientDto` | `ClientDto` |

### CreateClientDto

```json
{
  "name": "Alice Lawyer Client",
  "email": "alice@example.com",
  "password": "S3cur3Password!",
  "billingAddress": "Rua das Flores 1, Lisboa",
  "phoneNumber": "912345678"
}
```

### ClientDto (resposta — sem campo password)

```json
{
  "name": "Alice Lawyer Client",
  "email": "alice@example.com",
  "billingAddress": "Rua das Flores 1, Lisboa",
  "phoneNumber": "912345678"
}
```

---

## 7. Resumo da pipeline CI/CD

O desenho completo, o mapeamento para o rubric e a checklist de
configuração no GitHub estão em [Pipeline.md](Pipeline.md).

| Workflow | Disparo | Jobs |
|---|---|---|
| `build-test.yml` | push / PR para main, develop, manual | Build (Release) + testes xUnit + cobertura + artefacto publish |
| `codeql.yml` | push / PR / semanal / manual | SAST GitHub CodeQL (C#, `security-and-quality`) |
| `semgrep.yml` | push / PR / semanal / manual | SAST Semgrep (`p/default`, `p/security-audit`, `p/csharp`, `p/dockerfile`, `p/secrets`) |
| `sonarcloud.yml` | manual (até `SONAR_TOKEN` estar configurado) | Análise SonarCloud opcional — ver Pipeline.md §5 |
| `dependency-review.yml` | PR | GitHub Dependency Review sobre os diffs do PR (falha em `high` ou GPL/AGPL) |
| `security-scan.yml` | push / PR / semanal / manual | SCA (`dotnet list --vulnerable`), analisadores .NET, análise Trivy à imagem |
| `trivy-config.yml` | push / PR / manual | Análise Trivy ao filesystem + IaC/Dockerfile config (falha em CRITICAL/HIGH) |
| `sbom.yml` | push / PR / semanal / manual | SBOM CycloneDX (solução) + SBOM Syft (imagem) + análise Grype |
| `secrets-scan.yml` | push / PR / semanal / manual | Gitleaks (histórico completo) + TruffleHog (apenas verified) |
| `config-validation.yml` | push / PR / manual | Hadolint + actionlint + yamllint + validação JSON/appsettings + `dotnet format` |
| `dependabot.yml` (config) | agendamento semanal | Atualizações de NuGet + GitHub Actions + base-images Docker |

---

## 8. Limitações conhecidas e trabalho futuro

| Item | Descrição |
|---|---|
| Autenticação / Autorização | Controlo de acesso JWT / baseado em papéis ainda não implementado |
| Agregados restantes | Placeholders de `DocumentController`, `LegalProcessController` existem mas não estão implementados |
| Validação de input | Anotações de dados `[Required]` / `[MaxLength]` ainda não aplicadas aos DTOs |
| Rate limiting | Não implementado |
| Logging de auditoria | Sem trilho de auditoria estruturado para operações de create/update |
