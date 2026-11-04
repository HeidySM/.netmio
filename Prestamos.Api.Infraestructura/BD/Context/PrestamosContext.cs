using Dapper;
using System.Data;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Presenters;

namespace Prestamos.Api.Infraestructura.BD.Context;

public class PrestamosContext : IPrestamosContext
{
    private readonly DatabaseConfig _context;

    public PrestamosContext(DatabaseConfig context)
    {
        _context = context;
    }

    public async Task<string> PostPrestamos(IEnumerable<PrestamoAnual> prestamos)
    {
    var parameters = new DynamicParameters();
    var p = prestamos.First(); 
    
    parameters.Add("@accion", "SolicitarPrestamo", DbType.String, direction: ParameterDirection.Input);
    parameters.Add("@usuario", p.IdUsuario, DbType.Int32, direction: ParameterDirection.Input);
    parameters.Add("@febrero", p.febrero, DbType.Boolean, direction: ParameterDirection.Input);
    parameters.Add("@abril", p.abril, DbType.Boolean, direction: ParameterDirection.Input);
    parameters.Add("@junio", p.junio, DbType.Boolean, direction: ParameterDirection.Input);
    parameters.Add("@agosto", p.agosto, DbType.Boolean, direction: ParameterDirection.Input);
    parameters.Add("@octubre", p.octubre, DbType.Boolean, direction: ParameterDirection.Input);
    parameters.Add("@diciembre", p.diciembre, DbType.Boolean, direction: ParameterDirection.Input);

    var result = await _context.GetSingleData<dynamic>("acciones_prestamos", parameters);
    return result?.mensaje ?? "Error al procesar la solicitud";

    }   

    public async Task<dynamic?> GetPrestamosUsuario(int idUsuario)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@accion", "ConsultarPrestamosUsuario", DbType.String, direction: ParameterDirection.Input);
        parameters.Add("@usuario", idUsuario, DbType.Int32, direction: ParameterDirection.Input);
        return await _context.GetSingleData<dynamic>("acciones_prestamos", parameters);
    }
}