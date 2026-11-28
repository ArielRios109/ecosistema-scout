using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using EcosistemaScout.Api.Modulos.Miembros.Apoderados;
using EcosistemaScout.Api.Modulos.Miembros.Contactos;
using EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;

/// <summary>Estados del beneficiario y tipos de movimiento del historial (iguales a las restricciones CHECK).</summary>
public static class EstadosBeneficiario
{
    public const string Activo = "activo";
    public const string Retirado = "retirado";
}

public static class TiposHistorial
{
    public const string Ingreso = "ingreso";
    public const string CambioUnidad = "cambio_unidad";
    public const string CambioPatrulla = "cambio_patrulla";
    public const string Retiro = "retiro";
    public const string Reingreso = "reingreso";
}

/// <summary>Reglas de edad del PGS: a los 18 corresponde preparar el egreso y desde los 21 ya no se participa.</summary>
public static class Edades
{
    public const int Egreso = 18;
    public const int Maxima = 21;

    /// <summary>"egreso" entre 18 y 20 años, "edad-maxima" desde los 21, null si no hay aviso.</summary>
    public static string? Aviso(int edad) => edad >= Maxima ? "edad-maxima" : edad >= Egreso ? "egreso" : null;
}

public class FiltroBeneficiarios : ConsultaPaginada
{
    public long? UnidadId { get; set; }
    public long? PatrullaId { get; set; }
    /// <summary>activo o retirado; vacío = todos.</summary>
    public string? Estado { get; set; }
}

public record BeneficiarioResumen(
    long Id, string Rut, string Nombres, string Apellidos, DateOnly FechaNacimiento, int Edad,
    long UnidadId, string Unidad, long? PatrullaId, string? Patrulla, string Estado, DateOnly FechaIngreso,
    string? AvisoEdad, string EstadoFicha);

public record BeneficiarioDetalle(
    long Id, string Rut, string Nombres, string Apellidos, DateOnly FechaNacimiento, int Edad, string? Direccion,
    long UnidadId, string Unidad, string Rama, long? PatrullaId, string? Patrulla, string Estado, DateOnly FechaIngreso,
    short? RegistroInstitucionalAnio, string? AvisoEdad, string EstadoFicha);

public record MovimientoHistorial(
    long Id, string Tipo, DateOnly Fecha, string? UnidadOrigen, string? UnidadDestino, string? Detalle, string? Usuario);

public record SolicitudBeneficiario(
    [Required(ErrorMessage = "Ingresa el RUT.")][RutValido] string Rut,
    [Required(ErrorMessage = "Ingresa los nombres.")][MaxLength(80, ErrorMessage = "Los nombres admiten hasta 80 caracteres.")] string Nombres,
    [Required(ErrorMessage = "Ingresa los apellidos.")][MaxLength(80, ErrorMessage = "Los apellidos admiten hasta 80 caracteres.")] string Apellidos,
    DateOnly FechaNacimiento,
    [MaxLength(200, ErrorMessage = "La dirección admite hasta 200 caracteres.")] string? Direccion,
    long UnidadId,
    long? PatrullaId,
    DateOnly? FechaIngreso,
    [Range(1900, 2100, ErrorMessage = "El año de registro institucional no es válido.")] short? RegistroInstitucionalAnio);

public record SolicitudMovimiento(
    DateOnly? Fecha,
    [MaxLength(255, ErrorMessage = "El detalle admite hasta 255 caracteres.")] string? Detalle,
    long? UnidadId = null,
    long? PatrullaId = null);

/// <summary>Apoderado de la inscripción: uno ya registrado (ApoderadoId) o uno nuevo (Nuevo), nunca ambos.</summary>
public record ApoderadoInscripcion(
    long? ApoderadoId,
    SolicitudApoderado? Nuevo,
    [Required(ErrorMessage = "Indica el parentesco del apoderado.")][MaxLength(40, ErrorMessage = "El parentesco admite hasta 40 caracteres.")] string Parentesco,
    bool EsPrincipal);

