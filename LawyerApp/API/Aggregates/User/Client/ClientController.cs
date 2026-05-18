using LawyerApp.API.Controllers;
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

        [Authorize]
        [HttpGet("get/all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellation)
        {
            var result = await _clientService.GetAllClientsAsync(cancellation);
            return HandleResult(result);
        }
    }
}
