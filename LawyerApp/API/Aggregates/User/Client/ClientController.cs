using LawyerApp.API.Controllers;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawyerApp.API.Aggregates.User.Client
{
    [ApiController]
    [Route("api/client")]
    public class ClientController : ApiController
    {
        private readonly ILogger<ClientController> _logger;
        private readonly IClient _clientService;

        public ClientController(ILogger<ClientController> logger, IClient clientService)
        {
            _logger = logger;
            _clientService = clientService;
        }

        // Listing all clients exposes PII, so it is restricted to authenticated callers.
        [Authorize]
        [HttpGet("get/all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellation)
        {
            var result = await _clientService.GetAllClientsAsync(cancellation);
            return HandleResult(result);
        }

        // Public client self-registration. The Client role is fixed in the domain
        // (Client constructor sets Roles.Client), so this endpoint cannot be used
        // to create privileged accounts. Mirrors POST /api/auth/register.
        [HttpPost("create")]
        public async Task<IActionResult> Post([FromBody] CreateClientDto client, CancellationToken cancellationToken)
        {
            var result = await _clientService.CreateClientAsync(client, cancellationToken);
            return HandleResult(result);
        }
    }
}
