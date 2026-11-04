using Prestamos.Api.Domain.Entities;

namespace Prestamos.Api.Presenters;

public interface IBimestres
{
    Task<List<BimestresEntity>> Get();
}