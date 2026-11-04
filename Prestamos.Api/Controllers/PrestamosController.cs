using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestamos.Api.Domain;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Presenters;
using Prestamos.Api.Presenters.CasosUso;

namespace Prestamos.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrestamosController : ControllerBase
{
    private readonly IRepository _repository;
    private readonly ConsultarBimestresUseCase _consultar;
    private readonly ValidarSeleccionUseCase _validarSeleccion;
    private readonly ValidarSeleccionBimestreBloqueadoUseCase _validarBloqueado;
    private readonly IdentificarUsuarioUseCase _identificar;
    private readonly DeshabilitarDiciembreUseCase _deshabilitarDiciembre;
    private readonly ActualizarMontoPrestamoUseCase _actualizarMonto;
    private readonly ConsultarInformacionUsuarioUseCase _consultarUsuario;
    private readonly ConsultarBimestresPorUsuarioUseCase _consultarUsuarioBimestres;

    public PrestamosController(IRepository repository, ConsultarBimestresUseCase consultar,
        ValidarSeleccionUseCase validarSeleccion, ValidarSeleccionBimestreBloqueadoUseCase validarBloqueado,
        IdentificarUsuarioUseCase identificar, DeshabilitarDiciembreUseCase deshabilitarDiciembre,
        ActualizarMontoPrestamoUseCase actualizarMonto, ConsultarInformacionUsuarioUseCase consultarUsuario,
        ConsultarBimestresPorUsuarioUseCase consultarUsuarioBimestres)
    {
        _repository = repository;
        _consultar = consultar;
        _validarSeleccion = validarSeleccion;
        _validarBloqueado = validarBloqueado;
        _identificar = identificar;
        _deshabilitarDiciembre = deshabilitarDiciembre;
        _actualizarMonto = actualizarMonto;
        _consultarUsuario = consultarUsuario;
        _consultarUsuarioBimestres = consultarUsuarioBimestres;
    }

    [HttpGet("bimestres")]
    public async Task<IActionResult> ConsultarBimestres()
    {
        var rows = await _repository.ibimestre.Get();
        var result = _consultar.Execute(rows, DateOnly.FromDateTime(DateTime.Now))
            .Select((b, i) => new { idBimestre = b.Id, mes = b.Mes, estatus = rows[i].Estatus, bloqueado = b.Bloqueado });
        return Ok(new { type = TipoRecurso.Bimestres, codigo = (int)StatusHttp.ok, mensaje = "Consulta exitosa", data = result });
    }

    [HttpPost("solicitar")]
    public async Task<IActionResult> SolicitarPrestamo([FromBody] List<PrestamoAnual>? solicitudes)
    {
        if (solicitudes is null || solicitudes.Count == 0)
            return BadRequest(new { type = TipoRecurso.Validacion, codigo = (int)StatusHttp.badRequest, mensaje = "La solicitud no puede estar vacía" });

        var request = solicitudes[0];
        if (!_validarSeleccion.Execute(request))
            return BadRequest(new { type = TipoRecurso.Validacion, codigo = (int)StatusHttp.badRequest, mensaje = "Selecciona al menos un bimestre." });
        if (_validarBloqueado.Execute(request, DateOnly.FromDateTime(DateTime.Now)))
            return BadRequest(new { type = TipoRecurso.Validacion, codigo = (int)StatusHttp.badRequest, mensaje = "No puedes solicitar un préstamo para un bimestre que ya transcurrió." });

        var userData = await _repository.iusuario.ConsultarInformacionUsuario(request.IdUsuario) as IDictionary<string, object>;
        var user = _identificar.Execute(userData);
        if (user is null)
            return NotFound(new { type = TipoRecurso.Usuarios, codigo = (int)StatusHttp.notFound, mensaje = "Usuario no encontrado" });
        if (_deshabilitarDiciembre.Execute(user.Tipo) && request.diciembre)
            return BadRequest(new { type = TipoRecurso.Validacion, codigo = (int)StatusHttp.badRequest, mensaje = "Los alumnos no pueden solicitar préstamo en diciembre." });

        var message = await _repository.iprestamos.PostPrestamos(solicitudes);
        var amount = _actualizarMonto.Execute(user, request);
        return Ok(new { type = TipoRecurso.Solicitud, codigo = (int)StatusHttp.ok, mensaje = message, montoAprobado = amount });
    }

    [HttpGet("usuario/{id}/bimestres")]
    public async Task<IActionResult> ConsultarBimestresPorUsuario(int id)
    {
        var userData = await _repository.iusuario.ConsultarInformacionUsuario(id) as IDictionary<string, object>;
        var user = _consultarUsuario.Execute(userData);
        if (user is null)
            return NotFound(new { type = TipoRecurso.Usuarios, codigo = (int)StatusHttp.notFound, mensaje = "Usuario no encontrado" });

        var saved = await _repository.iprestamos.GetPrestamosUsuario(id) as IDictionary<string, object>;
        var months = await _repository.ibimestre.Get();
        var result = _consultarUsuarioBimestres.Execute(months, saved, user.Tipo,
            DateOnly.FromDateTime(DateTime.Now)).Select(b => new
            {
                idBimestre = b.Id,
                mes = b.Mes,
                bloqueado = b.Bloqueado,
                seleccionado = b.Seleccionado
            }).ToArray();
        return Ok(new
        {
            type = TipoRecurso.Bimestres,
            codigo = (int)StatusHttp.ok,
            usuario = user.Nombre,
            data = result
        });
    }
}
