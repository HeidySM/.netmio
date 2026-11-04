using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Prestamos.Api.Presenters.CasosUso;

public sealed record TokenGenerado(string AccessToken, string Tipo, int ExpiraEn);

/// <summary>Casos de uso para credenciales de cliente y tokens JWT.</summary>
public sealed class JwtUseCases(IConfiguration configuration)
{
    private const string DefaultClientId = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
    private const string DefaultClientSecret = "Y2xhdmVfc3VwZXJfc2VjcmV0YV9tdXlfc2VndXJh";

    public bool ValidarCliente(string? clientId, string? clientSecret) =>
        !string.IsNullOrWhiteSpace(clientId)
        && !string.IsNullOrWhiteSpace(clientSecret)
        && clientId == (configuration["Jwt:ClientId"] ?? DefaultClientId)
        && clientSecret == (configuration["Jwt:ClientSecret"] ?? DefaultClientSecret);

    public TokenGenerado GenerarToken(string clientId)
    {
        var keyText = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Falta Jwt:Issuer");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Falta Jwt:Audience");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience,
            [new Claim(JwtRegisteredClaimNames.Sub, clientId), new Claim(ClaimTypes.Role, "User")],
            expires: DateTime.UtcNow.AddHours(2), signingCredentials: credentials);
        return new TokenGenerado(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", 7200);
    }

    public ClaimsPrincipal? ValidarToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var keyText = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(keyText)) return null;
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch (SecurityTokenException) { return null; }
        catch (ArgumentException) { return null; }
    }

    public bool AutorizarRol(ClaimsPrincipal? principal, string rol) =>
        principal?.Identity?.IsAuthenticated == true && principal.IsInRole(rol);
}
