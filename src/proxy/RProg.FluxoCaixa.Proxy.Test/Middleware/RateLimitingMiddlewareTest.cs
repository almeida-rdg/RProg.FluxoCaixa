using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Proxy.Middleware;
using Xunit;
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

    public RateLimitingMiddlewareTest()
    {
        _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_QuandoPrimeiraRequisicao_DevePermitir()
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
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
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("192.168.1.1");

        // Act - Faz muitas requisições rapidamente
        for (int i = 0; i < 15; i++) // Limite é 10 por segundo
        {
            await middleware.InvokeAsync(context);
            context = CriarHttpContext("192.168.1.1"); // Cria novo contexto para cada requisição
        }

        // Assert - A última requisição deve ser bloqueada
        Assert.Equal((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoIpsDiferentes_DevePermitirSeparadamente()
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
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
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("127.0.0.1");
        context.Request.Headers.Add("X-Forwarded-For", "203.0.113.1");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("203.0.113.1")]
    public async Task InvokeAsync_ComDiferentesIps_DeveProcessarCorreteamente(string ip)
    {
        // Arrange
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
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
        var middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object);
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
