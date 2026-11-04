using Prestamos.Domain.Enums;

namespace Prestamos.Domain.Entities;
public class PrestamoEntity
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public decimal MontoSolicitado { get; set; }
    public decimal MontoAprobado { get; set; }
    public EstadoPrestamo Estado { get; set; }
    public DateTime FechaSolicitud { get; set; }
}
