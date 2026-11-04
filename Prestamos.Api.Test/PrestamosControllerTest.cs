using Microsoft.AspNetCore.Mvc;
using Moq;
using Prestamos.Api.Controllers;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Presenters;

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

        _controller = new PrestamosController(_mockRepo.Object);
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