using Prestamos.Api.Presenters;
using Prestamos.Api.Infraestructura;
using Prestamos.Api.Infraestructura.BD.Context;
using Prestamos.Api.Presenters.CasosUso;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Prestamos Bimestrales API",
        Version = "v1",
        Description = "API para consultar bimestres y gestionar solicitudes de préstamos."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el token JWT. Swagger enviará el encabezado Authorization: Bearer <token>."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddSingleton(new DatabaseConfig(connectionString));
builder.Services.AddScoped<IPrestamosContext, PrestamosContext>();
builder.Services.AddScoped<IBimestres, Bimestres>();
builder.Services.AddScoped<IUsuarios, UsuariosRepository>();
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<ConsultarBimestresUseCase>();
builder.Services.AddScoped<ValidarSeleccionUseCase>();
builder.Services.AddScoped<ValidarSeleccionBimestreBloqueadoUseCase>();
builder.Services.AddScoped<IdentificarUsuarioUseCase>();
builder.Services.AddScoped<DeshabilitarDiciembreUseCase>();
builder.Services.AddScoped<CalcularPrestamoEstudianteUseCase>();
builder.Services.AddScoped<CalcularPrestamoPorSueldoUseCase>();
builder.Services.AddScoped<CalcularMontoAdicional32UseCase>();
builder.Services.AddScoped<ActualizarMontoPrestamoUseCase>();
builder.Services.AddScoped<ConsultarInformacionUsuarioUseCase>();
builder.Services.AddScoped<OcultarDiciembreUseCase>();
builder.Services.AddScoped<ConsultarBimestresPorUsuarioUseCase>();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Prestamos Bimestrales API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
public partial class Program { }
