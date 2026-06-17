using FluentAssertions;
using LawyerApp.Domain.Aggregates.DocumentAggregate;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Tests.Helpers;
using Xunit;

namespace LawyerApp.Tests.Unit.Security.ASVS;

/// <summary>
/// ASVS V5 – File Handling
///
/// V5.3.2 – Application uses internally generated filenames, not user-submitted ones,
///          to prevent path traversal, LFI, RFI, and SSRF attacks.
/// V5.2.2 – File extension is validated (StoredFileName preserves original extension only).
/// V5.1.1 – Uploaded files produce safe, non-predictable stored names.
/// </summary>
public class V5_FileHandlingTests : IDisposable
{
    private readonly LawyerApp.Infrastructure.Persistence.LawyerAppDbContext _context;
    private readonly DocumentRepository _repo;

    public V5_FileHandlingTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _repo    = new DocumentRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── V5.3.2 – Stored filename is internally generated, not user-provided ───

    [Fact]
    public void V5_3_2_Document_StoredFileName_IsNotTheSameAsUserProvidedName()
    {
        var userProvidedName = "important-contract.pdf";
        var doc = new Document(userProvidedName, 1024, "application/pdf", DocCategory.Contract, Guid.NewGuid());

        doc.StoredFileName.Should().NotBe(userProvidedName,
            "the stored filename must be internally generated, never the raw user-provided name (V5.3.2)");
    }

    [Fact]
    public void V5_3_2_StoredFileName_ContainsGuid_PreventsEnumeration()
    {
        var doc = new Document("evidence.pdf", 512, "application/pdf", DocCategory.Evidence, Guid.NewGuid());

        // StoredFileName = {Guid}{.ext} — Guid part is 36 chars
        Guid.TryParse(doc.StoredFileName[..36], out var parsedGuid).Should().BeTrue(
            "stored filename must start with a GUID to prevent enumeration and path traversal (V5.3.2)");
        parsedGuid.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("../config/appsettings.json")]
    [InlineData("/etc/shadow")]
    [InlineData("C:\\Windows\\System32\\cmd.exe")]
    public void V5_3_2_PathTraversalInFileName_DoesNotAffectStoredFileName(string maliciousName)
    {
        var doc = new Document(maliciousName, 100, "application/octet-stream", DocCategory.Evidence, Guid.NewGuid());

        // StoredFileName is always {Guid}{.ext} — the user-provided path is never used
        doc.StoredFileName.Should().NotContain("..",       "path traversal sequences must be absent from stored filename (V5.3.2)");
        doc.StoredFileName.Should().NotContain("/etc/",   "absolute Unix paths must be absent (V5.3.2)");
        doc.StoredFileName.Should().NotContain("Windows", "Windows system paths must be absent (V5.3.2)");
    }

    // ── V5.2.2 – File extension is preserved from original (for type checking) ─

    [Theory]
    [InlineData("contract.pdf",  ".pdf")]
    [InlineData("brief.docx",   ".docx")]
    [InlineData("evidence.jpg", ".jpg")]
    [InlineData("notes.txt",    ".txt")]
    public void V5_2_2_StoredFileName_PreservesOriginalExtension(string fileName, string expectedExt)
    {
        var doc = new Document(fileName, 1024, "application/octet-stream", DocCategory.Contract, Guid.NewGuid());

        doc.StoredFileName.Should().EndWith(expectedExt,
            "the original file extension must be preserved so content-type validation can be applied (V5.2.2)");
    }

    // ── V5.1.1 – Each upload generates a unique stored name ───────────────────

    [Fact]
    public void V5_1_1_TwoUploads_WithSameOriginalName_GetDifferentStoredNames()
    {
        var doc1 = new Document("report.pdf", 1024, "application/pdf", DocCategory.Contract, Guid.NewGuid());
        var doc2 = new Document("report.pdf", 1024, "application/pdf", DocCategory.Contract, Guid.NewGuid());

        doc1.StoredFileName.Should().NotBe(doc2.StoredFileName,
            "each upload must receive a unique stored filename to prevent overwrite attacks (V5.1.1)");
    }

    [Fact]
    public async Task V5_1_1_PersistedDocument_StoredFilename_IsUnchangedAfterRetrieval()
    {
        var doc    = new Document("original.pdf", 2048, "application/pdf", DocCategory.Petition, Guid.NewGuid());
        var stored = doc.StoredFileName;
        await _repo.AddAsync(doc, CancellationToken.None);

        var retrieved = await _repo.GetByStoredFileNameAsync(stored, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.StoredFileName.Should().Be(stored,
            "the internally generated stored filename must persist unchanged (V5.1.1)");
    }

    // ── V5.3.2 – FileName field records the original name (for audit) ─────────

    [Fact]
    public void V5_3_2_Document_OriginalFileName_IsStoredSeparately_ForAudit()
    {
        const string original = "client-agreement.pdf";
        var doc = new Document(original, 512, "application/pdf", DocCategory.Contract, Guid.NewGuid());

        // FileName = original user-provided name (audit trail)
        // StoredFileName = safe GUID-based name (storage)
        doc.FileName.Should().Be(original,
            "original filename must be stored for audit purposes (V5.3.2)");
        doc.StoredFileName.Should().NotBe(original,
            "stored filename must differ from original to prevent direct path exposure (V5.3.2)");
    }
}
