using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RProg.FluxoCaixa.Proxy.Middleware;
using RProg.FluxoCaixa.Proxy.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace RProg.FluxoCaixa.Proxy.Test.Middleware;

/// <summary>
/// Testes unitários para o middleware de rate limiting
/// </summary>
public class RateLimitingMiddlewareTest
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly RateLimitingOptions _opcoesPadrao;

    public RateLimitingMiddlewareTest()
    {
        _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
        _opcoesPadrao = new RateLimitingOptions
        {
            LimitePorMinuto = 60,
            LimitePorSegundo = 10,
            JanelaTempo = TimeSpan.FromMinutes(1),
            TempoBloqueio = TimeSpan.FromMinutes(5),
            IntervaloLimpeza = TimeSpan.FromMinutes(5),
            IpsIsentos = new List<string> { "127.0.0.1" },
            Habilitado = true
        };    }

    [Fact]
    public async Task InvokeAsync_QuandoPrimeiraRequisicao_DevePermitir()
    {
        // Arrange
        var mockOpcoes = Options.Create(_opcoesPadrao);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context = CriarHttpContext("192.168.1.1");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Limit"));
        Assert.True(context.Response.Headers.ContainsKey("X-RateLimit-Remaining"));
    }

    [Fact]
    public async Task InvokeAsync_QuandoExcedeRequestsPorSegundo_DeveBloquer()
    {
        // Arrange
        var mockOpcoes = Options.Create(_opcoesPadrao);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context = CriarHttpContext("192.168.1.1");

        // Act - Faz muitas requisições rapidamente (mais que o limite de 10 por segundo)
        for (int i = 0; i < 15; i++)
        {
            context = CriarHttpContext("192.168.1.1");
            await middleware.InvokeAsync(context);
        }

        // Assert - A última requisição deve ser bloqueada
        Assert.Equal((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
    }    [Fact]
    public async Task InvokeAsync_QuandoExcedeRequestsPorMinuto_DeveBloquer()
    {
        // Arrange
        var opcoes = new RateLimitingOptions
        {
            LimitePorMinuto = 5,
            LimitePorSegundo = 10,
            JanelaTempo = TimeSpan.FromMinutes(1),
            TempoBloqueio = TimeSpan.FromMinutes(5),
            IntervaloLimpeza = TimeSpan.FromMinutes(5),
            IpsIsentos = new List<string>(),
            Habilitado = true
        };
        var mockOpcoes = Options.Create(opcoes);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);

        // Act - Faz mais requisições que o limite por minuto
        HttpContext? context = null;
        for (int i = 0; i < 7; i++)
        {
            context = CriarHttpContext("192.168.1.1");
            await middleware.InvokeAsync(context);
        }

        // Assert - A última requisição deve ser bloqueada
        Assert.NotNull(context);
        Assert.Equal((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoIpsDiferentes_DevePermitirSeparadamente()
    {
        // Arrange
        var mockOpcoes = Options.Create(_opcoesPadrao);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context1 = CriarHttpContext("192.168.1.1");
        var context2 = CriarHttpContext("192.168.1.2");

        // Act
        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        // Assert
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Exactly(2));
        Assert.Equal(200, context1.Response.StatusCode);
        Assert.Equal(200, context2.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoHeaderXForwardedFor_DeveUsarIpCorreto()
    {
        // Arrange
        var mockOpcoes = Options.Create(_opcoesPadrao);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context = CriarHttpContext("127.0.0.1");
        context.Request.Headers.Append("X-Forwarded-For", "203.0.113.1");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoIpIsento_DevePermitirSemRateLimit()
    {
        // Arrange
        var mockOpcoes = Options.Create(_opcoesPadrao);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context = CriarHttpContext("127.0.0.1"); // IP isento configurado

        // Act - Faz muitas requisições de um IP isento
        for (int i = 0; i < 20; i++)
        {
            context = CriarHttpContext("127.0.0.1");
            await middleware.InvokeAsync(context);
        }

        // Assert - Todas as requisições devem passar
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Exactly(20));
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoRateLimitingDesabilitado_DevePermitirTodas()
    {
        // Arrange
        var opcoes = new RateLimitingOptions
        {
            LimitePorMinuto = 1,
            LimitePorSegundo = 1,
            JanelaTempo = TimeSpan.FromMinutes(1),
            TempoBloqueio = TimeSpan.FromMinutes(5),
            IntervaloLimpeza = TimeSpan.FromMinutes(5),
            IpsIsentos = new List<string>(),
            Habilitado = false // Rate limiting desabilitado
        };
        var mockOpcoes = Options.Create(opcoes);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context = CriarHttpContext("192.168.1.1");

        // Act - Faz muitas requisições com rate limiting desabilitado
        for (int i = 0; i < 10; i++)
        {
            context = CriarHttpContext("192.168.1.1");
            await middleware.InvokeAsync(context);
        }

        // Assert - Todas as requisições devem passar
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Exactly(10));
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("203.0.113.1")]
    public async Task InvokeAsync_ComDiferentesIps_DeveProcessarCorreteamente(string ip)
    {
        // Arrange
        var mockOpcoes = Options.Create(_opcoesPadrao);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context = CriarHttpContext(ip);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Contains("X-RateLimit-Limit", context.Response.Headers.Keys);
    }

    [Fact]
    public async Task InvokeAsync_QuandoProximoChamaExcecao_DeveRethrow()
    {
        // Arrange
        var mockOpcoes = Options.Create(_opcoesPadrao);
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, mockOpcoes);
        var context = CriarHttpContext("192.168.1.1");
        var excecaoEsperada = new InvalidOperationException("Erro teste");

        _mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .ThrowsAsync(excecaoEsperada);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));
        
        Assert.Equal("Erro teste", excecao.Message);
    }

    private static HttpContext CriarHttpContext(string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Response.Body = new MemoryStream();
        return context;
    }
}
