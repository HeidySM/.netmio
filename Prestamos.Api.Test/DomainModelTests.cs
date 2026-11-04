using Prestamos.Api.Domain;
using Prestamos.Api.Domain.Entities;
using Prestamos.Domain.Entities;
using Prestamos.Domain.Enums;

namespace Prestamos.Api.Test;

public sealed class DomainModelTests
{
    [Fact]
    public void Mes_ValuesMatchCalendarAndNumeroHelper()
    {
        Assert.Equal(1, Mes.Enero.Numero());
        Assert.Equal(12, Mes.Diciembre.Numero());
        Assert.Equal(12, Enum.GetValues<Mes>().Length);
    }

    [Fact]
    public void PrestamoAnual_StoresEachBimonthlySelection()
    {
        var entity = new PrestamoAnual { IdUsuario = 1, febrero = true, abril = true, junio = true, agosto = true, octubre = true, diciembre = true };
        Assert.True(entity.febrero && entity.abril && entity.junio && entity.agosto && entity.octubre && entity.diciembre);
        Assert.Equal(1, entity.IdUsuario);
    }

    [Fact]
    public void PrestamoEntity_StoresLoanData()
    {
        var date = DateTime.UtcNow;
        var entity = new PrestamoEntity { Id = 1, UsuarioId = 2, MontoSolicitado = 100m, MontoAprobado = 80m, Estado = EstadoPrestamo.Aprobado, FechaSolicitud = date };
        Assert.Equal((1, 2, 100m, 80m, EstadoPrestamo.Aprobado, date),
            (entity.Id, entity.UsuarioId, entity.MontoSolicitado, entity.MontoAprobado, entity.Estado, entity.FechaSolicitud));
    }

    [Fact]
    public void UsuarioEntity_StoresAccountAndStatusData()
    {
        var entity = new UsuarioEntity { Id = 1, NombreCompleto = "Ana", Correo = "ana@example.com", PasswordHash = "hash", Tipo = TipoUsuario.Estudiante, Estado = EstadoUsuario.Activo };
        Assert.Equal((1, "Ana", "ana@example.com", "hash", TipoUsuario.Estudiante, EstadoUsuario.Activo),
            (entity.Id, entity.NombreCompleto, entity.Correo, entity.PasswordHash, entity.Tipo, entity.Estado));
    }
}
