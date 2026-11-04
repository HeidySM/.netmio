using Microsoft.Extensions.Configuration;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Presenters.CasosUso;
using System.IdentityModel.Tokens.Jwt;

namespace Prestamos.Api.Test;

public sealed class CasosUsoUnitTests
{
    private static readonly UsuarioInfo Alumna = new(1, "Ana", "Alumno", 0m);
    private static readonly UsuarioInfo Trabajador = new(2, "Luis", "Trabajador", 10000m);
    private static readonly PrestamoAnual Seleccion = new() { IdUsuario = 2, febrero = true, diciembre = true };
    private static readonly DateOnly Marzo = new(2026, 3, 1);
    private static readonly BimestresEntity[] Meses = [new() { IdBimestre = 1, Mes = "febrero" }, new() { IdBimestre = 2, Mes = "diciembre" }];

    [Fact] public void IdentificarUsuario_RechazaNoEncontrado() =>
        Assert.Null(new IdentificarUsuarioUseCase().Execute(new Dictionary<string, object> { ["codigo"] = 404 }));

    [Fact] public void ObtenerSueldoEmpleado_DevuelveSueldoDeTrabajador() =>
        Assert.Equal(10000m, new ObtenerSueldoEmpleadoUseCase().Execute(Trabajador));

    [Fact] public void ObtenerSueldoEstudiante_NoAsignaSueldoLaboral() =>
        Assert.Equal(0m, new ObtenerSueldoEstudianteUseCase().Execute(Alumna));

    [Fact] public void ConsultarBimestres_MarcaPeriodosTranscurridos() =>
        Assert.True(new ConsultarBimestresUseCase().Execute(Meses, Marzo).First().Bloqueado);

    [Fact] public void ValidarSeleccion_RequiereAlMenosUnPeriodo() =>
        Assert.False(new ValidarSeleccionUseCase().Execute(new PrestamoAnual()));

    [Fact] public void GuardarSeleccion_ConservaLaSolicitudActual() =>
        Assert.Same(Seleccion, Assert.Single(new GuardarSeleccionUseCase().Execute([Seleccion])));

    [Fact] public void ConsultarBimestresPorUsuario_DevuelveSeleccionGuardada() =>
        Assert.True(new ConsultarBimestresPorUsuarioUseCase(new(), new()).Execute(
            [Meses[0]], new Dictionary<string, object> { ["febrero"] = true }, "Alumno", new(2026, 1, 1)).Single().Seleccionado);

    [Fact] public void OcultarDiciembre_ExcluyeDiciembreParaEstudiante() =>
        Assert.Single(new OcultarDiciembreUseCase().Execute([new(1, "febrero", false), new(2, "diciembre", false)], "Alumno"));

    [Fact] public void BloquearBimestrePasado_ComparaFechaActual() =>
        Assert.True(new BloquearBimestrePasadoUseCase().Execute("febrero", Marzo));

    [Fact] public void ResetearBimestres_LimpiaLasSelecciones() =>
        Assert.False(Assert.Single(new ResetearBimestresUseCase().Execute([Seleccion])).febrero);

    [Fact] public void ValidarSeleccionBimestreBloqueado_RechazaPeriodoTranscurrido() =>
        Assert.True(new ValidarSeleccionBimestreBloqueadoUseCase().Execute(Seleccion, Marzo));

    [Fact] public void ActualizarEstadoBimestre_ActivaPeriodoVigente() =>
        Assert.True(new ActualizarEstadoBimestreUseCase().Execute(4, Marzo));

    [Fact] public void CalcularPrestamoPorSueldo_CalculaDieciseisPorciento() =>
        Assert.Equal(1600m, new CalcularPrestamoPorSueldoUseCase().Execute(Trabajador, Seleccion));

    [Fact] public void CalcularPrestamoPorAguinaldo_CalculaMontoDeDiciembre() =>
        Assert.Equal(1600m, new CalcularPrestamoPorAguinaldoUseCase().Execute(Trabajador, Seleccion));

    [Fact] public void CalcularPrestamoEstudiante_AplicaTope() =>
        Assert.Equal(3500m, new CalcularPrestamoEstudianteUseCase().Execute(Seleccion));

    [Fact] public void ValidarMontoEstudiante_RechazaMontoSobreTope() =>
        Assert.False(new ValidarMontoEstudianteUseCase().Execute(Alumna, 3500.01m));

    [Fact] public void ValidarSolicitudDuplicada_DetectaRegistroExistente() =>
        Assert.True(new ValidarSolicitudDuplicadaUseCase(new()).Execute(true, Seleccion));

    [Fact] public void NotificarMonto_IncluyeAlUsuario() =>
        Assert.Contains("Luis", new NotificarMontoUseCase().Execute(Trabajador, 100m));

    [Fact] public void DeshabilitarDiciembre_SeAplicaAEstudiantes() =>
        Assert.True(new DeshabilitarDiciembreUseCase().Execute("Alumno"));

    [Fact] public void ConsultarInformacionUsuario_DevuelveUsuarioEncontrado() =>
        Assert.Equal("Ana", new ConsultarInformacionUsuarioUseCase(new()).Execute(
            new Dictionary<string, object> { ["codigo"] = 200, ["nombre"] = "Ana" })?.Nombre);

