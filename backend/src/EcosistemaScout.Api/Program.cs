using System.Text;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Configuracion;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base;
using EcosistemaScout.Api.Modulos.Base.Autenticacion;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// En producción, no permitir los valores de desarrollo que vienen en el repositorio.
ValidacionProduccion.Verificar(builder.Configuration, builder.Environment);

// ----- Usuario de la petición y bitácora de auditoría -----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, ServicioUsuarioActual>();
builder.Services.AddScoped<InterceptorBitacora>();

// ----- Base de datos (PostgreSQL) -----
builder.Services.AddDbContext<AppDbContext>((servicios, opciones) =>
{
    var conexion = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(conexion))
    {
        throw new InvalidOperationException(
            "Falta la cadena de conexión 'ConnectionStrings:Default'. Configúrala con user-secrets o variables de entorno (ver README).");
    }
    opciones.UseNpgsql(conexion).UseSnakeCaseNamingConvention()
        .AddInterceptors(servicios.GetRequiredService<InterceptorBitacora>());
});

// ----- Cifrado de datos sensibles (ficha médica) -----
// Las llaves se guardan en la base de datos para que sobrevivan a reinicios del contenedor.
builder.Services.AddDataProtection()
    .SetApplicationName("Tali")
    .PersistKeysToDbContext<AppDbContext>();
builder.Services.AddSingleton<CifradoFichaMedica>();

// ----- Usuarios y roles (ASP.NET Identity) -----
builder.Services
    .AddIdentityCore<Usuario>(opciones =>
    {
        opciones.User.RequireUniqueEmail = true;
        opciones.Password.RequiredLength = 8;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<Rol>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<ErroresIdentityEspanol>();

// ----- Autenticación con JWT -----
builder.Services.Configure<JwtOpciones>(builder.Configuration.GetSection(JwtOpciones.Seccion));
var jwt = builder.Configuration.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>() ?? new JwtOpciones();
if (jwt.Clave.Length < 32)
{
    throw new InvalidOperationException(
        "Falta 'Jwt:Clave' o tiene menos de 32 caracteres. Configúrala con user-secrets o variables de entorno (ver README).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization(Politicas.Registrar);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ServicioTokens>();
builder.Services.AddScoped<DatosIniciales>();

// ----- CORS para el frontend Angular -----
const string PoliticaFrontend = "frontend";
var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddPolicy(PoliticaFrontend, p => p
    .WithOrigins(origenes)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Filtro global: con contraseña temporal solo se puede cambiar la contraseña.
builder.Services.AddControllers(o => o.Filters.Add<FiltroCambioPassword>());
builder.Services.AddOpenApi();
builder.Services.AgregarManejoErrores();

var app = builder.Build();

// ----- Migraciones y datos iniciales -----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Configuration.GetValue("Database:MigrarAlIniciar", true))
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }
    }

    var usuariosDemo = app.Configuration.GetValue("Seed:UsuariosDemo", false);
    await scope.ServiceProvider.GetRequiredService<DatosIniciales>().CargarAsync(usuariosDemo);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Todas las respuestas de error en formato ResponseData (excepciones, 401, 403, 404...)
app.UsarManejoErrores();
app.UseCors(PoliticaFrontend);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Visible para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
