using Microsoft.AspNetCore.Mvc;
using Prestamos.Api.Presenters.CasosUso;

namespace Prestamos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtUseCases _jwt;

    public AuthController(IConfiguration config)
    {
        _jwt = new JwtUseCases(config);
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromForm] string client_id, [FromForm] string client_secret)
    {
        if (!_jwt.ValidarCliente(client_id, client_secret))
            return Unauthorized();

        var token = _jwt.GenerarToken(client_id);
        return Ok(new { access_token = token.AccessToken, token_type = token.Tipo, expires_in = token.ExpiraEn });
    }
}
