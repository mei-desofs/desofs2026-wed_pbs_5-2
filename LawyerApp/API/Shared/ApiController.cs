using LawyerApp.Domain.Shared;
using LawyerApp.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LawyerApp.API.Controllers
{
    [ApiController]
    public abstract class ApiController : ControllerBase
    {
        // Este método único vai lidar com TUDO na tua API
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                // Se for sucesso mas não houver valor, devolve 404 Not Found
                if (result.Value == null) return NotFound();

                // Caso contrário, devolve 200 OK com os dados
                return Ok(result.Value);
            }

            // Se for falha, devolve 400 Bad Request com o erro formatado
            return BadRequest(new
            {
                errorCode = result.Error.Code,
                message = result.Error.Message
            });
        }
    }
}