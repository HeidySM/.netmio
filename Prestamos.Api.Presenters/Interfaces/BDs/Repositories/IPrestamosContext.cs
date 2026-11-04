using Prestamos.Api.Domain.Entities;

namespace Prestamos.Api.Presenters;

public interface IPrestamosContext
{
    Task<string> PostPrestamos(IEnumerable<PrestamoAnual> prestamos);
    Task<dynamic?> GetPrestamosUsuario(int idUsuario);
}