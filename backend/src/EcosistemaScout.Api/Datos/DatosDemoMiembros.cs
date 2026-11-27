using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base;
using EcosistemaScout.Api.Modulos.Miembros;
using EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;
using EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Datos;

/// <summary>
/// Datos ficticios del módulo Miembros para desarrollo y demostraciones: unidades por rama con sus patrullas,
/// 20 beneficiarios con RUT válido e historial, apoderados, contactos, algunas fichas médicas (cifradas) y voluntarios
/// con cargos. Solo se cargan junto con los usuarios demo (nunca en producción) y una sola vez.
/// Las personas son inventadas: cualquier parecido con personas reales es coincidencia.
/// </summary>
public class DatosDemoMiembros(AppDbContext db, CifradoFichaMedica cifrado, ILogger<DatosDemoMiembros> log)
{
    /// <summary>Si existe esta unidad, los datos demo ya se cargaron.</summary>
    public const string UnidadMarcador = "Tropa Lautaro";

    private record UnidadDemo(string Nombre, string Rama, int EdadMinima, string[] Patrullas);

    private static readonly UnidadDemo[] Unidades =
    [
        new("Bandada Picaflores", "golondrinas", 7, ["Seisena Azul", "Seisena Amarilla"]),
        new("Manada Seeonee", "lobatos", 7, ["Seisena Gris", "Seisena Café"]),
        new("Compañía Copihue", "guias", 11, ["Patrulla Gaviotas", "Patrulla Alerces"]),
        new(UnidadMarcador, "scouts", 11, ["Patrulla Cóndores", "Patrulla Pumas"]),
        new("Avanzada Los Andes", "pioneros", 15, ["Equipo Volcán"]),
        new("Clan Rucamanque", "caminantes", 17, []),
    ];

    // (número de RUT, nombres, apellidos, índice de unidad, índice de patrulla o -1, años sobre la edad mínima)
    private static readonly (int Rut, string Nombres, string Apellidos, int Unidad, int Patrulla, int Edad)[] Beneficiarios =
    [
        (25_102_331, "Isidora", "Muñoz Fuentes", 0, 0, 0),
        (25_340_118, "Agustina", "Rojas Pino", 0, 1, 1),
        (24_877_902, "Florencia", "Soto Cárdenas", 0, 0, 2),
        (25_011_457, "Mateo", "González Vera", 1, 0, 0),
        (24_903_276, "Benjamín", "Díaz Muñoz", 1, 1, 1),
        (24_655_380, "Vicente", "Pérez Lagos", 1, 0, 2),
        (24_512_649, "Tomás", "Contreras Silva", 1, 1, 3),
        (23_884_105, "Emilia", "Sepúlveda Ríos", 2, 0, 0),
        (23_560_772, "Josefa", "Morales Castro", 2, 1, 1),
        (23_201_934, "Antonia", "Díaz Muñoz", 2, 0, 2),
        (22_987_416, "Martina", "Espinoza Rivas", 2, 1, 3),
        (23_745_208, "Joaquín", "Torres Molina", 3, 0, 0),
        (23_310_867, "Lucas", "Fernández Araya", 3, 1, 1),
        (22_906_543, "Maximiliano", "Vargas Núñez", 3, 0, 2),
        (22_640_391, "Cristóbal", "Reyes Tapia", 3, 1, 3),
        (22_415_028, "Diego", "Herrera Bravo", 3, 0, 3),
        (21_903_682, "Catalina", "Gutiérrez Paredes", 4, 0, 0),
        (21_570_314, "Ignacio", "Castillo Mella", 4, 0, 2),
        (20_985_147, "Fernanda", "Jara Olivares", 5, -1, 1),
        (20_448_793, "Sebastián", "Navarro Leiva", 5, -1, 3),
    ];

