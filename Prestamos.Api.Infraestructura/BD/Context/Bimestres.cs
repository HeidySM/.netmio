using Dapper;
using System.Data;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Presenters;

namespace Prestamos.Api.Infraestructura.BD.Context;
public class Bimestres : IBimestres
{
    private readonly DatabaseConfig _database;

    public Bimestres(DatabaseConfig database)
    {
        _database = database;
    }

    public async Task<List<BimestresEntity>> Get()
    {
        List<BimestresEntity> bimestres = new List<BimestresEntity>();
        
        var parameters = new DynamicParameters();
        parameters.Add("@accion", "ConsultaBimestres", DbType.String, direction: ParameterDirection.Input);

        var atributos = await _database.GetData<BimestresEntity>("acciones_prestamos", parameters);
        
        if (atributos.Any())
        {
            atributos.ToList().ForEach(b => bimestres.Add(b));
        }

        return bimestres;
    }
}