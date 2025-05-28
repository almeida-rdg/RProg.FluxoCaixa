using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Proxy.Middleware;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace RProg.FluxoCaixa.Proxy.Test.Middleware;

/// <summary>
/// Testes unitários para o middleware de cache
/// </summary>
public class CacheMiddlewareTest
{
    private readonly Mock<ILogger<CacheMiddleware>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<RequestDelegate> _mockNext;

    public CacheMiddlewareTest()
    {
        _mockLogger = new Mock<ILogger<CacheMiddleware>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockNext = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_QuandoMetodoNaoEhGet_DeveChamarProximoSemCache()
    {
        // Arrange
        var middleware = new CacheMiddleware(_mockNext.Object, _memoryCache, _mockLogger.Object);
        var context = CriarHttpContext("POST", "/api/consolidado");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.False(context.Response.Headers.ContainsKey("X-Cache"));
    }

    [Fact]
    public async Task InvokeAsync_QuandoRotaNaoTemCache_DeveChamarProximoSemCache()
    {
        // Arrange
        var middleware = new CacheMiddleware(_mockNext.Object, _memoryCache, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/api/outros");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.False(context.Response.Headers.ContainsKey("X-Cache"));
    }

    [Fact]
    public async Task InvokeAsync_QuandoCacheMiss_DeveArmazenarNoCache()
    {
        // Arrange
        var middleware = new CacheMiddleware(_mockNext.Object, _memoryCache, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/consolidado");
        var respostaEsperada = "dados consolidados";

        _mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.Body.Write(Encoding.UTF8.GetBytes(respostaEsperada));
            });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.True(context.Response.Headers.ContainsKey("X-Cache"));
        Assert.Equal("MISS", context.Response.Headers["X-Cache"]);
    }

    [Fact]
    public async Task InvokeAsync_QuandoCacheHit_DeveRetornarDoCache()
    {
        // Arrange
        var middleware = new CacheMiddleware(_mockNext.Object, _memoryCache, _mockLogger.Object);
        var context1 = CriarHttpContext("GET", "/consolidado");
        var context2 = CriarHttpContext("GET", "/consolidado");
        var respostaEsperada = "dados consolidados";

        _mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.Body.Write(Encoding.UTF8.GetBytes(respostaEsperada));
            });

        // Act - Primeira chamada (MISS)
        await middleware.InvokeAsync(context1);

        // Act - Segunda chamada (HIT)
        await middleware.InvokeAsync(context2);

        // Assert
        _mockNext.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
        Assert.Equal("HIT", context2.Response.Headers["X-Cache"]);
    }

    [Theory]
    [InlineData("/consolidado/saldo-diario")]
    [InlineData("/consolidado")]
    [InlineData("/lancamentos")]
    public async Task InvokeAsync_QuandoRotaComCache_DeveAplicarCache(string caminho)
    {
        // Arrange
        var middleware = new CacheMiddleware(_mockNext.Object, _memoryCache, _mockLogger.Object);
        var context = CriarHttpContext("GET", caminho);

        _mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.Body.Write(Encoding.UTF8.GetBytes("{}"));
            });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Cache"));
    }

    private static HttpContext CriarHttpContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
