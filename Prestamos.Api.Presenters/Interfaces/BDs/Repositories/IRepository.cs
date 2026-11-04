namespace Prestamos.Api.Presenters;


public interface IRepository
{
    public IBimestres ibimestre { get; }
        public IUsuarios iusuario { get; }
    public IPrestamosContext iprestamos { get; }
}