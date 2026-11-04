using Dapper;
using System.Data;
using Prestamos.Api.Presenters;

namespace Prestamos.Api.Infraestructura.BD.Context;

public class UsuariosRepository : IUsuarios
{
    private readonly DatabaseConfig _database;

    public UsuariosRepository(DatabaseConfig database)
    {
        _database = database;
    }

    public async Task<dynamic?> ConsultarInformacionUsuario(int idUsuario)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@accion", "ConsultarUsuario", DbType.String, direction: ParameterDirection.Input);
        parameters.Add("@usuario", idUsuario, DbType.Int32, direction: ParameterDirection.Input);

        // Dapper mapea directamente las columnas de SQL a propiedades dinámicas
        return await _database.GetSingleData<dynamic>("acciones_prestamos", parameters);
    }
}