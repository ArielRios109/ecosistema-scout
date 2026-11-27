using System.Security.Cryptography;

namespace EcosistemaScout.Api.Modulos.Base.Usuarios;

/// <summary>Genera contraseñas temporales aleatorias, fáciles de dictar (sin caracteres confusos como 0/O o 1/l).</summary>
public static class GeneradorPassword
{
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
    private const string Numeros = "23456789";

    /// <summary>Contraseña de 10 caracteres con mayúsculas, minúsculas y números (cumple la política de Identity).</summary>
    public static string Generar()
    {
        var caracteres = new List<char>
        {
            Elegir(Mayusculas), Elegir(Mayusculas),
            Elegir(Numeros), Elegir(Numeros),
        };
        var todos = Mayusculas + Minusculas + Numeros;
        while (caracteres.Count < 10)
        {
            caracteres.Add(caracteres.Count < 6 ? Elegir(Minusculas) : Elegir(todos));
        }
        return new string(caracteres.OrderBy(_ => RandomNumberGenerator.GetInt32(1000)).ToArray());
    }

    private static char Elegir(string fuente) => fuente[RandomNumberGenerator.GetInt32(fuente.Length)];
}
