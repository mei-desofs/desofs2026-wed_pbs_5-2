using LawyerApp.Application.Interfaces.User;
using LawyerApp.Domain.Aggregates.UserAggregate.Dto;
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
        public async Task<IEnumerable<ClientDto>> GetAll()
        {
            var allClients = await _clientService.GetAllClientsAsync();
            return allClients;
        }

        [HttpPost("create")]
        public async Task<ClientDto> Post([FromBody] CreateClientDto client)
        {
            var result = await _clientService.CreateClientAsync(client);
            return result;
        }



    }
}
