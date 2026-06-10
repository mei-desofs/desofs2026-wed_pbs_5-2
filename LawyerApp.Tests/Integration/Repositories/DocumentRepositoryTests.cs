using FluentAssertions;
using LawyerApp.Domain.Aggregates.DocumentAggregate;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Integration.Repositories;

public class DocumentRepositoryTests : IDisposable
{
    private readonly LawyerApp.Infrastructure.Persistence.LawyerAppDbContext _context;
    private readonly DocumentRepository _sut;

    public DocumentRepositoryTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _sut = new DocumentRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsDocumentToDatabase()
    {
        var processId = Guid.NewGuid();
        var doc = new Document("contract.pdf", 1024, "application/pdf", DocCategory.Contract, processId);

        await _sut.AddAsync(doc, CancellationToken.None);

        _context.Documents.Should().ContainSingle(d => d.FileName == "contract.pdf");
    }

    [Fact]
    public async Task AddAsync_ReturnsPersistedDocument()
    {
        var doc = new Document("evidence.jpg", 2048, "image/jpeg", DocCategory.Evidence, Guid.NewGuid());

        var result = await _sut.AddAsync(doc, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Be("evidence.jpg");
        result.StoredFileName.Should().Be(doc.StoredFileName);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDocument()
    {
        var doc = new Document("petition.pdf", 512, "application/pdf", DocCategory.Petition, Guid.NewGuid());
        var added = await _sut.AddAsync(doc, CancellationToken.None);

        var result = await _sut.GetByIdAsync(added.DocumentId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.FileName.Should().Be("petition.pdf");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(99999, CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetByStoredFileNameAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByStoredFileNameAsync_WhenExists_ReturnsDocument()
    {
        var doc = new Document("order.pdf", 1024, "application/pdf", DocCategory.CourtOrder, Guid.NewGuid());
        await _sut.AddAsync(doc, CancellationToken.None);

        var result = await _sut.GetByStoredFileNameAsync(doc.StoredFileName, CancellationToken.None);

        result.Should().NotBeNull();
        result!.FileName.Should().Be("order.pdf");
    }

    [Fact]
    public async Task GetByStoredFileNameAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _sut.GetByStoredFileNameAsync("nonexistent-guid.pdf", CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetDocumentsByProcessIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetDocumentsByProcessIdAsync_ReturnsOnlyMatchingDocuments()
    {
        var processId = Guid.NewGuid();
        var otherProcessId = Guid.NewGuid();

        await _sut.AddAsync(new Document("a.pdf", 1, "application/pdf", DocCategory.Contract, processId), CancellationToken.None);
        await _sut.AddAsync(new Document("b.pdf", 1, "application/pdf", DocCategory.Contract, processId), CancellationToken.None);
        await _sut.AddAsync(new Document("c.pdf", 1, "application/pdf", DocCategory.Contract, otherProcessId), CancellationToken.None);

        var result = await _sut.GetDocumentsByProcessIdAsync(processId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.LegalProcessId == processId);
    }

    [Fact]
    public async Task GetDocumentsByProcessIdAsync_WhenNoDocuments_ReturnsEmptyCollection()
    {
        var result = await _sut.GetDocumentsByProcessIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsFileNameChange()
    {
        var doc = new Document("original.pdf", 1024, "application/pdf", DocCategory.Contract, Guid.NewGuid());
        var added = await _sut.AddAsync(doc, CancellationToken.None);

        added.FileName = "renamed.pdf";
        await _sut.UpdateAsync(added, CancellationToken.None);

        var updated = await _sut.GetByIdAsync(added.DocumentId, CancellationToken.None);
        updated!.FileName.Should().Be("renamed.pdf");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesDocumentFromDatabase()
    {
        var doc = new Document("toDelete.pdf", 1024, "application/pdf", DocCategory.Evidence, Guid.NewGuid());
        var added = await _sut.AddAsync(doc, CancellationToken.None);

        await _sut.DeleteAsync(added.DocumentId, CancellationToken.None);

        var result = await _sut.GetByIdAsync(added.DocumentId, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentDoesNotExist_DoesNotThrow()
    {
        var act = async () => await _sut.DeleteAsync(99999, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
