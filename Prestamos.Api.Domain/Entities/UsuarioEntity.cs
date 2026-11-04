using Prestamos.Domain.Enums;

namespace Prestamos.Domain.Entities;

public class UsuarioEntity
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public TipoUsuario Tipo { get; set; }
    public EstadoUsuario Estado { get; set; }
}
