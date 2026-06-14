using FluentAssertions;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.Security;
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Application.Services.UserAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Tests.Helpers;
using Moq;
using Xunit;

namespace LawyerApp.Tests.Unit.Security.ASVS;

/// <summary>
/// ASVS V2 – Validation and Business Logic
///
/// V2.2.1 – Input is validated against expected structure, pattern, and logical limits.
/// V2.3.1 – Business logic flows process steps in order and do not skip steps.
/// V2.3.3 – Operations affecting multiple data stores are atomic (roll back on failure).
/// V2.3.2 – Business logic limits are enforced.
/// </summary>
public class V2_InputValidationTests : IDisposable
{
    private readonly LawyerApp.Infrastructure.Persistence.LawyerAppDbContext _context;
    private readonly LegalProcessRepository _processRepo;
    private readonly Mock<IUserRepository>  _userRepoMock = new();
    private readonly Mock<IPasswordHasher>  _hasherMock   = new();
    private readonly ClientService          _clientService;

    public V2_InputValidationTests()
    {
        _context      = InMemoryDbContextFactory.Create();
        _processRepo  = new LegalProcessRepository(_context);
        _clientService = new ClientService(_userRepoMock.Object, _hasherMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── V2.2.1 / V2.3.2 – Duplicate email is rejected (business rule) ────────

    [Fact]
    public async Task V2_2_1_CreateClient_WithDuplicateEmail_IsRejected()
    {
        var dto = new CreateClientDto("Alice", "alice@test.com", "Pass1!", "Rua A", "910000001");

        _userRepoMock.Setup(r => r.EmailExistsAsync("alice@test.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);

        var result = await _clientService.CreateClientAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue("duplicate email violates a business rule and must be rejected (V2.2.1 / V2.3.2)");
    }

    [Fact]
    public async Task V2_2_1_CreateClient_WithUniqueEmail_IsAccepted()
    {
        var dto = new CreateClientDto("Bob", "bob@test.com", "Pass1!", "Rua B", "910000002");

        _userRepoMock.Setup(r => r.EmailExistsAsync("bob@test.com", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");
        _userRepoMock.Setup(r => r.AddClientAsync(It.IsAny<CreateClientDto>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((CreateClientDto d, CancellationToken _) =>
                         new LawyerApp.Domain.Aggregates.UserAggregate.Client(d.Name, d.Email, d.Password, d.BillingAddress, d.PhoneNumber));

        var result = await _clientService.CreateClientAsync(dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue("a unique email satisfies the business rule (V2.2.1)");
    }

    // ── V2.3.1 – Email uniqueness check must precede persistence ─────────────

    [Fact]
    public async Task V2_3_1_EmailCheck_IsPerformed_BeforePersistence()
    {
        var dto = new CreateClientDto("Carol", "carol@test.com", "Pass1!", "Rua C", "910000003");
        var callOrder = new List<string>();

        _userRepoMock.Setup(r => r.EmailExistsAsync("carol@test.com", It.IsAny<CancellationToken>()))
                     .Callback(() => callOrder.Add("EmailExists"))
                     .ReturnsAsync(false);
        _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>()))
                   .Callback(() => callOrder.Add("HashPassword"))
                   .Returns("hash");
        _userRepoMock.Setup(r => r.AddClientAsync(It.IsAny<CreateClientDto>(), It.IsAny<CancellationToken>()))
                     .Callback(() => callOrder.Add("AddClient"))
                     .ReturnsAsync(new LawyerApp.Domain.Aggregates.UserAggregate.Client("Carol", "carol@test.com", "hash", "Rua C", "910000003"));

        await _clientService.CreateClientAsync(dto, CancellationToken.None);

        callOrder[0].Should().Be("EmailExists",
            "the email uniqueness check must happen before hashing or persisting (V2.3.1)");
        callOrder[1].Should().Be("HashPassword",
            "password must be hashed before persisting (V2.3.1)");
        callOrder[2].Should().Be("AddClient",
            "persistence must be the last step (V2.3.1)");
    }

    // ── V2.3.2 – LegalProcess state machine enforces valid transitions ─────────

    [Fact]
    public async Task V2_3_2_LegalProcess_InitialStatus_MustBeOpen()
    {
        var process = new LegalProcess("Case 1", "Desc", Guid.NewGuid(), Guid.NewGuid());
        await _processRepo.AddAsync(process, CancellationToken.None);

        var retrieved = await _processRepo.GetByIdAsync(process.ProcessId, CancellationToken.None);

        retrieved!.Status.Should().Be(ProcessStatus.Open,
            "a new legal process must always start in Open status (V2.3.2 business logic limit)");
    }

    [Fact]
    public async Task V2_3_2_LegalProcess_StatusTransition_IsApplied_Atomically()
    {
        var process = new LegalProcess("Case 2", "Desc", Guid.NewGuid(), Guid.NewGuid());
        await _processRepo.AddAsync(process, CancellationToken.None);

        process.Status = ProcessStatus.InAnalysis;
        await _processRepo.UpdateAsync(process, CancellationToken.None);

        var retrieved = await _processRepo.GetByIdAsync(process.ProcessId, CancellationToken.None);

        retrieved!.Status.Should().Be(ProcessStatus.InAnalysis,
            "status transition must be persisted atomically (V2.3.3)");
    }

    // ── V2.3.3 – Delete is atomic: only target is removed ────────────────────

    [Fact]
    public async Task V2_3_3_DeleteProcess_OnlyRemovesTargetProcess()
    {
        var p1 = new LegalProcess("To Delete", "Desc", Guid.NewGuid(), Guid.NewGuid());
        var p2 = new LegalProcess("Keep This",  "Desc", Guid.NewGuid(), Guid.NewGuid());

        await _processRepo.AddAsync(p1, CancellationToken.None);
        await _processRepo.AddAsync(p2, CancellationToken.None);

        await _processRepo.DeleteAsync(p1.ProcessId, CancellationToken.None);

        var deleted  = await _processRepo.GetByIdAsync(p1.ProcessId, CancellationToken.None);
        var retained = await _processRepo.GetByIdAsync(p2.ProcessId, CancellationToken.None);

        deleted.Should().BeNull("the target process must be removed (V2.3.3)");
        retained.Should().NotBeNull("other processes must be unaffected by a delete operation (V2.3.3)");
    }

    // ── V2.2.1 – Access control: only lawyer or client of a process can access ─

    [Fact]
    public async Task V2_2_1_UserHasAccess_IsEnforced_ByProcessRelation()
    {
        var lawyerId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var process  = new LegalProcess("Restricted", "Desc", lawyerId, clientId);
        await _processRepo.AddAsync(process, CancellationToken.None);

        var lawyerAccess    = await _processRepo.UserHasAccessToProcessAsync(lawyerId,        process.ProcessId, CancellationToken.None);
        var clientAccess    = await _processRepo.UserHasAccessToProcessAsync(clientId,        process.ProcessId, CancellationToken.None);
        var strangerAccess  = await _processRepo.UserHasAccessToProcessAsync(Guid.NewGuid(),  process.ProcessId, CancellationToken.None);

        lawyerAccess.Should().BeTrue("the assigned lawyer must have access (V2.2.1)");
        clientAccess.Should().BeTrue("the assigned client must have access (V2.2.1)");
        strangerAccess.Should().BeFalse("an unrelated user must be denied access — IDOR prevention (V2.2.1 / V8.2.2)");
    }
}
