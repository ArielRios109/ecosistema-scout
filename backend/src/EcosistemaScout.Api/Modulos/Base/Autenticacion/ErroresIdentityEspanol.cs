using Microsoft.AspNetCore.Identity;

namespace EcosistemaScout.Api.Modulos.Base.Autenticacion;

/// <summary>Mensajes de ASP.NET Identity en español, para mostrarlos tal cual en el snackbar.</summary>
public class ErroresIdentityEspanol : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "Ocurrió un error inesperado.");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "La contraseña actual no es correcta.");
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), $"El correo '{email}' no es válido.");
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), $"El correo '{email}' ya está en uso.");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), $"El usuario '{userName}' ya existe.");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número.");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una minúscula.");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una mayúscula.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe incluir al menos un símbolo.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(nameof(PasswordRequiresUniqueChars), $"La contraseña debe tener al menos {uniqueChars} caracteres distintos.");
    public override IdentityError UserAlreadyInRole(string role) => Error(nameof(UserAlreadyInRole), $"El usuario ya tiene el rol '{role}'.");
    public override IdentityError UserNotInRole(string role) => Error(nameof(UserNotInRole), $"El usuario no tiene el rol '{role}'.");
    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "El enlace o código ya no es válido.");

    private static IdentityError Error(string codigo, string descripcion) => new() { Code = codigo, Description = descripcion };
}
