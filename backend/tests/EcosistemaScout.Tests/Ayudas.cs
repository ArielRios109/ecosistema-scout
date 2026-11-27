using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base.Autenticacion;

namespace EcosistemaScout.Tests;

/// <summary>Ayudas comunes para las pruebas de integración.</summary>
public static class Ayudas
{
    /// <summary>Crea un cliente HTTP ya autenticado con el usuario indicado.</summary>
    public static async Task<HttpClient> ClienteComoAsync(this FabricaApiPruebas fabrica, string email, string? password = null)
    {
        var cliente = fabrica.CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin(email, password ?? FabricaApiPruebas.PasswordDemo));
        login.EnsureSuccessStatusCode();
        var datos = (await login.Content.ReadFromJsonAsync<ResponseData<RespuestaLogin>>())!.Datos!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", datos.Token);
        return cliente;
    }

    /// <summary>Lee el cuerpo como ResponseData&lt;T&gt;.</summary>
    public static async Task<ResponseData<T>> LeerAsync<T>(this HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<ResponseData<T>>())!;
}