    [Fact] public void ActualizarMontoPrestamo_CombinaPrestamoYAdicional() =>
        Assert.Equal(4800m, new ActualizarMontoPrestamoUseCase(new(), new(), new()).Execute(Trabajador, Seleccion));

    [Fact] public void CalcularMontoAdicional32_AplicaPorcentajeEnDiciembre() =>
        Assert.Equal(3200m, new CalcularMontoAdicional32UseCase().Execute(Trabajador, Seleccion));

    [Fact]
    public void ReglasPrestamo_CubreTiposAlternativosYSeleccionDeCadaBimestre()
    {
        var employees = new ObtenerSueldoEmpleadoUseCase();
        Assert.Equal(10000m, employees.Execute(new(3, "D", "Docente", 10000m)));
        Assert.Equal(10000m, employees.Execute(new(4, "A", "Administrativo", 10000m)));
        Assert.Equal(0m, employees.Execute(null));
        var students = new ObtenerSueldoEstudianteUseCase();
        Assert.Equal(100m, students.Execute(new(5, "E", "Estudiante", 100m)));
        Assert.Equal(0m, students.Execute(null));

        foreach (var selection in new[]
        {
            new PrestamoAnual { febrero = true }, new PrestamoAnual { abril = true },
            new PrestamoAnual { junio = true }, new PrestamoAnual { agosto = true }, new PrestamoAnual { octubre = true }
        })
        {
            Assert.True(new ValidarSeleccionUseCase().Execute(selection));
            Assert.Equal(3500m, new CalcularPrestamoEstudianteUseCase().Execute(selection));
        }
        Assert.Equal(0m, new CalcularPrestamoEstudianteUseCase().Execute(new()));
        Assert.True(new BloquearBimestrePasadoUseCase().Execute("mes-invalido", Marzo));
    }

    [Fact]
    public void CasosUsuarioYPrestamo_CubrenDatosAusentesYTiposNoElegibles()
    {
        var identify = new IdentificarUsuarioUseCase();
        Assert.Null(identify.Execute(null));
        Assert.Null(identify.Execute(new Dictionary<string, object>()));
        Assert.Null(identify.Execute(new Dictionary<string, object>()));
        var partial = identify.Execute(new Dictionary<string, object> { ["codigo"] = 200, ["nombre"] = null! });
        Assert.NotNull(partial);
        Assert.Equal(0, partial.Id);
        Assert.Equal("", partial.Nombre);
        Assert.Equal(0m, new CalcularPrestamoPorAguinaldoUseCase().Execute(Alumna, Seleccion));
        Assert.Equal(0m, new CalcularPrestamoPorAguinaldoUseCase().Execute(Trabajador, new()));
        Assert.True(new ValidarMontoEstudianteUseCase().Execute(Trabajador, -50m));
        Assert.Equal(0m, new CalcularMontoAdicional32UseCase().Execute(Alumna, Seleccion));
        Assert.False(new DeshabilitarDiciembreUseCase().Execute(null));
        Assert.Equal(2, new OcultarDiciembreUseCase().Execute([new(1, "enero", false), new(2, "diciembre", false)], null).Count);
    }

    [Fact] public void Jwt_ValidaCredencialesDelCliente()
    {
        var jwt = new JwtUseCases(JwtConfig());
        Assert.True(jwt.ValidarCliente("client", "secret"));
        Assert.False(jwt.ValidarCliente("bad", "secret"));
    }

    [Fact] public void Jwt_GeneraTokenConTipoYVencimiento()
    {
        var token = new JwtUseCases(JwtConfig()).GenerarToken("client");
        Assert.Equal("Bearer", token.Tipo);
        Assert.Equal(7200, token.ExpiraEn);
        Assert.Equal("client", new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken).Subject);
    }

    [Fact] public void Jwt_ValidaTokenYAutorizacionDeRol()
    {
        var jwt = new JwtUseCases(JwtConfig());
        var token = jwt.GenerarToken("client");
        var principal = jwt.ValidarToken(token.AccessToken);
        Assert.NotNull(principal);
        Assert.True(jwt.AutorizarRol(principal, "User"));
        Assert.False(jwt.AutorizarRol(principal, "Admin"));
        Assert.False(jwt.AutorizarRol(new System.Security.Claims.ClaimsPrincipal(), "User"));
    }

    [Fact]
    public void Jwt_RechazaCamposIncompletosYConfiguracionFaltante()
    {
        var jwt = new JwtUseCases(JwtConfig());
        Assert.False(jwt.ValidarCliente(null, "secret"));
        Assert.False(jwt.ValidarCliente("client", null));
        Assert.Null(jwt.ValidarToken(" "));
        Assert.Throws<InvalidOperationException>(() => new JwtUseCases(new ConfigurationBuilder().Build()).GenerarToken("x"));
        var issuerOnly = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "a-key-that-is-at-least-32-characters-long", ["Jwt:Issuer"] = "issuer"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new JwtUseCases(issuerOnly).GenerarToken("x"));
        var keyOnly = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "a-key-that-is-at-least-32-characters-long"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new JwtUseCases(keyOnly).GenerarToken("x"));
    }

    private static IConfiguration JwtConfig() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:ClientId"] = "client", ["Jwt:ClientSecret"] = "secret",
        ["Jwt:Key"] = "a-key-that-is-at-least-32-characters-long", ["Jwt:Issuer"] = "issuer", ["Jwt:Audience"] = "audience"
    }).Build();
}
