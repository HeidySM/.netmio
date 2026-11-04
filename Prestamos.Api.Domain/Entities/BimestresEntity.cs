namespace Prestamos.Api.Domain.Entities;

public class BimestresEntity
{
    public int IdBimestre { get; set; }
    public string Mes { get; set; } = string.Empty;
    public int Estatus { get; set; }
}

public class PrestamoAnual 
{
    public int IdUsuario { get; set; }
    public bool febrero { get; set; }
    public bool abril { get; set; }
    public bool junio { get; set; }
    public bool agosto { get; set; }
    public bool octubre { get; set; }
    public bool diciembre { get; set; }
}
