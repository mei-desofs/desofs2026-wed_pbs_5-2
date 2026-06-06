using LawyerApp.API.Controllers;
using LawyerApp.Application.DTOS.LegalProcess;
using LawyerApp.Application.Interfaces.LegalProcess;
using LawyerApp.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LawyerApp.API.Aggregates.LegalProcess;

[Route("api/processes")]
[Authorize]
public class LegalProcessController : ApiController
{
    private readonly ILegalProcessService _processService;

    public LegalProcessController(ILegalProcessService processService)
    {
        _processService = processService;
    }

    [HttpPost]
    [Authorize(Roles = "Lawyer")]
    public async Task<IActionResult> Create([FromBody] CreateLegalProcessDto dto, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await _processService.CreateProcessAsync(dto, userId, role, ip, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _processService.GetProcessByIdAsync(id, userId, role, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _processService.GetAllProcessesAsync(userId, role, cancellationToken);
        return HandleResult(result);
    }
}
