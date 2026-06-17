using LawyerApp.API.Controllers;
using LawyerApp.Application.Interfaces.Document;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LawyerApp.API.Aggregates.Documents;

[Route("api/documents")]
[Authorize]
public class DocumentController : ApiController
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload/{processId}")]
    [Authorize(Roles = "Lawyer")]
    public async Task<IActionResult> Upload(Guid processId, IFormFile file, [FromQuery] string category, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _documentService.UploadDocumentAsync(processId, file, category, userId, role, ip, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _documentService.DownloadDocumentAsync(id, userId, role, ip, cancellationToken);
        if (result.IsSuccess)
        {
            return File(result.Value.content, result.Value.contentType, result.Value.fileName);
        }
        return HandleResult(result);
    }

    [HttpGet("process/{processId}")]
    public async Task<IActionResult> GetByProcess(Guid processId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _documentService.GetDocumentsByProcessAsync(processId, userId, role, ip, cancellationToken);
        return HandleResult(result);
    }
}
