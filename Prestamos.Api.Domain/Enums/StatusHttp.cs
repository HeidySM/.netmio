namespace Prestamos.Api.Domain;

/// <summary>Códigos HTTP tipados para las respuestas de la API.</summary>
public enum StatusHttp
{
    ok = 200,
    created = 201,
    badRequest = 400,
    unauthorized = 401,
    notFound = 404,
    conflict = 409,
    serverError = 500
}
