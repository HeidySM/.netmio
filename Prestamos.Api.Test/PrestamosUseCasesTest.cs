using Microsoft.Extensions.Configuration;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Presenters.CasosUso;
using System.Security.Claims;

namespace Prestamos.Api.Test;

public class PrestamosUseCasesTest
{
    private static UsuarioInfo Alumno => new(1, "Ana", "Alumno", 0m);
    private static UsuarioInfo Trabajador => new(2, "Luis", "Trabajador", 10000m);
    private static PrestamoAnual Seleccion => new() { IdUsuario = 2, febrero = true, diciembre = true };

    [Fact]
    public void IdentificarUsuario_ParsesValidAndRejectsMissingOrNotFound()
    {
        var identify = new IdentificarUsuarioUseCase();
        Assert.Null(identify.Execute(null));
        Assert.Null(identify.Execute(new Dictionary<string, object> { ["codigo"] = 404 }));
        var user = identify.Execute(new Dictionary<string, object>
        {
            ["codigo"] = 200, ["idusuario"] = 5, ["nombre"] = "Eva", ["tipoUsuario"] = "Alumno", ["sueldoBase"] = 0m
        });
        Assert.Equal("Eva", user?.Nombre);
        Assert.Equal(0m, new ObtenerSueldoEmpleadoUseCase().Execute(user));
        Assert.Equal(0m, new ObtenerSueldoEstudianteUseCase().Execute(user));
        Assert.Equal(10000m, new ObtenerSueldoEmpleadoUseCase().Execute(Trabajador));
    }

    [Fact]
    public void SeleccionAndBimestres_ApplyAvailabilityAndRoleRules()
    {
        var validate = new ValidarSeleccionUseCase();
        Assert.False(validate.Execute(null));
        Assert.False(validate.Execute(new PrestamoAnual()));
        Assert.True(validate.Execute(Seleccion));
        Assert.True(new BloquearBimestrePasadoUseCase().Execute("febrero", new DateOnly(2026, 3, 1)));
        Assert.False(new BloquearBimestrePasadoUseCase().Execute("abril", new DateOnly(2026, 3, 1)));
        Assert.True(new ValidarSeleccionBimestreBloqueadoUseCase().Execute(Seleccion, new DateOnly(2026, 3, 1)));
        Assert.False(new ValidarSeleccionBimestreBloqueadoUseCase().Execute(new PrestamoAnual { abril = true }, new DateOnly(2026, 3, 1)));
        Assert.False(new ActualizarEstadoBimestreUseCase().Execute(2, new DateOnly(2026, 3, 1)));
        Assert.True(new ActualizarEstadoBimestreUseCase().Execute(4, new DateOnly(2026, 3, 1)));

        var months = new[] { new BimestresEntity { IdBimestre = 1, Mes = "febrero" }, new BimestresEntity { IdBimestre = 2, Mes = "diciembre" } };
        var consult = new ConsultarBimestresUseCase();
        Assert.Equal(2, consult.Execute(months, new DateOnly(2026, 3, 1)).Count);
        Assert.Single(new OcultarDiciembreUseCase().Execute(consult.Execute(months, new DateOnly(2026, 1, 1)), "Alumno"));
        Assert.Equal(2, new OcultarDiciembreUseCase().Execute(consult.Execute(months, new DateOnly(2026, 1, 1)), "Trabajador").Count);
        Assert.Single(new GuardarSeleccionUseCase().Execute([Seleccion]));
        Assert.Empty(new GuardarSeleccionUseCase().Execute(null));
        Assert.Single(new ResetearBimestresUseCase().Execute([Seleccion]));
    }

