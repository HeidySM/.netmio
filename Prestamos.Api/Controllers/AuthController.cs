using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Dynamic;

namespace Prestamos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromForm] string client_id, [FromForm] string client_secret)
    {
        if (client_id != "f47ac10b-58cc-4372-a567-0e02b2c3d479" || client_secret != "Y2xhdmVfc3VwZXJfc2VjcmV0YV9tdXlfc2VndXJh")
            return Unauthorized();

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, client_id),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        // Uso de clase dinámica para construir la respuesta
        dynamic response = new ExpandoObject();
        response.access_token = new JwtSecurityTokenHandler().WriteToken(token);
        response.token_type = "Bearer";
        response.expires_in = 7200;

        return Ok(response);
    }
}