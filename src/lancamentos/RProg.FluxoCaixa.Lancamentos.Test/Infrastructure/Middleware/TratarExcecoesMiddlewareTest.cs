using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Middleware;
using System.IO;
using System.Net;
using System.Text.Json;
using Xunit;

namespace RProg.FluxoCaixa.Lancamentos.Test.Infrastructure.Middleware
{
    public class TratarExcecoesMiddlewareTest
    {
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly Mock<ILogger<TratarExcecoesMiddleware>> _loggerMock;
        private readonly TratarExcecoesMiddleware _middleware;

        public TratarExcecoesMiddlewareTest()
        {
            _nextMock = new Mock<RequestDelegate>();
            _loggerMock = new Mock<ILogger<TratarExcecoesMiddleware>>();
            _middleware = new TratarExcecoesMiddleware(_nextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task InvokeAsync_DadoRequestSemExcecao_DeveExecutarProximoMiddleware()
        {
            // Arrange - Dado um contexto HTTP válido
            var contexto = CriarHttpContext();

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .Returns(Task.CompletedTask);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve chamar o próximo middleware
            _nextMock.Verify(x => x(contexto), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_DadoExcecaoDadosInvalidos_DeveRetornarBadRequest()
        {
            // Arrange - Dado uma exceção de dados inválidos
            var contexto = CriarHttpContext();
            var mensagemErro = "Dados inválidos fornecidos";
            var excecao = new ExcecaoDadosInvalidos(mensagemErro);

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve retornar BadRequest
            contexto.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var resposta = await ObterRespostaDoContexto(contexto);
            resposta.Mensagem.Should().Be(mensagemErro);
            resposta.Tipo.Should().Be("DadosInvalidos");
            resposta.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task InvokeAsync_DadoExcecaoRecursoNaoEncontrado_DeveRetornarNotFound()
        {
            // Arrange - Dado uma exceção de recurso não encontrado
            var contexto = CriarHttpContext();
            var mensagemErro = "Recurso não encontrado";
            var excecao = new ExcecaoRecursoNaoEncontrado(mensagemErro);

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve retornar NotFound
            contexto.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            var resposta = await ObterRespostaDoContexto(contexto);
            resposta.Mensagem.Should().Be(mensagemErro);
            resposta.Tipo.Should().Be("RecursoNaoEncontrado");
            resposta.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task InvokeAsync_DadoExcecaoRegraDeNegocio_DeveRetornarUnprocessableEntity()
        {
            // Arrange - Dado uma exceção de regra de negócio
            var contexto = CriarHttpContext();
            var mensagemErro = "Violação de regra de negócio";
            var excecao = new ExcecaoRegraDeNegocio(mensagemErro);

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve retornar UnprocessableEntity
            contexto.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
            var resposta = await ObterRespostaDoContexto(contexto);
            resposta.Mensagem.Should().Be(mensagemErro);
            resposta.Tipo.Should().Be("RegraDeNegocio");
            resposta.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task InvokeAsync_DadoExcecaoGenerica_DeveRetornarInternalServerError()
        {
            // Arrange - Dado uma exceção genérica
            var contexto = CriarHttpContext();
            var excecao = new InvalidOperationException("Erro interno");

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve retornar InternalServerError
            contexto.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            var resposta = await ObterRespostaDoContexto(contexto);
            resposta.Mensagem.Should().Be("Ocorreu um erro interno no servidor. Tente novamente em alguns minutos.");
            resposta.Tipo.Should().Be("ErroInterno");
            resposta.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task InvokeAsync_DadoQualquerExcecao_DeveDefinirContentTypeComoJson()
        {
            // Arrange - Dado qualquer exceção
            var contexto = CriarHttpContext();
            var excecao = new ExcecaoDadosInvalidos("Teste");

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve definir ContentType como JSON
            contexto.Response.ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task InvokeAsync_DadoQualquerExcecao_DeveIncluirTraceId()
        {
            // Arrange - Dado qualquer exceção
            var contexto = CriarHttpContext();
            var excecao = new ExcecaoDadosInvalidos("Teste");

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve incluir TraceId na resposta
            var resposta = await ObterRespostaDoContexto(contexto);
            resposta.TraceId.Should().Be(contexto.TraceIdentifier);
        }

        [Fact]
        public async Task InvokeAsync_DadoExcecaoDeInfraestrutura_DeveLogarComoError()
        {
            // Arrange - Dado uma exceção de infraestrutura
            var contexto = CriarHttpContext();
            var excecao = new InvalidOperationException("Erro de infraestrutura");

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve logar como Error
            VerificarLogLevel(LogLevel.Error);
        }

        [Fact]
        public async Task InvokeAsync_DadoExcecaoDeNegocio_DeveLogarComoWarning()
        {
            // Arrange - Dado uma exceção de negócio
            var contexto = CriarHttpContext();
            var excecao = new ExcecaoDadosInvalidos("Dados inválidos");

            _nextMock
                .Setup(x => x(It.IsAny<HttpContext>()))
                .ThrowsAsync(excecao);

            // Act - Quando executar o middleware
            await _middleware.InvokeAsync(contexto);

            // Assert - Então deve logar como Warning
            VerificarLogLevel(LogLevel.Warning);
        }

        private HttpContext CriarHttpContext()
        {
            var contexto = new DefaultHttpContext();
            contexto.Response.Body = new MemoryStream();
            contexto.TraceIdentifier = Guid.NewGuid().ToString();
            return contexto;
        }

        private async Task<RespostaErro> ObterRespostaDoContexto(HttpContext contexto)
        {
            contexto.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(contexto.Response.Body);
            var json = await reader.ReadToEndAsync();
            
            return JsonSerializer.Deserialize<RespostaErro>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })!;
        }

        private void VerificarLogLevel(LogLevel expectedLevel)
        {
            _loggerMock.Verify(
                x => x.Log(
                    expectedLevel,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
