using FluentAssertions;
using LawyerApp.Domain.Aggregates.DocumentAggregate;
using LawyerApp.Domain.Aggregates.LegalProcessAggregate;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Shared;
using Xunit;

namespace LawyerApp.Tests.Unit.Domain;

public class LawyerTests
{
    [Fact]
    public void Lawyer_Constructor_SetsAllProperties()
    {
        var lawyer = new Lawyer("Dave", "dave@firm.com", "hash", "OA/12345");

        lawyer.Name.Should().Be("Dave");
        lawyer.Email.Should().Be("dave@firm.com");
        lawyer.PasswordHash.Should().Be("hash");
        lawyer.LicenseNumber.Should().Be("OA/12345");
    }

    [Fact]
    public void Lawyer_Role_IsLawyer()
    {
        var lawyer = new Lawyer("Eve", "eve@firm.com", "hash", "OA/99999");

        lawyer.userRole.Should().Be(Roles.Lawyer);
    }

    [Fact]
    public void Lawyer_IsSubclassOfUser()
    {
        var lawyer = new Lawyer("Frank", "frank@firm.com", "hash", "OA/11111");

        lawyer.Should().BeAssignableTo<User>();
    }

    [Fact]
    public void Lawyer_CreatedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var lawyer = new Lawyer("Grace", "grace@firm.com", "hash", "OA/22222");
        var after = DateTime.UtcNow.AddSeconds(1);

        lawyer.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }
}

public class LegalAssistantTests
{
    [Fact]
    public void LegalAssistant_Constructor_SetsAllProperties()
    {
        var assistant = new LegalAssistant("Hank", "hank@firm.com", "hash", "EMP-001");

        assistant.Name.Should().Be("Hank");
        assistant.Email.Should().Be("hank@firm.com");
        assistant.PasswordHash.Should().Be("hash");
        assistant.EmployeeId.Should().Be("EMP-001");
    }

    [Fact]
    public void LegalAssistant_Role_IsLegalAssistant()
    {
        var assistant = new LegalAssistant("Ida", "ida@firm.com", "hash", "EMP-002");

        assistant.userRole.Should().Be(Roles.LegalAssistant);
    }

    [Fact]
    public void LegalAssistant_IsSubclassOfUser()
    {
        var assistant = new LegalAssistant("Jack", "jack@firm.com", "hash", "EMP-003");

        assistant.Should().BeAssignableTo<User>();
    }
}

public class ClientRoleTests
{
    [Fact]
    public void Client_Role_IsClient()
    {
        var client = new Client("Kim", "kim@test.com", "hash", "Rua K", "910000010");

        client.userRole.Should().Be(Roles.Client);
    }
}

public class LegalProcessExtendedTests
{
    [Fact]
    public void LegalProcess_OpenedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var process = new LegalProcess("Title", "Desc", Guid.NewGuid(), Guid.NewGuid());
        var after = DateTime.UtcNow.AddSeconds(1);

        process.OpenedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void LegalProcess_Constructor_AssignsLawyerAndClientIds()
    {
        var lawyerId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var process = new LegalProcess("Title", "Desc", lawyerId, clientId);

        process.LawyerId.Should().Be(lawyerId);
        process.ClientId.Should().Be(clientId);
    }

    [Fact]
    public void LegalProcess_Constructor_AssignsTitleAndDescription()
    {
        var process = new LegalProcess("My Case", "Case description", Guid.NewGuid(), Guid.NewGuid());

        process.Title.Should().Be("My Case");
        process.Description.Should().Be("Case description");
    }

    [Fact]
    public void LegalProcess_ProcessId_IsNotEmpty()
    {
        var process = new LegalProcess("T", "D", Guid.NewGuid(), Guid.NewGuid());

        process.ProcessId.Should().NotBeEmpty();
    }

    [Fact]
    public void LegalProcess_StatusCanBeUpdatedToInAnalysis()
    {
        var process = new LegalProcess("T", "D", Guid.NewGuid(), Guid.NewGuid());

        process.Status = ProcessStatus.InAnalysis;

        process.Status.Should().Be(ProcessStatus.InAnalysis);
    }

    [Fact]
    public void LegalProcess_StatusCanBeUpdatedToClosed()
    {
        var process = new LegalProcess("T", "D", Guid.NewGuid(), Guid.NewGuid());

        process.Status = ProcessStatus.Closed;

        process.Status.Should().Be(ProcessStatus.Closed);
    }
}

public class DocumentExtendedTests
{
    [Fact]
    public void Document_Constructor_SetsFileNameAndSize()
    {
        var doc = new Document("file.pdf", 4096, "application/pdf", DocCategory.Petition, Guid.NewGuid());

        doc.FileName.Should().Be("file.pdf");
        doc.FileSize.Should().Be(4096);
    }

    [Fact]
    public void Document_Constructor_SetsContentType()
    {
        var doc = new Document("file.txt", 100, "text/plain", DocCategory.Correspondence, Guid.NewGuid());

        doc.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void Document_Constructor_SetsCategory()
    {
        var doc = new Document("evidence.jpg", 2048, "image/jpeg", DocCategory.Evidence, Guid.NewGuid());

        doc.Category.Should().Be(DocCategory.Evidence);
    }

    [Fact]
    public void Document_Constructor_SetsLegalProcessId()
    {
        var processId = Guid.NewGuid();
        var doc = new Document("order.pdf", 1024, "application/pdf", DocCategory.CourtOrder, processId);

        doc.LegalProcessId.Should().Be(processId);
    }

    [Fact]
    public void Document_UploadedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var doc = new Document("f.pdf", 512, "application/pdf", DocCategory.Contract, Guid.NewGuid());
        var after = DateTime.UtcNow.AddSeconds(1);

        doc.UploadedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void Document_StoredFileName_IsNotEmpty()
    {
        var doc = new Document("report.pdf", 1024, "application/pdf", DocCategory.Contract, Guid.NewGuid());

        doc.StoredFileName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Document_StoredFileName_ContainsGuidSegment()
    {
        var doc = new Document("contract.pdf", 1024, "application/pdf", DocCategory.Contract, Guid.NewGuid());

        // StoredFileName = "{Guid}{.ext}" — the Guid part is 36 chars
        doc.StoredFileName.Length.Should().BeGreaterThan(36);
    }
}
