using Prestamos.Api.Domain;
using Prestamos.Api.Domain.Entities;

namespace Prestamos.Api.Presenters.CasosUso;

public sealed record UsuarioInfo(int Id, string Nombre, string Tipo, decimal SueldoBase);
public sealed record BimestreInfo(int Id, string Mes, bool Bloqueado, bool Seleccionado = false);

internal static class ReglasPrestamo
{
    internal static readonly Mes[] Meses = [Mes.Febrero, Mes.Abril, Mes.Junio, Mes.Agosto, Mes.Octubre];

    internal static bool EsTrabajador(string? tipo) => tipo is not null &&
        (tipo.Equals("Trabajador", StringComparison.OrdinalIgnoreCase)
         || tipo.Equals("Docente", StringComparison.OrdinalIgnoreCase)
         || tipo.Equals("Administrativo", StringComparison.OrdinalIgnoreCase));

    internal static bool EsEstudiante(string? tipo) => tipo is not null &&
        (tipo.Equals("Alumno", StringComparison.OrdinalIgnoreCase)
         || tipo.Equals("Estudiante", StringComparison.OrdinalIgnoreCase));

    internal static bool Seleccionado(PrestamoAnual p, Mes mes) => mes switch
    {
        Mes.Febrero => p.febrero, Mes.Abril => p.abril, Mes.Junio => p.junio,
        Mes.Agosto => p.agosto, Mes.Octubre => p.octubre, _ => false
    };

    internal static bool TryMes(string? value, out Mes mes) => Enum.TryParse(value, true, out mes);
    internal static int MesNumero(string? value) => TryMes(value, out var mes) ? mes.Numero() : 0;
}

public sealed class IdentificarUsuarioUseCase
{
    public UsuarioInfo? Execute(IDictionary<string, object>? data)
    {
        if (data is null || !data.TryGetValue("codigo", out var code) || Convert.ToInt32(code) != (int)StatusHttp.ok) return null;
        string Text(string key) => data.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
        int Number(string key) => data.TryGetValue(key, out var value) && value is not null ? Convert.ToInt32(value) : 0;
        decimal Amount(string key) => data.TryGetValue(key, out var value) && value is not null ? Convert.ToDecimal(value) : 0m;
        return new UsuarioInfo(Number("idusuario"), Text("nombre"), Text("tipoUsuario"), Amount("sueldoBase"));
    }
}

public sealed class ConsultarInformacionUsuarioUseCase(IdentificarUsuarioUseCase identify)
{
    public UsuarioInfo? Execute(IDictionary<string, object>? data) => identify.Execute(data);
}

public sealed class ObtenerSueldoEmpleadoUseCase
{
    public decimal Execute(UsuarioInfo? user) => ReglasPrestamo.EsTrabajador(user?.Tipo) ? user!.SueldoBase : 0m;
}

public sealed class ObtenerSueldoEstudianteUseCase
{
    public decimal Execute(UsuarioInfo? user) => ReglasPrestamo.EsEstudiante(user?.Tipo) ? user!.SueldoBase : 0m;
}

public sealed class ConsultarBimestresUseCase
{
    public IReadOnlyList<BimestreInfo> Execute(IEnumerable<BimestresEntity> items, DateOnly date) =>
        items.Select(x => new BimestreInfo(x.IdBimestre, x.Mes, ReglasPrestamo.MesNumero(x.Mes) < date.Month)).ToList();
}

public sealed class ValidarSeleccionUseCase
{
    public bool Execute(PrestamoAnual? request) => request is not null &&
        (ReglasPrestamo.Meses.Any(m => ReglasPrestamo.Seleccionado(request, m)) || request.diciembre);
}

public sealed class GuardarSeleccionUseCase
{
    public IReadOnlyList<PrestamoAnual> Execute(IEnumerable<PrestamoAnual>? requests) => requests?.Take(1).ToList() ?? [];
}

public sealed class OcultarDiciembreUseCase
{
    public IReadOnlyList<BimestreInfo> Execute(IEnumerable<BimestreInfo> months, string? userType) =>
        ReglasPrestamo.EsEstudiante(userType)
            ? months.Where(x => !ReglasPrestamo.TryMes(x.Mes, out var month) || month != Mes.Diciembre).ToList()
            : months.ToList();
}

