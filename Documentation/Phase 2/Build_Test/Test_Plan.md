# Plano de Testes — LawyerApp

**Versão:** 1.0
**Data:** 2026-05-12
**Autor:** Grupo Wed PBS 5-2 — Build & Test
**Projeto:** LawyerApp — Back-end Seguro de Gestão Legal

> Ver também [Pipeline.md](Pipeline.md) — referência operacional da
> pipeline, cobre como este plano de testes é executado em CI
> (`build-test.yml`).

---

## 1. Objetivos

Este documento descreve a estratégia de testes para a REST API do
LawyerApp. Os objetivos são:

1. Validar que todas as regras de negócio implementadas se comportam
   como especificado.
2. Verificar que os controlos de segurança (hashing de palavra-passe,
   ausência de exposição de credenciais nas respostas) são impostos.
3. Garantir que os contratos da API são estáveis e livres de
   regressões.
4. Fornecer evidência automática e reprodutível de correção para o
   relatório SSDLC.

---

## 2. Âmbito

| Dentro do âmbito | Fora do âmbito |
|---|---|
| Lógica aplicacional do `ClientService` | UI / front-end (não existe) |
| Endpoints da API `ClientController` | Testes de carga / stress |
| Camada de acesso a dados `UserRepository` | Testes exploratórios manuais |
| Utilitário de segurança `BCryptPasswordHasher` | Conectividade ao HashiCorp Vault |
| Entidades de domínio (Client, LegalProcess, Document) | Base de dados de produção (Aiven PostgreSQL) |

---

## 3. Níveis de testes

### 3.1 Testes unitários

**Localização:** `LawyerApp.Tests/Unit/`
**Framework:** xUnit 2.9 · Moq 4.20 · FluentAssertions 6.12
**Isolamento:** Todas as dependências externas (repositório, hasher)
são substituídas por mocks Moq.

| Classe de testes | Cobre | # Testes |
|---|---|---|
| `ClientServiceTests` | Caminho feliz de `CreateClientAsync`, rejeição de email duplicado, imposição do hash de palavra-passe, mapeamento de `GetAll`, ausência de `PasswordHash` no DTO | 7 |
| `BCryptPasswordHasherTests` | Formato do hash, unicidade (salt aleatório), verify-correto, verify-errado, verify-vazio | 7 |
| `ClientTests` | Mapeamento de propriedades pelo construtor, timestamp UTC de `CreatedAt`, cadeia de herança | 3 |
| `LegalProcessTests` | Unicidade do GUID, estado inicial = Open | 2 |
| `DocumentTests` | Unicidade de `StoredFileName`, preservação da extensão | 2 |

**Total de testes unitários:** 21

### 3.2 Testes de integração

**Localização:** `LawyerApp.Tests/Integration/`
**Framework:** `Microsoft.AspNetCore.Mvc.Testing` + provider InMemory
do EF Core
**Infraestrutura:** `CustomWebApplicationFactory` substitui o
`DbContext` PostgreSQL por uma base de dados in-memory isolada por
coleção de testes.

| Classe de testes | Cobre | # Testes |
|---|---|---|
| `ClientControllerTests` | GET /api/client/get/all (vazio + após inserção), POST /api/client/create (sucesso, sem hash na resposta, rejeição de duplicado) | 4 |
| `UserRepositoryTests` | AddClientAsync, GetAllClientsAsync (discriminador TPH), EmailExistsAsync (true/false/case), GetByEmailAsync (encontrado / não encontrado) | 7 |

**Total de testes de integração:** 11

---

## 4. Execução dos testes

### 4.1 Execução local

```bash
# A partir da raiz do repositório
cd LawyerApp

# Correr todos os testes (unitários + integração)
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj -v normal

# Correr com recolha de cobertura
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Correr só testes unitários
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Unit"

# Correr só testes de integração
dotnet test LawyerApp.Tests/LawyerApp.Tests.csproj \
  --filter "FullyQualifiedName~Integration"
```

### 4.2 Execução CI/CD

Os testes correm automaticamente em cada push e pull request para
`main` e `develop` através do workflow GitHub Actions
[build-test.yml](../../../.github/workflows/build-test.yml). Os
resultados são carregados como artefactos (`.trx` + XML Cobertura).

---

## 5. Casos de teste focados em segurança

Os testes seguintes exercitam diretamente controlos OWASP e ASVS:

| Teste | Controlo ASVS | Descrição |
|---|---|---|
| `CreateClientAsync_NeverStoresPlainTextPassword` | V2.1.1 | Verifica que a string de palavra-passe em bruto nunca é enviada ao repositório |
| `CreateClientAsync_HashesPasswordBeforePersisting` | V2.1.1 | Confirma que o hash BCrypt (e não o texto simples) é armazenado |
| `BCryptPasswordHasherTests.*` | V2.1.9 | Cobertura completa do ciclo de hash/verify do bcrypt |
| `Create_ResponseDoesNotContainPasswordHash` | V8.3.1 | O body de resposta HTTP não deve incluir o campo `passwordHash` |
| `GetAllClientsAsync_DoesNotExposePasswordHash` | V8.3.1 | O `ClientDto` não deve ter propriedade `PasswordHash` |
| `GetAllClientsAsync_WhenNoClients_ReturnsEmptyArray` | V13.1.1 | Array JSON válido devolvido mesmo para conjuntos de resultados vazios |

---

## 6. Estratégia de dados de teste

Todos os dados de teste estão contidos dentro de cada classe de testes
(padrão Arrange-Act-Assert). Sem estado partilhado entre testes. A base
de dados in-memory tem escopo limitado à execução do teste via
`Guid.NewGuid()` em `InMemoryDbContextFactory.Create()`, prevenindo
poluição entre testes.

---

## 7. Metas de cobertura

| Camada | Cobertura alvo |
|---|---|
| Application Services | ≥ 90% |
| Entidades de Domínio | ≥ 85% |
| Infrastructure (Repositórios) | ≥ 80% |
| Controladores da API | ≥ 75% (via testes de integração) |

---

## 8. Análise de artefactos

Ver [Artifact_Scanning.md](Artifact_Scanning.md) para a estratégia
completa de análise, ferramentas e integração com CI.

---

## 9. Critérios de passagem / falha

Uma build é considerada **a passar** se:

- Todas as execuções `dotnet test` saem com código 0.
- Não são encontrados pacotes NuGet vulneráveis
  (`dotnet list package --vulnerable`).
- O Trivy não encontra CVEs **CRITICAL** ou **HIGH** na imagem do
  contentor.
- O Gitleaks não encontra segredos hardcoded.
