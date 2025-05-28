using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Proxy.Middleware;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace RProg.FluxoCaixa.Proxy.Test.Middleware;

/// <summary>
/// Testes unitários para o middleware de segurança
/// </summary>
public class SecurityMiddlewareTest
{
    private readonly Mock<ILogger<SecurityMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;

    public SecurityMiddlewareTest()
    {
        _mockLogger = new Mock<ILogger<SecurityMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_QuandoRequisicaoValida_DeveAdicionarHeadersSeguranca()
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/api/consolidado");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.True(context.Response.Headers.ContainsKey("X-XSS-Protection"));
        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
    }

    [Theory]
    [InlineData("sqlmap")]
    [InlineData("nikto")]
    [InlineData("nmap")]
    [InlineData("Mozilla/5.0 sqlmap/1.0")]
    public async Task InvokeAsync_QuandoUserAgentSuspeito_DeveBloquer(string userAgent)
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/api/consolidado");
        context.Request.Headers.Add("User-Agent", userAgent);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/test?id=1' OR '1'='1")]
    [InlineData("/api/test?script=<script>alert('xss')</script>")]
    [InlineData("/api/test?path=../../../etc/passwd")]
    [InlineData("/api/test?eval=eval(malicious_code)")]
    public async Task InvokeAsync_QuandoPadraoAtaqueNaUrl_DeveBloquer(string path)
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("GET", path);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoRequisicaoMuitoGrande_DeveBloquer()
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("POST", "/api/test");
        context.Request.ContentLength = 60 * 1024 * 1024; // 60MB - acima do limite de 50MB

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("X-Injection", "<script>alert('xss')</script>")]
    [InlineData("X-SQL", "1' OR '1'='1")]
    [InlineData("X-Path", "../../../etc/passwd")]
    public async Task InvokeAsync_QuandoPadraoAtaqueNoHeader_DeveBloquer(string headerName, string headerValue)
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/api/test");
        context.Request.Headers.Add(headerName, headerValue);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Never);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoUserAgentValido_DevePermitir()
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/api/consolidado");
        context.Request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.NotEqual(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_QuandoExcecaoOcorre_DeveRethrow()
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/api/test");
        var excecaoEsperada = new InvalidOperationException("Erro teste");

        _mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .ThrowsAsync(excecaoEsperada);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));
        
        Assert.Equal("Erro teste", excecao.Message);
    }

    [Fact]
    public async Task InvokeAsync_DeveRemoverHeadersQueRevelamInformacoes()
    {
        // Arrange
        var middleware = new SecurityMiddleware(_mockNext.Object, _mockLogger.Object);
        var context = CriarHttpContext("GET", "/api/test");
        
        // Simula headers que deveriam ser removidos
        context.Response.Headers.Add("Server", "Kestrel");
        context.Response.Headers.Add("X-Powered-By", "ASP.NET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Server"));
        Assert.False(context.Response.Headers.ContainsKey("X-Powered-By"));
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
