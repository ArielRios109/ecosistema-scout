using Microsoft.AspNetCore.DataProtection;

namespace EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;

/// <summary>
/// Cifra y descifra los datos de salud con ASP.NET Data Protection (AES-256 con llaves rotativas guardadas
/// en la base de datos). Así, quien acceda directamente a la tabla fichas_medicas solo ve texto cifrado.
/// </summary>
public class CifradoFichaMedica(IDataProtectionProvider proveedor)
{
    private readonly IDataProtector _protector = proveedor.CreateProtector("Tali.Miembros.FichaMedica.v1");

    public string? Cifrar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : _protector.Protect(texto.Trim());

    public string? Descifrar(string? cifrado) =>
        string.IsNullOrEmpty(cifrado) ? null : _protector.Unprotect(cifrado);
}
