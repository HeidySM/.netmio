namespace Prestamos.Api.Presenters;


public interface IUsuarios
{
    Task<dynamic?> ConsultarInformacionUsuario(int idUsuario);
}