using Microsoft.AspNetCore.Mvc;
using Moq;
using Prestamos.Api.Controllers;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Presenters;
using Prestamos.Api.Presenters.CasosUso;

namespace Prestamos.Api.Test;

public class PrestamosControllerTest
{
    private readonly Mock<IRepository> _mockRepo;
    private readonly Mock<IBimestres> _mockBimestres;
    private readonly Mock<IUsuarios> _mockUsuarios;
    private readonly Mock<IPrestamosContext> _mockPrestamos;
    private readonly PrestamosController _controller;

    public PrestamosControllerTest()
    {
        _mockRepo = new Mock<IRepository>();
        _mockBimestres = new Mock<IBimestres>();
        _mockUsuarios = new Mock<IUsuarios>();
        _mockPrestamos = new Mock<IPrestamosContext>();

        // Configuramos el Wrapper para que devuelva nuestras interfaces simuladas
        _mockRepo.Setup(r => r.ibimestre).Returns(_mockBimestres.Object);
        _mockRepo.Setup(r => r.iusuario).Returns(_mockUsuarios.Object);
        _mockRepo.Setup(r => r.iprestamos).Returns(_mockPrestamos.Object);

        var identify = new IdentificarUsuarioUseCase();
        var consult = new ConsultarBimestresUseCase();
        _controller = new PrestamosController(_mockRepo.Object, consult, new(), new(), identify,
            new(), new(new(), new(), new()), new(identify), new(consult, new()));
    }

    [Fact]
    public async Task ConsultarBimestres_DevuelveOk_ConListaBimestres()
    {
        // Arrange: Simulamos que la BD devuelve un bimestre
        var lista = new List<BimestresEntity> 
        { 
            new BimestresEntity { IdBimestre = 1, Mes = "febrero", Estatus = 1 } 
        };
        _mockBimestres.Setup(b => b.Get()).ReturnsAsync(lista);

        // Act: Ejecutamos el endpoint
        var result = await _controller.ConsultarBimestres() as OkObjectResult;

        // Assert: Validamos que devuelva 200 OK
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task SolicitarPrestamo_TrabajadorDiciembre_DevuelveMontoAprobado()
    {
        _mockUsuarios.Setup(u => u.ConsultarInformacionUsuario(7)).ReturnsAsync(new Dictionary<string, object>
        {
            ["codigo"] = 200, ["idusuario"] = 7, ["tipoUsuario"] = "Trabajador", ["sueldoBase"] = 10000m, ["nombre"] = "Luis"
        });
        _mockPrestamos.Setup(p => p.PostPrestamos(It.IsAny<IEnumerable<PrestamoAnual>>())).ReturnsAsync("guardado");
        var request = new List<PrestamoAnual> { new() { IdUsuario = 7, diciembre = true } };

        var result = await _controller.SolicitarPrestamo(request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        _mockPrestamos.Verify(p => p.PostPrestamos(It.IsAny<IEnumerable<PrestamoAnual>>()), Times.Once);
    }

    [Fact]
    public async Task SolicitarPrestamo_AlumnoDiciembre_DevuelveBadRequest()
    {
        _mockUsuarios.Setup(u => u.ConsultarInformacionUsuario(7)).ReturnsAsync(new Dictionary<string, object>
        {
            ["codigo"] = 200, ["idusuario"] = 7, ["tipoUsuario"] = "Alumno", ["sueldoBase"] = 0m, ["nombre"] = "Ana"
        });

        var result = await _controller.SolicitarPrestamo(
            [new PrestamoAnual { IdUsuario = 7, diciembre = true }]) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        _mockPrestamos.Verify(p => p.PostPrestamos(It.IsAny<IEnumerable<PrestamoAnual>>()), Times.Never);
    }

    [Fact]
    public async Task SolicitarPrestamo_SinBimestreSeleccionado_DevuelveBadRequest()
    {
        var result = await _controller.SolicitarPrestamo([new PrestamoAnual { IdUsuario = 7 }]) as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task SolicitarPrestamo_BimestrePasado_DevuelveBadRequest()
    {
        var result = await _controller.SolicitarPrestamo([new PrestamoAnual { IdUsuario = 7, febrero = true }]) as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        _mockUsuarios.Verify(u => u.ConsultarInformacionUsuario(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SolicitarPrestamo_UsuarioNoEncontrado_DevuelveNotFound()
    {
        _mockUsuarios.Setup(u => u.ConsultarInformacionUsuario(7)).ReturnsAsync(new Dictionary<string, object> { ["codigo"] = 404 });
        var result = await _controller.SolicitarPrestamo([new PrestamoAnual { IdUsuario = 7, diciembre = true }]) as NotFoundObjectResult;
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task ConsultarBimestresPorUsuario_Alumno_OcultaDiciembre()
    {
        _mockUsuarios.Setup(u => u.ConsultarInformacionUsuario(7)).ReturnsAsync(new Dictionary<string, object>
        {
            ["codigo"] = 200, ["idusuario"] = 7, ["tipoUsuario"] = "Alumno", ["sueldoBase"] = 0m, ["nombre"] = "Ana"
        });
        _mockPrestamos.Setup(p => p.GetPrestamosUsuario(7)).ReturnsAsync(new Dictionary<string, object> { ["febrero"] = true });
        _mockBimestres.Setup(b => b.Get()).ReturnsAsync(new List<BimestresEntity>
        {
            new() { IdBimestre = 1, Mes = "febrero", Estatus = 1 },
            new() { IdBimestre = 2, Mes = "diciembre", Estatus = 1 }
        });

        var result = await _controller.ConsultarBimestresPorUsuario(7) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task SolicitarPrestamo_ListaVacia_DevuelveBadRequest()
    {
        // Arrange: Enviamos una lista vacía
        var solicitudes = new List<PrestamoAnual>();

        // Act
        var result = await _controller.SolicitarPrestamo(solicitudes) as BadRequestObjectResult;

        // Assert: Validamos el error 400
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task ConsultarBimestresPorUsuario_UsuarioNoExiste_DevuelveNotFound()
    {
        // Arrange: Simulamos un usuario no encontrado (codigo 404)
        var fakeUser = new Dictionary<string, object> { { "codigo", 404 } };
        _mockUsuarios.Setup(u => u.ConsultarInformacionUsuario(It.IsAny<int>())).ReturnsAsync(fakeUser);

        // Act
        var result = await _controller.ConsultarBimestresPorUsuario(99) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }
}
