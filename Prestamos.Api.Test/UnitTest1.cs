using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Prestamos.Api.Test;

public class AuthControllerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Token_Success()
    {
        var client = _factory.CreateClient();
        var form = new Dictionary<string, string>
        {
            {"client_id", "f47ac10b-58cc-4372-a567-0e02b2c3d479"},
            {"client_secret", "Y2xhdmVfc3VwZXJfc2VjcmV0YV9tdXlfc2VndXJh"}
        };
        var content = new FormUrlEncodedContent(form);

        var response = await client.PostAsync("api/auth/token", content);

        response.EnsureSuccessStatusCode();
        Assert.True((int)response.StatusCode == 200);
    }

    [Fact]
    public async Task Create_Token_Fail()
    {
        var client = _factory.CreateClient();
        var form = new Dictionary<string, string>
        {
            {"client_id", "usuario_invalido"},
            {"client_secret", "clave_incorrecta"}
        };
        var content = new FormUrlEncodedContent(form);

        var response = await client.PostAsync("api/auth/token", content);

        Assert.True((int)response.StatusCode == 401);
    }
}