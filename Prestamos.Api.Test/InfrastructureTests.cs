using Dapper;
using Moq;
using Prestamos.Api.Domain.Entities;
using Prestamos.Api.Infraestructura;
using Prestamos.Api.Infraestructura.BD.Context;
using Prestamos.Api.Presenters;
using System.Data;
using System.Dynamic;

namespace Prestamos.Api.Test;

public sealed class InfrastructureTests
{
    [Fact]
    public async Task Bimestres_GetMapsRowsAndHandlesEmptyResult()
    {
        var database = new FakeDatabaseConfig { Rows = [new BimestresEntity { IdBimestre = 2, Mes = "abril", Estatus = 1 }] };
        var result = await new Bimestres(database).Get();
        Assert.Single(result);
        Assert.Equal("ConsultaBimestres", database.LastParameters!.Get<string>("@accion"));
        database.Rows = [];
        Assert.Empty(await new Bimestres(database).Get());
    }

    [Fact]
    public async Task PrestamosContext_PostMapsAllBimestresAndReturnsMessage()
    {
        dynamic row = new ExpandoObject();
        row.mensaje = "guardado";
        var database = new FakeDatabaseConfig { SingleResult = row };
        var request = new PrestamoAnual { IdUsuario = 8, febrero = true, abril = true, junio = true, agosto = true, octubre = true, diciembre = true };
        Assert.Equal("guardado", await new PrestamosContext(database).PostPrestamos([request]));
        Assert.Equal("SolicitarPrestamo", database.LastParameters!.Get<string>("@accion"));
        Assert.True(database.LastParameters.Get<bool>("@diciembre"));
        database.SingleResult = null;
        Assert.Equal("Error al procesar la solicitud", await new PrestamosContext(database).PostPrestamos([request]));
    }

    [Fact]
    public async Task PrestamosContext_GetByUserSendsQueryParameters()
    {
        var database = new FakeDatabaseConfig();
        await new PrestamosContext(database).GetPrestamosUsuario(8);
        Assert.Equal("ConsultarPrestamosUsuario", database.LastParameters!.Get<string>("@accion"));
        Assert.Equal(8, database.LastParameters.Get<int>("@usuario"));
    }

    [Fact]
    public async Task UsuariosRepository_SendsUserQueryParameters()
    {
        var database = new FakeDatabaseConfig();
        await new UsuariosRepository(database).ConsultarInformacionUsuario(11);
        Assert.Equal("ConsultarUsuario", database.LastParameters!.Get<string>("@accion"));
        Assert.Equal(11, database.LastParameters.Get<int>("@usuario"));
    }

    [Fact]
    public void Repository_ExposesInjectedContexts()
    {
        var months = new Mock<IBimestres>().Object;
        var users = new Mock<IUsuarios>().Object;
        var loans = new Mock<IPrestamosContext>().Object;
        var repository = new Repository(months, users, loans);
        Assert.Same(months, repository.ibimestre);
        Assert.Same(users, repository.iusuario);
        Assert.Same(loans, repository.iprestamos);
    }

    [Fact]
    public async Task DatabaseConfig_RejectsInvalidConnectionString()
    {
        var database = new DatabaseConfig("not a valid connection string");
        await Assert.ThrowsAnyAsync<Exception>(() => database.GetData<BimestresEntity>("stored_proc"));
        await Assert.ThrowsAnyAsync<Exception>(() => database.GetSingleData<BimestresEntity>("stored_proc", new DynamicParameters()));
    }

    private sealed class FakeDatabaseConfig : DatabaseConfig
    {
        public FakeDatabaseConfig() : base("not-used") { }
        public IEnumerable<BimestresEntity> Rows { get; set; } = [];
        public object? SingleResult { get; set; }
        public DynamicParameters? LastParameters { get; private set; }

        public override Task<IEnumerable<T>> GetData<T>(string storeProcedure, DynamicParameters? parameters = null)
        {
            LastParameters = parameters;
            return Task.FromResult(Rows.Cast<T>());
        }

        public override Task<T?> GetSingleData<T>(string storeProcedure, DynamicParameters parameters) where T : default
        {
            LastParameters = parameters;
            if (SingleResult is null) return Task.FromResult<T?>(default);
            return Task.FromResult<T?>((T)SingleResult);
        }
    }
}
