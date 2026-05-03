using LawyerApp.API.Controllers;
using LawyerApp.Application.DTOS.Auth;
using LawyerApp.Application.DTOS.Users;
using LawyerApp.Application.Interfaces.Login;
using LawyerApp.Application.Interfaces.User;
using LawyerApp.Application.Services.UserAggregate;
using Microsoft.AspNetCore.Mvc;

namespace LawyerApp.API.Login
{
    [ApiController]
    [Route("api/auth")]
    public class LoginController : ApiController
    {

        private readonly ILogger<LoginController> _logger;
        private readonly ILogin _loginService;
        private readonly IClient _clientService;

        public LoginController(ILogger<LoginController> logger, ILogin loginService, IClient clientService)
        {
            _logger = logger;
            _loginService = loginService;
            _clientService = clientService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateClientDto createClientDto,CancellationToken cancellationToken)
        {
            var result = await _clientService.CreateClientAsync(createClientDto, CancellationToken.None);
            return HandleResult(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDto,CancellationToken cancellationToken)
        {
            var result = await _loginService.Login(loginDto.Email, loginDto.Password, cancellationToken);
            return HandleResult(result);
        }
    }
}
