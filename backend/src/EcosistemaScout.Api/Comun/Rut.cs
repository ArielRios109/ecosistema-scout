using System.ComponentModel.DataAnnotations;

namespace EcosistemaScout.Api.Comun;

/// <summary>Validación y formato del RUT chileno (módulo 11).</summary>
public static class Rut
{
    /// <summary>Deja solo dígitos y la K, en mayúscula (quita puntos, guion y espacios).</summary>
    public static string Limpiar(string? rut) =>
        new string((rut ?? "").Where(c => char.IsDigit(c) || c is 'k' or 'K').ToArray()).ToUpperInvariant();

    /// <summary>Calcula el dígito verificador de un número de RUT.</summary>
    public static char CalcularDv(long numero)
    {
        int suma = 0, multiplo = 2;
        while (numero > 0)
        {
            suma += (int)(numero % 10) * multiplo;
            numero /= 10;
            multiplo = multiplo == 7 ? 2 : multiplo + 1;
        }
        var resto = 11 - (suma % 11);
        return resto switch { 11 => '0', 10 => 'K', _ => (char)('0' + resto) };
    }

    /// <summary>Largo máximo con formato: "12.345.678-9".</summary>
    public const int LargoMaximo = 12;

    /// <summary>
    /// Indica si el RUT tiene un dígito verificador correcto. Acepta con o sin puntos y guion, pero no otros
    /// caracteres (letras distintas de K, símbolos) ni más de 12 caracteres.
    /// </summary>
    public static bool EsValido(string? rut)
    {
        if (string.IsNullOrWhiteSpace(rut) || rut.Trim().Length > LargoMaximo) return false;
        if (rut.Trim().Any(c => !(char.IsDigit(c) || c is 'k' or 'K' or '.' or '-'))) return false;
        var limpio = Limpiar(rut);
        if (limpio.Length < 2 || limpio.Length > 9) return false;
        var cuerpo = limpio[..^1];
        if (!long.TryParse(cuerpo, out var numero) || numero <= 0) return false;
        return CalcularDv(numero) == limpio[^1];
    }

    /// <summary>Forma en que se guarda en la base de datos: "12345678-9" (sin puntos, con guion).</summary>
    public static string Normalizar(string rut)
    {
        var limpio = Limpiar(rut);
        return $"{limpio[..^1]}-{limpio[^1]}";
    }

    /// <summary>Forma para mostrar: "12.345.678-9".</summary>
    public static string Formatear(string rut)
    {
        var limpio = Limpiar(rut);
        var cuerpo = long.Parse(limpio[..^1]);
        return $"{cuerpo:#,0}".Replace(',', '.') + "-" + limpio[^1];
    }
}

/// <summary>Valida que un campo contenga un RUT chileno válido.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class RutValidoAttribute() : ValidationAttribute("El RUT no es válido.")
{
    public override bool IsValid(object? value) => value is null || (value is string s && Rut.EsValido(s));
}

/// <summary>Respuesta de las consultas "¿ya existe este RUT?" que usan los formularios antes de pedir el resto de los datos.</summary>
public record RutRegistrado(bool Existe, long? Id, string? Nombre)
{
    public static readonly RutRegistrado Libre = new(false, null, null);
}
