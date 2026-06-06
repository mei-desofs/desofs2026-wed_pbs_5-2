using LawyerApp.API.Controllers;
using LawyerApp.Application.Interfaces.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LawyerApp.API.Aggregates.Audit
{
    [ApiController]
    [Route("api/audit")]
    [Authorize(Roles = "Lawyer,LegalAssistant")]
    public class AuditController : ApiController
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet("process/{processId}")]
        public async Task<IActionResult> GetByProcess(Guid processId, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;

            var result = await _auditService.GetLogsByProcessAsync(processId, userId, role, cancellationToken);
            return HandleResult(result);
        }
    }
}
