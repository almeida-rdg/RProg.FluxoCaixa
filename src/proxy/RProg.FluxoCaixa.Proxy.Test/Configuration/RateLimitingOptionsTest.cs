using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RProg.FluxoCaixa.Proxy.Configuration;

namespace RProg.FluxoCaixa.Proxy.Test.Configuration;

/// <summary>
/// Testes para validar o carregamento das configurações de Rate Limiting
/// </summary>
public class RateLimitingOptionsTest
{
    [Fact]
    public void RateLimitingOptions_DeveCarregarConfiguracoesPadrao()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string>
        {
            {"RateLimiting:LimitePorMinuto", "60"},
            {"RateLimiting:LimitePorSegundo", "10"},
            {"RateLimiting:JanelaTempo", "00:01:00"},
            {"RateLimiting:TempoBloqueio", "00:05:00"},
            {"RateLimiting:IntervaloLimpeza", "00:05:00"},
            {"RateLimiting:Habilitado", "true"},
            {"RateLimiting:IpsIsentos:0", "127.0.0.1"},
            {"RateLimiting:IpsIsentos:1", "::1"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var services = new ServiceCollection();
        services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimiting"));
        services.AddSingleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        // Assert
        Assert.Equal(60, options.LimitePorMinuto);
        Assert.Equal(10, options.LimitePorSegundo);
        Assert.Equal(TimeSpan.FromMinutes(1), options.JanelaTempo);
        Assert.Equal(TimeSpan.FromMinutes(5), options.TempoBloqueio);
        Assert.Equal(TimeSpan.FromMinutes(5), options.IntervaloLimpeza);
        Assert.True(options.Habilitado);
        Assert.Contains("127.0.0.1", options.IpsIsentos);
        Assert.Contains("::1", options.IpsIsentos);
    }

    [Fact]
    public void RateLimitingOptionsValidator_DeveValidarOpcoesValidas()
    {
        // Arrange
        var validator = new RateLimitingOptionsValidator();
        var options = new RateLimitingOptions
        {
            LimitePorMinuto = 60,
            LimitePorSegundo = 10,
            JanelaTempo = TimeSpan.FromMinutes(1),
            TempoBloqueio = TimeSpan.FromMinutes(5),
            IntervaloLimpeza = TimeSpan.FromMinutes(5),
            IpsIsentos = new List<string> { "127.0.0.1" },
            Habilitado = true
        };

        // Act
        var result = validator.Validate("RateLimiting", options);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0, 10)] // LimitePorMinuto = 0
    [InlineData(60, 0)] // LimitePorSegundo = 0
    [InlineData(-1, 10)] // LimitePorMinuto negativo
    [InlineData(60, -1)] // LimitePorSegundo negativo
    public void RateLimitingOptionsValidator_DeveRejectarLimitesInvalidos(int limitePorMinuto, int limitePorSegundo)
    {
        // Arrange
        var validator = new RateLimitingOptionsValidator();
        var options = new RateLimitingOptions
        {
            LimitePorMinuto = limitePorMinuto,
            LimitePorSegundo = limitePorSegundo,
            JanelaTempo = TimeSpan.FromMinutes(1),
            TempoBloqueio = TimeSpan.FromMinutes(5),
            IntervaloLimpeza = TimeSpan.FromMinutes(5),
            IpsIsentos = new List<string>(),
            Habilitado = true
        };

        // Act
        var result = validator.Validate("RateLimiting", options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Failures);
    }

    [Fact]
    public void RateLimitingOptionsValidator_DeveRejectarTemposInvalidos()
    {
        // Arrange
        var validator = new RateLimitingOptionsValidator();
        var options = new RateLimitingOptions
        {
            LimitePorMinuto = 60,
            LimitePorSegundo = 10,
            JanelaTempo = TimeSpan.Zero, // Tempo inválido
            TempoBloqueio = TimeSpan.FromMinutes(5),
            IntervaloLimpeza = TimeSpan.FromMinutes(5),
            IpsIsentos = new List<string>(),
            Habilitado = true
        };

        // Act
        var result = validator.Validate("RateLimiting", options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures, f => f.Contains("JanelaTempo"));
    }
}