    [Fact]
    public void LoanCalculations_EnforceStudentCapAndWorkerRates()
    {
        var salary = new CalcularPrestamoPorSueldoUseCase();
        Assert.Equal(1600m, salary.Execute(Trabajador, Seleccion));
        Assert.Equal(3200m, new CalcularMontoAdicional32UseCase().Execute(Trabajador, Seleccion));
        Assert.Equal(1600m, new CalcularPrestamoPorAguinaldoUseCase().Execute(Trabajador, Seleccion));
        var total = new ActualizarMontoPrestamoUseCase(new(), salary, new());
        Assert.Equal(4800m, total.Execute(Trabajador, Seleccion));
        Assert.Equal(3500m, total.Execute(Alumno, Seleccion));
        Assert.Equal(0m, salary.Execute(Alumno, Seleccion));
        Assert.True(new ValidarMontoEstudianteUseCase().Execute(Alumno, 3500m));
        Assert.False(new ValidarMontoEstudianteUseCase().Execute(Alumno, 3500.01m));
        Assert.True(new ValidarMontoEstudianteUseCase().Execute(Trabajador, 5000m));
        Assert.True(new DeshabilitarDiciembreUseCase().Execute("Estudiante"));
        Assert.False(new DeshabilitarDiciembreUseCase().Execute("Trabajador"));
        Assert.Contains("Ana", new NotificarMontoUseCase().Execute(Alumno, 3500m));
        Assert.True(new ValidarSolicitudDuplicadaUseCase(new()).Execute(true, Seleccion));
        Assert.False(new ValidarSolicitudDuplicadaUseCase(new()).Execute(false, Seleccion));
    }

    [Fact]
    public void ConsultarBimestresUsuario_MapsStoredSelection()
    {
        var rows = new ConsultarBimestresPorUsuarioUseCase(new(), new()).Execute(
            [new BimestresEntity { IdBimestre = 1, Mes = "febrero" }],
            new Dictionary<string, object> { ["febrero"] = true }, "Alumno", new DateOnly(2026, 1, 1));
        Assert.True(rows.Single().Seleccionado);
        Assert.Equal("Alumno", new ConsultarInformacionUsuarioUseCase(new()).Execute(
            new Dictionary<string, object> { ["codigo"] = 200, ["tipoUsuario"] = "Alumno" })?.Tipo);
    }
}

public class JwtUseCasesTest
{
    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:ClientId"] = "client", ["Jwt:ClientSecret"] = "secret", ["Jwt:Key"] = "a-key-that-is-at-least-32-characters-long",
        ["Jwt:Issuer"] = "issuer", ["Jwt:Audience"] = "audience"
    }).Build();

    [Fact]
    public void ClientValidation_RequiresMatchingCredentials()
    {
        var jwt = new JwtUseCases(Config());
        Assert.True(jwt.ValidarCliente("client", "secret"));
        Assert.False(jwt.ValidarCliente("bad", "secret"));
        Assert.False(jwt.ValidarCliente(null, "secret"));
    }

    [Fact]
    public void GeneratedToken_ValidatesAndAuthorizesRole()
    {
        var jwt = new JwtUseCases(Config());
        var token = jwt.GenerarToken("client");
        Assert.Equal("Bearer", token.Tipo);
        Assert.Equal(7200, token.ExpiraEn);
        var principal = jwt.ValidarToken(token.AccessToken);
        Assert.NotNull(principal);
        Assert.True(jwt.AutorizarRol(principal, "User"));
        Assert.False(jwt.AutorizarRol(principal, "Admin"));
        Assert.Null(jwt.ValidarToken("invalid"));
        Assert.Null(jwt.ValidarToken(null));
        Assert.False(jwt.AutorizarRol(null, "User"));
    }

    [Fact]
    public void ValidarToken_RechazaTokenFirmadoConOtraClave()
    {
        var jwt = new JwtUseCases(Config());
        var otherConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "another-key-that-is-at-least-32-characters",
            ["Jwt:Issuer"] = "issuer", ["Jwt:Audience"] = "audience"
        }).Build();
        var invalid = new JwtUseCases(otherConfiguration).GenerarToken("client");
        Assert.Null(jwt.ValidarToken(invalid.AccessToken));
    }
}