    // (número de RUT, nombres, apellidos, correo, teléfono, beneficiarios a cargo [índice, parentesco, principal])
    private static readonly (int Rut, string Nombres, string Apellidos, string Email, string Telefono, (int Hijo, string Parentesco, bool Principal)[] Hijos)[] Apoderados =
    [
        (15_234_871, "Carolina", "Muñoz Díaz", "apoderado@demo.cl", "+56 9 5123 4401", [(4, "Madre", true), (9, "Madre", true)]),
        (14_876_302, "Rodrigo", "Díaz Pinto", "rodrigo.diaz@demo.cl", "+56 9 5123 4402", [(4, "Padre", false), (9, "Padre", false)]),
        (16_102_554, "Paulina", "Fuentes Mora", "paulina.fuentes@demo.cl", "+56 9 5123 4403", [(0, "Madre", true)]),
        (15_789_013, "Andrés", "Rojas Ibarra", "andres.rojas@demo.cl", "+56 9 5123 4404", [(1, "Padre", true)]),
        (16_430_998, "Valentina", "Cárdenas Soto", "valentina.cardenas@demo.cl", "+56 9 5123 4405", [(2, "Madre", true)]),
        (13_998_245, "Marcela", "Vera Lillo", "marcela.vera@demo.cl", "+56 9 5123 4406", [(3, "Madre", true)]),
        (14_220_671, "Claudio", "Lagos Ortiz", "claudio.lagos@demo.cl", "+56 9 5123 4407", [(5, "Padre", true)]),
        (15_007_436, "Ximena", "Silva Rebolledo", "ximena.silva@demo.cl", "+56 9 5123 4408", [(6, "Madre", true)]),
        (13_765_120, "Patricio", "Ríos Gallardo", "patricio.rios@demo.cl", "+56 9 5123 4409", [(7, "Padre", true)]),
        (16_890_327, "Daniela", "Castro Figueroa", "daniela.castro@demo.cl", "+56 9 5123 4410", [(8, "Madre", true)]),
        (12_654_789, "Gloria", "Molina Saavedra", "gloria.molina@demo.cl", "+56 9 5123 4411", [(11, "Abuela", true)]),
        (15_543_216, "Felipe", "Araya Cortés", "felipe.araya@demo.cl", "+56 9 5123 4412", [(12, "Padre", true)]),
    ];

    // (número de RUT, nombres, apellidos, correo, cargos [cargo, índice de unidad o -1, años desde el inicio, años de duración o -1 si vigente])
    private static readonly (int Rut, string Nombres, string Apellidos, string Email, (string Cargo, int Unidad, int Desde, int Duracion)[] Cargos)[] Voluntarios =
    [
        (12_345_670, "Verónica", "Salinas Ortega", "veronica.salinas@demo.cl", [("Responsable de grupo", -1, 3, -1), ("Responsable de unidad", 2, 7, 4)]),
        (16_789_241, "Sergio", "Paredes Lillo", "dirigente@demo.cl", [("Responsable de unidad", 3, 2, -1)]),
        (17_456_903, "Camilo", "Bustos Henríquez", "camilo.bustos@demo.cl", [("Asistente de unidad", 3, 1, -1)]),
        (18_102_667, "Javiera", "Poblete Arancibia", "javiera.poblete@demo.cl", [("Responsable de unidad", 1, 2, -1)]),
        (17_830_154, "Nicolás", "Riquelme Vidal", "nicolas.riquelme@demo.cl", [("Responsable de unidad", 0, 1, -1), ("Apoyo", -1, 3, 2)]),
        (14_567_398, "Alejandra", "Zúñiga Campos", "alejandra.zuniga@demo.cl", [("Tesorería", -1, 4, -1)]),
    ];

