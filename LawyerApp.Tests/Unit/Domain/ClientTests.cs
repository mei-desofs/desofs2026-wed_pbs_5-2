using FluentAssertions;
using LawyerApp.Domain.Aggregates.UserAggregate;
using Xunit;

namespace LawyerApp.Tests.Unit.Domain;

public class ClientTests
{
    [Fact]
    public void Client_Constructor_SetsAllProperties()
    {
        var client = new Client("Alice", "alice@example.com", "hash", "Rua A 1", "912000001");

        client.Name.Should().Be("Alice");
        client.Email.Should().Be("alice@example.com");
        client.PasswordHash.Should().Be("hash");
        client.BillingAddress.Should().Be("Rua A 1");
        client.PhoneNumber.Should().Be("912000001");
    }

    [Fact]
    public void Client_CreatedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var client = new Client("Bob", "bob@example.com", "hash", "Rua B", "912000002");
        var after = DateTime.UtcNow.AddSeconds(1);

        client.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void Client_IsSubclassOfUser()
    {
        var client = new Client("Carol", "carol@example.com", "hash", "Rua C", "912000003");

        client.Should().BeAssignableTo<User>();
    }
}

public class LegalProcessTests
{
    [Fact]
    public void LegalProcess_Constructor_GeneratesUniqueGuids()
    {
        var lawyerId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var p1 = new LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess(
            "Case A", "Desc A", lawyerId, clientId);
        var p2 = new LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess(
            "Case B", "Desc B", lawyerId, clientId);

        p1.ProcessId.Should().NotBe(p2.ProcessId);
    }

    [Fact]
    public void LegalProcess_InitialStatus_IsOpen()
    {
        var process = new LawyerApp.Domain.Aggregates.LegalProcessAggregate.LegalProcess(
            "Title", "Desc", Guid.NewGuid(), Guid.NewGuid());

        process.Status.Should().Be(
            LawyerApp.Domain.Aggregates.LegalProcessAggregate.ProcessStatus.Open);
    }
}

public class DocumentTests
{
    [Fact]
    public void Document_Constructor_GeneratesUniqueStoredFileName()
    {
        var legalProcessId = Guid.NewGuid();

        var d1 = new LawyerApp.Domain.Aggregates.DocumentAggregate.Document(
            "contract.pdf", 1024, "application/pdf",
            LawyerApp.Domain.Aggregates.DocumentAggregate.DocCategory.Contract, legalProcessId);
        var d2 = new LawyerApp.Domain.Aggregates.DocumentAggregate.Document(
            "contract.pdf", 1024, "application/pdf",
            LawyerApp.Domain.Aggregates.DocumentAggregate.DocCategory.Contract, legalProcessId);

        d1.StoredFileName.Should().NotBe(d2.StoredFileName);
    }

    [Fact]
    public void Document_StoredFileName_PreservesFileExtension()
    {
        var doc = new LawyerApp.Domain.Aggregates.DocumentAggregate.Document(
            "report.docx", 2048, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            LawyerApp.Domain.Aggregates.DocumentAggregate.DocCategory.Contract, Guid.NewGuid());

        doc.StoredFileName.Should().EndWith(".docx");
    }
}
