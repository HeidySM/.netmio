using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestamos.Api.Presenters;
using Prestamos.Api.Domain.Entities;
using System.Dynamic;

namespace Prestamos.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrestamosController : ControllerBase
{
    private readonly IRepository _repository;

    public PrestamosController(IRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("bimestres")]
    public async Task<IActionResult> ConsultarBimestres()
    {
        // Se reemplaza _bimestresRepository por _repository.ibimestre
        var listaBimestres = await _repository.ibimestre.Get();
        var mesActual = DateTime.Now.Month;

        var bimestresProcesados = listaBimestres.Select(b => 
        {
            dynamic obj = new ExpandoObject();
            obj.idBimestre = b.IdBimestre;
            obj.mes = b.Mes;
            obj.estatus = b.Estatus;
            
            int mesNumerico = b.Mes.ToLower() switch {
                "febrero" => 2, "abril" => 4, "junio" => 6,
                "agosto" => 8, "octubre" => 10, "diciembre" => 12, _ => 0
            };
            
            obj.bloqueado = mesNumerico < mesActual;
            return obj;
        });

        dynamic response = new ExpandoObject();
        response.codigo = 200;
        response.mensaje = "Consulta exitosa";
        response.data = bimestresProcesados; 
        return Ok(response);
    }

    [HttpPost("solicitar")]
    public async Task<IActionResult> SolicitarPrestamo([FromBody] List<PrestamoAnual> solicitudes)
    {
        if (solicitudes == null || !solicitudes.Any())
            return BadRequest(new { codigo = 400, mensaje = "La solicitud no puede estar vacía" });

        var peticion = solicitudes.First();
        var mesActual = DateTime.Now.Month;

        if ((peticion.febrero && 2 < mesActual) || (peticion.abril && 4 < mesActual) ||
            (peticion.junio && 6 < mesActual) || (peticion.agosto && 8 < mesActual) ||
            (peticion.octubre && 10 < mesActual))
        {
            return BadRequest(new { codigo = 400, mensaje = "No puedes solicitar un préstamo para un bimestre que ya transcurrió." });
        }

        // Se reemplaza _usuariosRepository por _repository.iusuario
        var resultadoUsuario = await _repository.iusuario.ConsultarInformacionUsuario(peticion.IdUsuario);
        var usuarioDict = resultadoUsuario as IDictionary<string, object>;
        
        if (usuarioDict == null || !usuarioDict.ContainsKey("codigo") || Convert.ToInt32(usuarioDict["codigo"]) == 404) 
            return NotFound(new { codigo = 404, mensaje = "Usuario no encontrado" });

        string tipoUsuario = usuarioDict["tipoUsuario"]?.ToString() ?? "";
        decimal sueldoBase = usuarioDict["sueldoBase"] != null ? Convert.ToDecimal(usuarioDict["sueldoBase"]) : 0;
        
        if (tipoUsuario == "Alumno" && peticion.diciembre)
            return BadRequest(new { codigo = 400, mensaje = "Los alumnos no pueden solicitar préstamo en diciembre." });

        // Se reemplaza _context por _repository.iprestamos
        var mensajeSql = await _repository.iprestamos.PostPrestamos(solicitudes);

        decimal montoTotal = 0;
        int bimestresNormales = (peticion.febrero ? 1 : 0) + (peticion.abril ? 1 : 0) + 
                                (peticion.junio ? 1 : 0) + (peticion.agosto ? 1 : 0) + (peticion.octubre ? 1 : 0);

        if (tipoUsuario == "Alumno")
        {
            montoTotal = bimestresNormales > 0 ? 3500.00m : 0; 
        }
        else if (tipoUsuario == "Trabajador")
        {
            montoTotal = bimestresNormales * (sueldoBase * 0.16m); 
            
            if (peticion.diciembre)
                montoTotal += (sueldoBase * 0.32m); 
        }

        dynamic response = new ExpandoObject();
        response.codigo = 200;
        response.mensaje = mensajeSql;
        response.montoAprobado = montoTotal;

        return Ok(response);
    }

    [HttpGet("usuario/{id}/bimestres")]
    public async Task<IActionResult> ConsultarBimestresPorUsuario(int id)
    {
        // Se reemplaza _usuariosRepository por _repository.iusuario
        var resultadoUsuario = await _repository.iusuario.ConsultarInformacionUsuario(id);
        var usuarioDict = resultadoUsuario as IDictionary<string, object>;

        if (usuarioDict == null || !usuarioDict.ContainsKey("codigo") || Convert.ToInt32(usuarioDict["codigo"]) == 404) 
        {
            return NotFound(new { codigo = 404, mensaje = "Usuario no encontrado" });
        }

        // Se reemplaza _context por _repository.iprestamos
        var seleccionGuardada = await _repository.iprestamos.GetPrestamosUsuario(id) as IDictionary<string, object>;
        
        // Se reemplaza _bimestresRepository por _repository.ibimestre
        var listaBimestres = await _repository.ibimestre.Get();
        var mesActual = DateTime.Now.Month;

        var bimestresProcesados = listaBimestres.Select(b => 
        {
            dynamic obj = new ExpandoObject();
            obj.idBimestre = b.IdBimestre;
            obj.mes = b.Mes;
            
            int mesNumerico = b.Mes.ToLower() switch {
                "febrero" => 2, "abril" => 4, "junio" => 6,
                "agosto" => 8, "octubre" => 10, "diciembre" => 12, _ => 0
            };
            
            obj.bloqueado = mesNumerico < mesActual;
            
            obj.seleccionado = seleccionGuardada != null && 
                               seleccionGuardada.ContainsKey(b.Mes.ToLower()) && 
                               Convert.ToBoolean(seleccionGuardada[b.Mes.ToLower()]);

            return obj;
        }).ToList();

        string tipoUsuarioStr = usuarioDict["tipoUsuario"]?.ToString() ?? "";
        if (tipoUsuarioStr == "Alumno")
        {
            bimestresProcesados.RemoveAll(b => b.mes.ToLower() == "diciembre");
        }

        dynamic response = new ExpandoObject();
        response.codigo = 200;
        response.usuario = usuarioDict["nombre"]?.ToString();
        response.data = bimestresProcesados; 

        return Ok(response);
    }
}