/// <summary>Inscripción completa de un beneficiario: sus datos, apoderados, contactos y ficha médica (opcional).</summary>
public record SolicitudInscripcion(
    [Required(ErrorMessage = "Faltan los datos del beneficiario.")] SolicitudBeneficiario Beneficiario,
    IReadOnlyList<ApoderadoInscripcion>? Apoderados,
    IReadOnlyList<SolicitudContacto>? Contactos,
    SolicitudFichaMedica? FichaMedica);

/// <summary>
/// Beneficiarios (niños, niñas y jóvenes). Los gestionan administración y dirigentes.
/// Cada ingreso, cambio de unidad o patrulla, retiro y reingreso queda en el historial automáticamente.
/// Un beneficiario nunca se borra: se retira.
/// </summary>
[ApiController]
[Route("api/miembros/beneficiarios")]
[Authorize(Policy = Politicas.Miembros)]
public class BeneficiariosController(AppDbContext db, IUsuarioActual usuarioActual, CifradoFichaMedica cifrado) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] FiltroBeneficiarios filtro, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && filtro.Estado is not (EstadosBeneficiario.Activo or EstadosBeneficiario.Retirado))
        {
            return BadRequest(ResponseData.Error("El estado debe ser activo o retirado."));
        }

        var consulta = db.Beneficiarios.AsNoTracking();
        if (filtro.BuscarNormalizado is { } texto)
        {
            var rut = texto.Replace(".", "").Replace(" ", "");
            consulta = consulta.Where(b =>
                (b.Nombres + " " + b.Apellidos).ToLower().Contains(texto) || b.Rut.ToLower().Contains(rut));
        }
        if (filtro.UnidadId is { } unidadId) consulta = consulta.Where(b => b.UnidadId == unidadId);
        if (filtro.PatrullaId is { } patrullaId) consulta = consulta.Where(b => b.PatrullaId == patrullaId);
        if (!string.IsNullOrWhiteSpace(filtro.Estado)) consulta = consulta.Where(b => b.Estado == filtro.Estado);

        var pagina = await consulta
            .OrderBy(b => b.Apellidos).ThenBy(b => b.Nombres)
            .Select(b => new
            {
                b.Id, b.Rut, b.Nombres, b.Apellidos, b.FechaNacimiento, b.UnidadId, Unidad = b.Unidad.Nombre,
                b.PatrullaId, Patrulla = b.Patrulla != null ? b.Patrulla.Nombre : null, b.Estado, b.FechaIngreso,
                FichaActualizada = b.FichaMedica != null ? (DateTime?)b.FichaMedica.UpdatedAt : null,
            })
            .PaginarAsync(filtro, ct);

        var hoy = Hoy();
        var elementos = pagina.Elementos.Select(b => new BeneficiarioResumen(
            b.Id, Rut.Formatear(b.Rut), b.Nombres, b.Apellidos, b.FechaNacimiento, Edad(b.FechaNacimiento, hoy),
            b.UnidadId, b.Unidad, b.PatrullaId, b.Patrulla, b.Estado, b.FechaIngreso,
            Edades.Aviso(Edad(b.FechaNacimiento, hoy)), VigenciaFicha.Estado(b.FichaActualizada))).ToList();

        return Ok(ResponseData.Ok(new Pagina<BeneficiarioResumen>(elementos, pagina.Total, pagina.PaginaActual, pagina.Tamano)));
    }

    /// <summary>Indica si ya hay un beneficiario con ese RUT, para avisarlo en el formulario antes de pedir el resto de los datos.</summary>
    [HttpGet("existe")]
    public async Task<IActionResult> Existe([FromQuery] string? rut, CancellationToken ct)
    {
        if (!Rut.EsValido(rut)) return BadRequest(ResponseData.Error("El RUT no es válido."));
        var normalizado = Rut.Normalizar(rut!);
        var registrado = await db.Beneficiarios.AsNoTracking()
            .Where(x => x.Rut == normalizado)
            .Select(x => new RutRegistrado(true, x.Id, x.Nombres + " " + x.Apellidos))
            .FirstOrDefaultAsync(ct);
        return Ok(ResponseData.Ok(registrado ?? RutRegistrado.Libre));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id, CancellationToken ct)
    {
        var detalle = await DetalleAsync(id, ct);
        return detalle is null ? NoEncontrado() : Ok(ResponseData.Ok(detalle));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(SolicitudBeneficiario solicitud, CancellationToken ct)
    {
        var rut = Rut.Normalizar(solicitud.Rut);
        if (ValidarFechas(solicitud) is { } errorFechas) return errorFechas;
        if (await db.Beneficiarios.AnyAsync(b => b.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe un beneficiario con ese RUT."));
        }
        if (await ValidarUbicacionAsync(solicitud.UnidadId, solicitud.PatrullaId, ct) is { } errorUbicacion) return errorUbicacion;

        var beneficiario = new Beneficiario
        {
            Rut = rut,
            Nombres = solicitud.Nombres.Trim(),
            Apellidos = solicitud.Apellidos.Trim(),
            FechaNacimiento = solicitud.FechaNacimiento,
            Direccion = Limpio(solicitud.Direccion),
            UnidadId = solicitud.UnidadId,
            PatrullaId = solicitud.PatrullaId,
            FechaIngreso = solicitud.FechaIngreso ?? Hoy(),
            RegistroInstitucionalAnio = solicitud.RegistroInstitucionalAnio,
            Estado = EstadosBeneficiario.Activo,
        };
        db.Beneficiarios.Add(beneficiario);
        Registrar(beneficiario, TiposHistorial.Ingreso, beneficiario.FechaIngreso, null, beneficiario.UnidadId, null);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = beneficiario.Id },
            ResponseData.Ok(await DetalleAsync(beneficiario.Id, ct), "Beneficiario registrado."));
    }

    /// <summary>
    /// Inscribe a un beneficiario con sus apoderados, contactos de emergencia y ficha médica en una sola operación:
    /// todo se guarda con un único SaveChanges (una transacción), así que si algo falla no queda nada a medias.
    /// </summary>
    [HttpPost("inscripcion")]
    public async Task<IActionResult> Inscribir(SolicitudInscripcion solicitud, CancellationToken ct)
    {
        var datos = solicitud.Beneficiario;
        var rut = Rut.Normalizar(datos.Rut);
        if (ValidarFechas(datos) is { } errorFechas) return errorFechas;
        if (await db.Beneficiarios.AnyAsync(b => b.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe un beneficiario con ese RUT."));
        }
        if (await ValidarUbicacionAsync(datos.UnidadId, datos.PatrullaId, ct) is { } errorUbicacion) return errorUbicacion;

        var contactos = solicitud.Contactos ?? [];
        if (contactos.Count > ContactosController.Maximo)
        {
            return BadRequest(ResponseData.Error($"Se admiten hasta {ContactosController.Maximo} contactos de emergencia."));
        }

        var apoderados = solicitud.Apoderados ?? [];
        if (await ValidarApoderadosAsync(apoderados, ct) is { } errorApoderados) return errorApoderados;

        var beneficiario = new Beneficiario
        {
            Rut = rut,
            Nombres = datos.Nombres.Trim(),
            Apellidos = datos.Apellidos.Trim(),
            FechaNacimiento = datos.FechaNacimiento,
            Direccion = Limpio(datos.Direccion),
            UnidadId = datos.UnidadId,
            PatrullaId = datos.PatrullaId,
            FechaIngreso = datos.FechaIngreso ?? Hoy(),
            RegistroInstitucionalAnio = datos.RegistroInstitucionalAnio,
            Estado = EstadosBeneficiario.Activo,
        };
        db.Beneficiarios.Add(beneficiario);
        Registrar(beneficiario, TiposHistorial.Ingreso, beneficiario.FechaIngreso, null, beneficiario.UnidadId, null);

        // El primero queda como principal si no se marcó ninguno.
        var indicePrincipal = apoderados.ToList().FindIndex(a => a.EsPrincipal);
        if (indicePrincipal < 0) indicePrincipal = 0;
        for (var i = 0; i < apoderados.Count; i++)
        {
            var item = apoderados[i];
            var vinculo = new BeneficiarioApoderado
            {
                Beneficiario = beneficiario,
                Parentesco = item.Parentesco.Trim(),
                EsPrincipal = i == indicePrincipal,
            };
            if (item.ApoderadoId is { } apoderadoId)
            {
                vinculo.ApoderadoId = apoderadoId;
            }
            else
            {
                var nuevo = new Apoderado();
                ApoderadosController.Copiar(item.Nuevo!, nuevo, Rut.Normalizar(item.Nuevo!.Rut));
                vinculo.Apoderado = nuevo;
            }
            db.BeneficiarioApoderado.Add(vinculo);
        }

        foreach (var datosContacto in contactos)
        {
            var contacto = new ContactoEmergencia { Beneficiario = beneficiario };
            ContactosController.Copiar(datosContacto, contacto);
            db.ContactosEmergencia.Add(contacto);
        }

        if (solicitud.FichaMedica is { } datosFicha)
        {
            var ficha = new FichaMedica { Beneficiario = beneficiario };
            FichaMedicaController.Aplicar(ficha, datosFicha, cifrado, usuarioActual.Id);
            db.FichasMedicas.Add(ficha);
        }

        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = beneficiario.Id },
            ResponseData.Ok(await DetalleAsync(beneficiario.Id, ct), "Beneficiario inscrito."));
    }

    /// <summary>Cada apoderado es uno existente o uno nuevo; sin repetidos y sin RUT ya registrados.</summary>
    private async Task<IActionResult?> ValidarApoderadosAsync(IReadOnlyList<ApoderadoInscripcion> apoderados, CancellationToken ct)
    {
        if (apoderados.Any(a => (a.ApoderadoId is null) == (a.Nuevo is null)))
        {
            return BadRequest(ResponseData.Error("Cada apoderado debe ser uno ya registrado o uno nuevo."));
        }
        if (apoderados.Count(a => a.EsPrincipal) > 1)
        {
            return BadRequest(ResponseData.Error("Solo puede haber un apoderado principal."));
        }

        var existentes = apoderados.Where(a => a.ApoderadoId is not null).Select(a => a.ApoderadoId!.Value).ToList();
        if (existentes.Count != existentes.Distinct().Count())
        {
            return BadRequest(ResponseData.Error("Un apoderado está repetido."));
        }
        if (await db.Apoderados.CountAsync(a => existentes.Contains(a.Id), ct) != existentes.Count)
        {
            return BadRequest(ResponseData.Error("Uno de los apoderados elegidos no existe."));
        }

        var rutsNuevos = apoderados.Where(a => a.Nuevo is not null).Select(a => Rut.Normalizar(a.Nuevo!.Rut)).ToList();
        if (rutsNuevos.Count != rutsNuevos.Distinct().Count())
        {
            return BadRequest(ResponseData.Error("Hay dos apoderados nuevos con el mismo RUT."));
        }
        var yaRegistrado = await db.Apoderados.Where(a => rutsNuevos.Contains(a.Rut))
            .Select(a => a.Nombres + " " + a.Apellidos).FirstOrDefaultAsync(ct);
        if (yaRegistrado is not null)
        {
            return Conflict(ResponseData.Error($"Ya existe un apoderado con ese RUT ({yaRegistrado}). Elígelo de la lista en vez de crearlo."));
        }
        return null;
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Editar(long id, SolicitudBeneficiario solicitud, CancellationToken ct)
    {
        var beneficiario = await db.Beneficiarios.FindAsync([id], ct);
        if (beneficiario is null) return NoEncontrado();

        var rut = Rut.Normalizar(solicitud.Rut);
        if (ValidarFechas(solicitud, beneficiario.Estado == EstadosBeneficiario.Activo) is { } errorFechas) return errorFechas;
        if (await db.Beneficiarios.AnyAsync(b => b.Id != id && b.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe otro beneficiario con ese RUT."));
        }

        var cambiaUnidad = beneficiario.UnidadId != solicitud.UnidadId;
        var cambiaPatrulla = beneficiario.PatrullaId != solicitud.PatrullaId;
        if ((cambiaUnidad || cambiaPatrulla) && beneficiario.Estado == EstadosBeneficiario.Retirado)
        {
            return Conflict(ResponseData.Error("El beneficiario está retirado. Reingrésalo para asignarle otra unidad o patrulla."));
        }
        if ((cambiaUnidad || cambiaPatrulla)
            && await ValidarUbicacionAsync(solicitud.UnidadId, solicitud.PatrullaId, ct, id) is { } errorUbicacion)
        {
            return errorUbicacion;
        }

        if (cambiaUnidad)
        {
            Registrar(beneficiario, TiposHistorial.CambioUnidad, Hoy(), beneficiario.UnidadId, solicitud.UnidadId, null);
        }
        else if (cambiaPatrulla)
        {
            var nombres = await db.Patrullas.Where(p => p.Id == beneficiario.PatrullaId || p.Id == solicitud.PatrullaId)
                .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);
            var desde = beneficiario.PatrullaId is { } a && nombres.TryGetValue(a, out var na) ? na : "sin patrulla";
            var hacia = solicitud.PatrullaId is { } b && nombres.TryGetValue(b, out var nb) ? nb : "sin patrulla";
            Registrar(beneficiario, TiposHistorial.CambioPatrulla, Hoy(), beneficiario.UnidadId, beneficiario.UnidadId,
                $"De {desde} a {hacia}.");
        }

        beneficiario.Rut = rut;
        beneficiario.Nombres = solicitud.Nombres.Trim();
        beneficiario.Apellidos = solicitud.Apellidos.Trim();
        beneficiario.FechaNacimiento = solicitud.FechaNacimiento;
        beneficiario.Direccion = Limpio(solicitud.Direccion);
        beneficiario.UnidadId = solicitud.UnidadId;
        beneficiario.PatrullaId = solicitud.PatrullaId;
        if (solicitud.FechaIngreso is { } ingreso) beneficiario.FechaIngreso = ingreso;
        beneficiario.RegistroInstitucionalAnio = solicitud.RegistroInstitucionalAnio;
        await db.SaveChangesAsync(ct);

        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Datos del beneficiario actualizados."));
    }

    /// <summary>Retira al beneficiario del grupo (sus datos se conservan).</summary>
    [HttpPost("{id:long}/retirar")]
    public async Task<IActionResult> Retirar(long id, SolicitudMovimiento solicitud, CancellationToken ct)
    {
        var beneficiario = await db.Beneficiarios.FindAsync([id], ct);
        if (beneficiario is null) return NoEncontrado();
        if (beneficiario.Estado == EstadosBeneficiario.Retirado)
        {
            return Conflict(ResponseData.Error("El beneficiario ya está retirado."));
        }

        var fecha = solicitud.Fecha ?? Hoy();
        if (fecha > Hoy()) return BadRequest(ResponseData.Error("La fecha de retiro no puede ser futura."));

        beneficiario.Estado = EstadosBeneficiario.Retirado;
        Registrar(beneficiario, TiposHistorial.Retiro, fecha, beneficiario.UnidadId, null, Limpio(solicitud.Detalle));
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Beneficiario retirado."));
    }

    /// <summary>Reingresa a un beneficiario retirado, opcionalmente en otra unidad o patrulla.</summary>
    [HttpPost("{id:long}/reingresar")]
    public async Task<IActionResult> Reingresar(long id, SolicitudMovimiento solicitud, CancellationToken ct)
    {
        var beneficiario = await db.Beneficiarios.FindAsync([id], ct);
        if (beneficiario is null) return NoEncontrado();
        if (beneficiario.Estado == EstadosBeneficiario.Activo)
        {
            return Conflict(ResponseData.Error("El beneficiario ya está activo."));
        }

        if (ErrorEdadMaxima(beneficiario.FechaNacimiento) is { } errorEdad) return errorEdad;
        var fecha = solicitud.Fecha ?? Hoy();
        if (fecha > Hoy()) return BadRequest(ResponseData.Error("La fecha de reingreso no puede ser futura."));

        var unidadId = solicitud.UnidadId ?? beneficiario.UnidadId;
        var patrullaId = solicitud.UnidadId is null ? solicitud.PatrullaId ?? beneficiario.PatrullaId : solicitud.PatrullaId;
        if (await ValidarUbicacionAsync(unidadId, patrullaId, ct, id) is { } errorUbicacion) return errorUbicacion;

        Registrar(beneficiario, TiposHistorial.Reingreso, fecha, beneficiario.UnidadId, unidadId, Limpio(solicitud.Detalle));
        beneficiario.Estado = EstadosBeneficiario.Activo;
        beneficiario.UnidadId = unidadId;
        beneficiario.PatrullaId = patrullaId;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Beneficiario reingresado."));
    }

    /// <summary>Movimientos del beneficiario, del más reciente al más antiguo.</summary>
    [HttpGet("{id:long}/historial")]
    public async Task<IActionResult> Historial(long id, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == id, ct)) return NoEncontrado();

        var movimientos = await db.HistorialBeneficiario.AsNoTracking()
            .Where(h => h.BeneficiarioId == id)
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
            .ToListAsync(ct);

        var idsUnidades = movimientos.SelectMany(m => new[] { m.UnidadOrigenId, m.UnidadDestinoId })
            .OfType<long>().Distinct().ToList();
        var unidades = await db.Unidades.Where(u => idsUnidades.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);
        var idsUsuarios = movimientos.Select(m => m.UsuarioId).OfType<long>().Distinct().ToList();
        var usuarios = await db.Users.Where(u => idsUsuarios.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);

        var resultado = movimientos.Select(m => new MovimientoHistorial(
            m.Id, m.Tipo, m.Fecha,
            m.UnidadOrigenId is { } o ? unidades.GetValueOrDefault(o) : null,
            m.UnidadDestinoId is { } d ? unidades.GetValueOrDefault(d) : null,
            m.Detalle,
            m.UsuarioId is { } u ? usuarios.GetValueOrDefault(u) : null)).ToList();

        return Ok(ResponseData.Ok<IReadOnlyList<MovimientoHistorial>>(resultado));
    }

    private void Registrar(Beneficiario beneficiario, string tipo, DateOnly fecha, long? origen, long? destino, string? detalle) =>
        db.HistorialBeneficiario.Add(new HistorialBeneficiario
        {
            Beneficiario = beneficiario,
            Tipo = tipo,
            Fecha = fecha,
            UnidadOrigenId = origen,
            UnidadDestinoId = destino,
            Detalle = detalle,
            UsuarioId = usuarioActual.Id,
        });

    private async Task<BeneficiarioDetalle?> DetalleAsync(long id, CancellationToken ct)
    {
        var b = await db.Beneficiarios.AsNoTracking().Include(x => x.Unidad).Include(x => x.Patrulla).Include(x => x.FichaMedica)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return null;

        var edad = Edad(b.FechaNacimiento, Hoy());
        return new BeneficiarioDetalle(b.Id, Rut.Formatear(b.Rut), b.Nombres, b.Apellidos, b.FechaNacimiento,
            edad, b.Direccion, b.UnidadId, b.Unidad.Nombre, b.Unidad.Rama, b.PatrullaId, b.Patrulla?.Nombre, b.Estado,
            b.FechaIngreso, b.RegistroInstitucionalAnio, Edades.Aviso(edad), VigenciaFicha.Estado(b.FichaMedica?.UpdatedAt));
    }

    /// <summary>
    /// La unidad debe existir, estar activa y tener cupo (máximo 32 activos); la patrulla, si viene, debe ser de esa
    /// unidad, estar activa y no superar el máximo de su tipo (seisena 6, patrulla 8). `beneficiarioId` excluye al
    /// propio beneficiario del conteo cuando se edita.
    /// </summary>
    private async Task<IActionResult?> ValidarUbicacionAsync(long unidadId, long? patrullaId, CancellationToken ct, long? beneficiarioId = null)
    {
        var unidad = await db.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unidadId, ct);
        if (unidad is null) return BadRequest(ResponseData.Error("Selecciona una unidad válida."));
        if (!unidad.Activa) return Conflict(ResponseData.Error("La unidad seleccionada está desactivada."));

        var activos = db.Beneficiarios.Where(b => b.Estado == EstadosBeneficiario.Activo && b.Id != beneficiarioId);
        if (await activos.CountAsync(b => b.UnidadId == unidadId, ct) >= Ramas.MaximoPorUnidad)
        {
            return Conflict(ResponseData.Error($"{unidad.Nombre} ya tiene {Ramas.MaximoPorUnidad} beneficiarios activos, el máximo por unidad."));
        }

        if (patrullaId is { } idPatrulla)
        {
            var patrulla = await db.Patrullas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == idPatrulla, ct);
            var subgrupo = Ramas.Subgrupo(unidad.Rama);
            if (patrulla is null || patrulla.UnidadId != unidadId)
            {
                return BadRequest(ResponseData.Error($"La {subgrupo.Nombre} no pertenece a la unidad seleccionada."));
            }
            if (!patrulla.Activa) return Conflict(ResponseData.Error($"La {subgrupo.Nombre} seleccionada está desactivada."));
            if (subgrupo.Maximo is { } maximo && await activos.CountAsync(b => b.PatrullaId == idPatrulla, ct) >= maximo)
            {
                return Conflict(ResponseData.Error($"La {subgrupo.Nombre} {patrulla.Nombre} ya tiene {maximo} integrantes, el máximo."));
            }
        }
        return null;
    }

    /// <summary>Fechas coherentes y, si va a quedar activo, que no supere la edad máxima.</summary>
    private BadRequestObjectResult? ValidarFechas(SolicitudBeneficiario solicitud, bool quedaActivo = true)
    {
        var hoy = Hoy();
        if (solicitud.FechaNacimiento == default || solicitud.FechaNacimiento > hoy || solicitud.FechaNacimiento.Year < 1900)
        {
            return BadRequest(ResponseData.Error("Ingresa una fecha de nacimiento válida."));
        }
        if (quedaActivo && ErrorEdadMaxima(solicitud.FechaNacimiento) is { } errorEdad) return errorEdad;
        if (solicitud.FechaIngreso is { } ingreso && (ingreso > hoy || ingreso < solicitud.FechaNacimiento))
        {
            return BadRequest(ResponseData.Error("La fecha de ingreso no puede ser futura ni anterior al nacimiento."));
        }
        return null;
    }

    private BadRequestObjectResult? ErrorEdadMaxima(DateOnly nacimiento)
    {
        var edad = Edad(nacimiento, Hoy());
        return edad >= Edades.Maxima
            ? BadRequest(ResponseData.Error($"Con {edad} años supera la edad máxima para participar ({Edades.Maxima})."))
            : null;
    }

    private NotFoundObjectResult NoEncontrado() => NotFound(ResponseData.Error("No se encontró el beneficiario."));

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.Now);

    public static int Edad(DateOnly nacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - nacimiento.Year;
        if (hoy < nacimiento.AddYears(edad)) edad--;
        return Math.Max(0, edad);
    }
}
