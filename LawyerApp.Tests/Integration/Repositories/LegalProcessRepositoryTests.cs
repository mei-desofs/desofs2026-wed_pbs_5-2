using FluentAssertions;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Integration.Repositories;

public class LegalProcessRepositoryTests : IDisposable
{
    private readonly LawyerApp.Infrastructure.Persistence.LawyerAppDbContext _context;
    private readonly LegalProcessRepository _sut;

    public LegalProcessRepositoryTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _sut = new LegalProcessRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsProcessToDatabase()
    {
        var process = new LegalProcess("Case A", "Desc A", Guid.NewGuid(), Guid.NewGuid());

        await _sut.AddAsync(process, CancellationToken.None);

        _context.LegalProcesses.Should().ContainSingle(p => p.Title == "Case A");
    }

    [Fact]
    public async Task AddAsync_ReturnsPersistedProcess()
    {
        var process = new LegalProcess("Case B", "Desc B", Guid.NewGuid(), Guid.NewGuid());

        var result = await _sut.AddAsync(process, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Case B");
        result.ProcessId.Should().Be(process.ProcessId);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsProcess()
    {
        var process = new LegalProcess("Case C", "Desc C", Guid.NewGuid(), Guid.NewGuid());
        await _sut.AddAsync(process, CancellationToken.None);

        var result = await _sut.GetByIdAsync(process.ProcessId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Case C");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllProcesses()
    {
        await _sut.AddAsync(new LegalProcess("P1", "D1", Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await _sut.AddAsync(new LegalProcess("P2", "D2", Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyCollection()
    {
        var result = await _sut.GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── GetByLawyerIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByLawyerIdAsync_ReturnsOnlyMatchingProcesses()
    {
        var lawyerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await _sut.AddAsync(new LegalProcess("Match", "D", lawyerId, Guid.NewGuid()), CancellationToken.None);
        await _sut.AddAsync(new LegalProcess("Other", "D", otherId, Guid.NewGuid()), CancellationToken.None);

        var result = await _sut.GetByLawyerIdAsync(lawyerId, CancellationToken.None);

        result.Should().ContainSingle(p => p.Title == "Match");
    }

    // ── GetByClientIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByClientIdAsync_ReturnsOnlyMatchingProcesses()
    {
        var clientId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await _sut.AddAsync(new LegalProcess("ClientMatch", "D", Guid.NewGuid(), clientId), CancellationToken.None);
        await _sut.AddAsync(new LegalProcess("ClientOther", "D", Guid.NewGuid(), otherId), CancellationToken.None);

        var result = await _sut.GetByClientIdAsync(clientId, CancellationToken.None);

        result.Should().ContainSingle(p => p.Title == "ClientMatch");
    }

    // ── UserHasAccessToProcessAsync ───────────────────────────────────────────

    [Fact]
    public async Task UserHasAccessToProcessAsync_WhenUserIsLawyer_ReturnsTrue()
    {
        var lawyerId = Guid.NewGuid();
        var process = new LegalProcess("T", "D", lawyerId, Guid.NewGuid());
        await _sut.AddAsync(process, CancellationToken.None);

        var hasAccess = await _sut.UserHasAccessToProcessAsync(lawyerId, process.ProcessId, CancellationToken.None);

        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasAccessToProcessAsync_WhenUserIsClient_ReturnsTrue()
    {
        var clientId = Guid.NewGuid();
        var process = new LegalProcess("T", "D", Guid.NewGuid(), clientId);
        await _sut.AddAsync(process, CancellationToken.None);

        var hasAccess = await _sut.UserHasAccessToProcessAsync(clientId, process.ProcessId, CancellationToken.None);

        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasAccessToProcessAsync_WhenUserHasNoRelation_ReturnsFalse()
    {
        var process = new LegalProcess("T", "D", Guid.NewGuid(), Guid.NewGuid());
        await _sut.AddAsync(process, CancellationToken.None);

        var hasAccess = await _sut.UserHasAccessToProcessAsync(Guid.NewGuid(), process.ProcessId, CancellationToken.None);

        hasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task UserHasAccessToProcessAsync_WhenProcessDoesNotExist_ReturnsFalse()
    {
        var hasAccess = await _sut.UserHasAccessToProcessAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        hasAccess.Should().BeFalse();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var process = new LegalProcess("T", "D", Guid.NewGuid(), Guid.NewGuid());
        await _sut.AddAsync(process, CancellationToken.None);

        process.Status = ProcessStatus.Closed;
        await _sut.UpdateAsync(process, CancellationToken.None);

        var updated = await _sut.GetByIdAsync(process.ProcessId, CancellationToken.None);
        updated!.Status.Should().Be(ProcessStatus.Closed);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesProcessFromDatabase()
    {
        var process = new LegalProcess("ToDelete", "D", Guid.NewGuid(), Guid.NewGuid());
        await _sut.AddAsync(process, CancellationToken.None);

        await _sut.DeleteAsync(process.ProcessId, CancellationToken.None);

        var result = await _sut.GetByIdAsync(process.ProcessId, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenProcessDoesNotExist_DoesNotThrow()
    {
        var act = async () => await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