public sealed class ConsultarBimestresPorUsuarioUseCase(ConsultarBimestresUseCase consultar, OcultarDiciembreUseCase ocultar)
{
    public IReadOnlyList<BimestreInfo> Execute(IEnumerable<BimestresEntity> months, IDictionary<string, object>? selection,
        string? userType, DateOnly date)
    {
        var result = consultar.Execute(months, date).Select(x => x with
        {
            Seleccionado = selection is not null && ReglasPrestamo.TryMes(x.Mes, out var month)
                && selection.TryGetValue(month.ToString().ToLowerInvariant(), out var value)
                && Convert.ToBoolean(value)
        });
        return ocultar.Execute(result, userType);
    }
}

public sealed class BloquearBimestrePasadoUseCase
{
    public bool Execute(string? month, DateOnly date) => ReglasPrestamo.MesNumero(month) < date.Month;
}

public sealed class ResetearBimestresUseCase
{
    public IReadOnlyList<PrestamoAnual> Execute(IEnumerable<PrestamoAnual> records) => records.Select(_ => new PrestamoAnual()).ToList();
}

public sealed class ValidarSeleccionBimestreBloqueadoUseCase
{
    public bool Execute(PrestamoAnual request, DateOnly date) =>
        ReglasPrestamo.Meses.Any(m => ReglasPrestamo.Seleccionado(request, m) && m.Numero() < date.Month);
}

public sealed class ActualizarEstadoBimestreUseCase
{
    public bool Execute(int month, DateOnly date) => month >= date.Month;
}

public sealed class CalcularPrestamoPorSueldoUseCase
{
    public decimal Execute(UsuarioInfo user, PrestamoAnual request) => !ReglasPrestamo.EsTrabajador(user.Tipo) ? 0m :
        ReglasPrestamo.Meses.Count(m => ReglasPrestamo.Seleccionado(request, m)) * user.SueldoBase * 0.16m;
}

public sealed class CalcularPrestamoPorAguinaldoUseCase
{
    public decimal Execute(UsuarioInfo user, PrestamoAnual request) =>
        ReglasPrestamo.EsTrabajador(user.Tipo) && request.diciembre ? user.SueldoBase * 0.16m : 0m;
}

public sealed class CalcularPrestamoEstudianteUseCase
{
    public decimal Execute(PrestamoAnual request) =>
        ReglasPrestamo.Meses.Any(m => ReglasPrestamo.Seleccionado(request, m)) ? 3500m : 0m;
}

public sealed class ValidarMontoEstudianteUseCase
{
    public bool Execute(UsuarioInfo user, decimal amount) =>
        !ReglasPrestamo.EsEstudiante(user.Tipo) || amount is >= 0m and <= 3500m;
}

public sealed class ValidarSolicitudDuplicadaUseCase(ValidarSeleccionUseCase validar)
{
    public bool Execute(bool exists, PrestamoAnual request) => exists && validar.Execute(request);
}

public sealed class NotificarMontoUseCase
{
    public string Execute(UsuarioInfo user, decimal amount) => $"{user.Nombre}: monto aprobado {amount:C2}";
}

public sealed class DeshabilitarDiciembreUseCase
{
    public bool Execute(string? userType) => ReglasPrestamo.EsEstudiante(userType);
}

public sealed class CalcularMontoAdicional32UseCase
{
    public decimal Execute(UsuarioInfo user, PrestamoAnual request) =>
        ReglasPrestamo.EsTrabajador(user.Tipo) && request.diciembre ? user.SueldoBase * 0.32m : 0m;
}

public sealed class ActualizarMontoPrestamoUseCase(CalcularPrestamoEstudianteUseCase studentLoan,
    CalcularPrestamoPorSueldoUseCase salaryLoan, CalcularMontoAdicional32UseCase additional)
{
    public decimal Execute(UsuarioInfo user, PrestamoAnual request) => ReglasPrestamo.EsEstudiante(user.Tipo)
        ? studentLoan.Execute(request)
        : salaryLoan.Execute(user, request) + additional.Execute(user, request);
}