    public async Task CargarAsync()
    {
        if (await db.Unidades.AnyAsync(u => u.Nombre == UnidadMarcador))
        {
            return;
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // Unidades y patrullas
        var unidades = Unidades.Select(u => new Unidad
        {
            Nombre = u.Nombre,
            Rama = u.Rama,
            Patrullas = u.Patrullas.Select(p => new Patrulla { Nombre = p }).ToList(),
        }).ToList();
        db.Unidades.AddRange(unidades);
        await db.SaveChangesAsync();

        // Beneficiarios con su ingreso en el historial
        var beneficiarios = new List<Beneficiario>();
        for (var i = 0; i < Beneficiarios.Length; i++)
        {
            var (rut, nombres, apellidos, iUnidad, iPatrulla, edad) = Beneficiarios[i];
            var unidad = unidades[iUnidad];
            var anios = Unidades[iUnidad].EdadMinima + edad;
            var ingreso = hoy.AddYears(-Math.Min(edad + 1, 3)).AddDays(-(i * 11 % 90));
            var beneficiario = new Beneficiario
            {
                Rut = $"{rut}-{Rut.CalcularDv(rut)}",
                Nombres = nombres,
                Apellidos = apellidos,
                FechaNacimiento = hoy.AddYears(-anios).AddDays(-(i * 37 % 300) - 10),
                Direccion = $"Pasaje Los Aromos {120 + i * 7}, Temuco",
                UnidadId = unidad.Id,
                PatrullaId = iPatrulla >= 0 ? unidad.Patrullas.ElementAt(iPatrulla).Id : null,
                FechaIngreso = ingreso,
                RegistroInstitucionalAnio = (short)hoy.Year,
                Estado = EstadosBeneficiario.Activo,
            };
            beneficiarios.Add(beneficiario);
            db.HistorialBeneficiario.Add(new HistorialBeneficiario
            {
                // El beneficiario 15 entró a la Manada y luego pasó a la Tropa (ver más abajo).
                Beneficiario = beneficiario, Tipo = TiposHistorial.Ingreso, Fecha = ingreso,
                UnidadDestinoId = i == 15 ? unidades[1].Id : unidad.Id,
            });
        }
        db.Beneficiarios.AddRange(beneficiarios);

        // Un paso de unidad (de la Manada a la Tropa) y un retiro, para que el historial tenga ejemplos.
        var paso = beneficiarios[15];
        db.HistorialBeneficiario.Add(new HistorialBeneficiario
        {
            Beneficiario = paso, Tipo = TiposHistorial.CambioUnidad, Fecha = hoy.AddMonths(-8),
            UnidadOrigenId = unidades[1].Id, UnidadDestinoId = unidades[3].Id, Detalle = "Paso de la Manada a la Tropa.",
        });
        var retirado = beneficiarios[17];
        retirado.Estado = EstadosBeneficiario.Retirado;
        db.HistorialBeneficiario.Add(new HistorialBeneficiario
        {
            Beneficiario = retirado, Tipo = TiposHistorial.Retiro, Fecha = hoy.AddMonths(-2),
            UnidadOrigenId = retirado.UnidadId, Detalle = "Cambio de ciudad.",
        });
        await db.SaveChangesAsync();

        // Apoderados y vínculos (la cuenta apoderado@demo.cl queda vinculada a su apoderado)
        var usuariosDemo = await db.Users.Where(u => u.Email!.EndsWith("@demo.cl")).ToDictionaryAsync(u => u.Email!, u => u.Id);
        foreach (var (rut, nombres, apellidos, email, telefono, hijos) in Apoderados)
        {
            var apoderado = new Apoderado
            {
                Rut = $"{rut}-{Rut.CalcularDv(rut)}",
                Nombres = nombres,
                Apellidos = apellidos,
                Email = email,
                Telefono = telefono,
                UsuarioId = usuariosDemo.GetValueOrDefault(email) is var id and > 0 ? id : null,
            };
            foreach (var (hijo, parentesco, principal) in hijos)
            {
                apoderado.Beneficiarios.Add(new BeneficiarioApoderado
                {
                    BeneficiarioId = beneficiarios[hijo].Id, Parentesco = parentesco, EsPrincipal = principal,
                });
            }
            db.Apoderados.Add(apoderado);
        }

        // Contactos de emergencia para la mitad de los beneficiarios
        (string Nombre, string Parentesco)[] contactos =
        [
            ("Rosa Fuentes", "Abuela"), ("Mónica Sáez", "Tía"), ("Hernán Toro", "Tío"), ("Lidia Campos", "Vecina"), ("Raúl Ibáñez", "Padrino"),
        ];
        for (var i = 0; i < beneficiarios.Count; i += 2)
        {
            var (nombre, parentesco) = contactos[i / 2 % contactos.Length];
            db.ContactosEmergencia.Add(new ContactoEmergencia
            {
                BeneficiarioId = beneficiarios[i].Id,
                Nombre = nombre,
                Parentesco = parentesco,
                Telefono = $"+56 9 6{i:00}0 {1000 + i * 37}",
            });
        }

        // Fichas médicas (cifradas)
        (int Indice, string Prevision, string Grupo, string? Alergias, string? Enfermedades, string? Medicamentos, string? Dieta, string? Emocional)[] fichas =
        [
            (0, "Fonasa", "O+", "Penicilina", null, null, null, "Le cuesta dormir fuera de casa; ayuda tener una linterna."),
            (4, "Isapre", "A+", null, "Asma leve", "Inhalador salbutamol antes de actividad física intensa", null, null),
            (9, "Isapre", "A+", "Picadura de abeja", null, "Lleva adrenalina autoinyectable", null, null),
            (11, "Fonasa", "B+", null, null, null, "Celíaco: alimentos sin gluten", null),
            (13, "Fonasa", "O-", "Mariscos", null, null, null, null),
            (16, "Isapre", "AB+", null, "Diabetes tipo 1", "Insulina según indicación", "Controlar azúcar en colaciones", null),
        ];
        foreach (var f in fichas)
        {
            db.FichasMedicas.Add(new FichaMedica
            {
                BeneficiarioId = beneficiarios[f.Indice].Id,
                Prevision = cifrado.Cifrar(f.Prevision),
                GrupoSanguineo = cifrado.Cifrar(f.Grupo),
                Alergias = cifrado.Cifrar(f.Alergias),
                Enfermedades = cifrado.Cifrar(f.Enfermedades),
                Medicamentos = cifrado.Cifrar(f.Medicamentos),
                RestriccionesAlimentarias = cifrado.Cifrar(f.Dieta),
                NecesidadesEmocionales = cifrado.Cifrar(f.Emocional),
            });
        }

        // Voluntarios y cargos (la cuenta dirigente@demo.cl queda vinculada a su voluntario)
        var numero = 0;
        foreach (var (rut, nombres, apellidos, email, cargos) in Voluntarios)
        {
            numero++;
            db.Voluntarios.Add(new Voluntario
            {
                Rut = $"{rut}-{Rut.CalcularDv(rut)}",
                Nombres = nombres,
                Apellidos = apellidos,
                Email = email,
                Telefono = $"+56 9 7{numero:00}4 55{numero:00}",
                UsuarioId = usuariosDemo.GetValueOrDefault(email) is var id and > 0 ? id : null,
                Cargos = cargos.Select(c => new CargoVoluntario
                {
                    Cargo = c.Cargo,
                    UnidadId = c.Unidad >= 0 ? unidades[c.Unidad].Id : null,
                    FechaInicio = hoy.AddYears(-c.Desde),
                    FechaTermino = c.Duracion >= 0 ? hoy.AddYears(-c.Desde + c.Duracion) : null,
                }).ToList(),
            });
        }

        await db.SaveChangesAsync();

        // Dos fichas con su último cambio hace 5 meses, para que la alerta de ficha desactualizada se vea en la demo.
        // (SaveChanges siempre pone la fecha actual, por eso se ajusta con SQL; las pruebas usan una base en memoria).
        if (db.Database.IsRelational())
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE fichas_medicas SET updated_at = now() - interval '5 months' WHERE beneficiario_id IN ({beneficiarios[11].Id}, {beneficiarios[13].Id})");
        }
        log.LogInformation("Datos demo de Miembros cargados: {Unidades} unidades y {Beneficiarios} beneficiarios.",
            unidades.Count, beneficiarios.Count);
    }
}
