using Prestamos.Api.Presenters;

namespace Prestamos.Api.Infraestructura.BD.Context;

public class Repository : IRepository
{
    public IBimestres ibimestre { get; }
    public IUsuarios iusuario { get; }
    public IPrestamosContext iprestamos { get; }

    public Repository(IBimestres bimestres, IUsuarios usuarios, IPrestamosContext prestamos)
    {
        ibimestre = bimestres;
        iusuario = usuarios;
        iprestamos = prestamos;
    }
}