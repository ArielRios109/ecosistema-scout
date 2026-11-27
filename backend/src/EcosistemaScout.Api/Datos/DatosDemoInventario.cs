using EcosistemaScout.Api.Modulos.Inventario;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Datos;

/// <summary>
/// Inventario ficticio para desarrollo: equipamiento típico de un grupo scout, algunos préstamos (uno vencido)
/// y una baja. Se carga junto con los usuarios demo, después de Miembros, y una sola vez.
/// </summary>
public class DatosDemoInventario(AppDbContext db, ILogger<DatosDemoInventario> log)
{
    /// <summary>Si existe esta categoría, el inventario demo ya se cargó.</summary>
    public const string CategoriaMarcador = "Campamento";

    // (categoría, nombre, cantidad, marca, origen, estado, ubicación, valor estimado)
    private static readonly (string Categoria, string Nombre, int Cantidad, string? Marca, string Origen, string Estado, string Ubicacion, int Valor)[] Bienes =
    [
        (CategoriaMarcador, "Carpa iglú 4 personas", 6, "Doite", "propio", "bueno", "Bodega del grupo", 89_990),
        (CategoriaMarcador, "Carpa comedor 3x3 m", 2, "Coleman", "donacion", "regular", "Bodega del grupo", 120_000),
        (CategoriaMarcador, "Toldo de lona 4x6 m", 3, null, "propio", "bueno", "Bodega del grupo", 45_000),
        ("Cocina", "Olla de aluminio 20 L", 4, "Tramontina", "propio", "bueno", "Bodega del grupo", 32_000),
        ("Cocina", "Cocinilla a gas 2 platos", 2, "Ursus Trotter", "propio", "regular", "Bodega del grupo", 54_990),
        ("Cocina", "Bidón de agua 20 L", 6, null, "propio", "bueno", "Bodega del grupo", 8_990),
        ("Pionerismo", "Cuerda de escalada 30 m", 4, "Edelrid", "propio", "bueno", "Sala de la Tropa", 79_990),
        ("Pionerismo", "Rollizo de pino 2,5 m", 20, null, "donacion", "bueno", "Patio de la sede", 4_500),
        ("Herramientas", "Hacha de mano", 3, "Tramontina", "propio", "bueno", "Sala de la Tropa", 24_990),
        ("Herramientas", "Pala plegable", 4, null, "propio", "malo", "Bodega del grupo", 12_990),
        ("Primeros auxilios", "Botiquín de campamento", 3, null, "propio", "bueno", "Oficina de dirigentes", 35_000),
        ("Ceremonial", "Banderín de la Tropa", 1, null, "propio", "bueno", "Sala de la Tropa", 60_000),
    ];

    public async Task CargarAsync()
    {
        if (await db.CategoriasBien.AnyAsync(c => c.Nombre == CategoriaMarcador))
        {
            return;
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var categorias = Bienes.Select(b => b.Categoria).Distinct().ToDictionary(n => n, n => new CategoriaBien { Nombre = n });
        db.CategoriasBien.AddRange(categorias.Values);

        // Responsable: el voluntario vinculado a la cuenta del dirigente demo, si existe.
        var responsable = await db.Voluntarios.Where(v => v.Email == "dirigente@demo.cl").Select(v => (long?)v.Id).FirstOrDefaultAsync();
        var registrador = await db.Users.Where(u => u.Email == "tesoreria@demo.cl").Select(u => (long?)u.Id).FirstOrDefaultAsync()
            ?? await db.Users.Select(u => u.Id).FirstAsync();

        var bienes = Bienes.Select((b, i) => new Bien
        {
            Codigo = $"INV-{i + 1:D5}",
            Nombre = b.Nombre,
            Categoria = categorias[b.Categoria],
            Cantidad = b.Cantidad,
            Marca = b.Marca,
            Origen = b.Origen,
            Estado = b.Estado,
            Ubicacion = b.Ubicacion,
            ValorEstimado = b.Valor,
            PrecioCompra = b.Origen == "propio" ? b.Valor : null,
            FechaAdquisicion = hoy.AddMonths(-6 - i * 3),
            ResponsableId = b.Ubicacion == "Sala de la Tropa" ? responsable : null,
        }).ToList();
        db.Bienes.AddRange(bienes);

        db.Prestamos.AddRange(
            new Prestamo
            {
                Bien = bienes[0], Cantidad = 3, Destinatario = "Tropa Lautaro", Actividad = "Campamento de verano",
                FechaSalida = hoy.AddDays(-3), FechaDevolucionEsperada = hoy.AddDays(4), RegistradoPor = registrador,
            },
            new Prestamo
            {
                Bien = bienes[6], Cantidad = 2, Destinatario = "Avanzada Los Andes", Actividad = "Salida de montaña",
                FechaSalida = hoy.AddDays(-20), FechaDevolucionEsperada = hoy.AddDays(-6), RegistradoPor = registrador,
            },
            new Prestamo
            {
                Bien = bienes[3], Cantidad = 2, Destinatario = "Manada Seeonee", Actividad = "Acantonamiento",
                FechaSalida = hoy.AddMonths(-2), FechaDevolucionEsperada = hoy.AddMonths(-2).AddDays(3),
                FechaDevolucion = hoy.AddMonths(-2).AddDays(3), EstadoDevolucion = "bueno", RegistradoPor = registrador,
            });

        bienes[9].DadoDeBaja = true;
        db.BajasBien.Add(new BajaBien
        {
            Bien = bienes[9], Motivo = "deterioro", Fecha = hoy.AddMonths(-1),
            Detalle = "Mango quebrado, no se puede reparar.", RegistradoPor = registrador,
        });

        await db.SaveChangesAsync();
        log.LogInformation("Datos demo de Inventario cargados: {Bienes} bienes.", bienes.Count);
    }
}
