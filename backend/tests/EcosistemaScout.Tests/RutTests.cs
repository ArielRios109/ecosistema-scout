using EcosistemaScout.Api.Comun;

namespace EcosistemaScout.Tests;

public class RutTests
{
    [Theory]
    [InlineData("11.111.111-1")]
    [InlineData("12345678-5")]
    [InlineData("123456785")]
    [InlineData("7.775.735-k")]
    [InlineData("7775735K")]
    public void EsValido_ConDigitoCorrecto_DevuelveTrue(string rut) => Assert.True(Rut.EsValido(rut));

    [Theory]
    [InlineData("12345678-9")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc-d")]
    [InlineData("0-0")]
    public void EsValido_ConRutIncorrecto_DevuelveFalse(string? rut) => Assert.False(Rut.EsValido(rut));

    [Fact]
    public void Normalizar_DejaFormatoSinPuntosConGuion() => Assert.Equal("7775735-K", Rut.Normalizar("7.775.735-k"));

    [Fact]
    public void Formatear_AgregaPuntos() => Assert.Equal("12.345.678-5", Rut.Formatear("123456785"));
}
