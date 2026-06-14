// TODO: REVERT BEFORE MERGING — this file is disabled to unblock DAST (ZAP).
// This controller duplicates [Route("api/client")] and [HttpGet("get/all")] from
// API/Aggregates/User/Client/ClientController.cs, causing Swashbuckle to throw a 500
// on /swagger/v1/swagger.json and preventing ZAP from discovering API endpoints.
// Additionally, this version is missing [Authorize] on GetAll, which is a security issue.
// The duplicate should be reviewed and removed by the team.
#if false
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Application.DTOS.Users;
using Microsoft.AspNetCore.Mvc;

namespace LawyerApp.API
{
    [ApiController]
    [Route("api/client")]
    public class ClientController : ControllerBase
    {
        private readonly ILogger<ClientController> _logger;
        private readonly IClient _clientService;

        public ClientController(ILogger<ClientController> logger, IClient clientService)
        {
            _logger = logger;
            _clientService = clientService;
        }

        [HttpGet("get/all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _clientService.GetAllClientsAsync(cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Post([FromBody] CreateClientDto client, CancellationToken cancellationToken)
        {
            var result = await _clientService.CreateClientAsync(client, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }



    }
}
#endif
