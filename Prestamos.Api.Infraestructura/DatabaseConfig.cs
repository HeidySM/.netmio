using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Prestamos.Api.Infraestructura;

public class DatabaseConfig
{
    private readonly string _connectionString;

    public DatabaseConfig(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection Conexion() => new SqlConnection(_connectionString);

    public async Task<T?> GetSingleData<T>(string storeProcedure, DynamicParameters parameters)
    {
        using (var conect = Conexion())
        {
            return await conect.QueryFirstOrDefaultAsync<T>(
                storeProcedure, 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }
    }

    public async Task<IEnumerable<T>> GetData<T>(string storeProcedure, DynamicParameters? parameters = null)
    {
        using (var conect = Conexion())
        {
            return await conect.QueryAsync<T>(
                storeProcedure, 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }
    }
}