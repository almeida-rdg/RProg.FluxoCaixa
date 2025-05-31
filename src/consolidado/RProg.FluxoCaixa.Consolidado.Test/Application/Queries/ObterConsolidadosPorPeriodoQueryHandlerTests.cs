using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Consolidado.Application.DTOs;
using RProg.FluxoCaixa.Consolidado.Application.Queries;
using RProg.FluxoCaixa.Consolidado.Domain.Entities;
using RProg.FluxoCaixa.Consolidado.Infrastructure.Data;

namespace RProg.FluxoCaixa.Consolidado.Test.Application.Queries
{
    /// <summary>
    /// Testes unitários para ObterConsolidadosPorPeriodoQueryHandler.
    /// </summary>
    public class ObterConsolidadosPorPeriodoQueryHandlerTests
    {
        private readonly Mock<IConsolidadoDiarioRepository> _repositoryMock;
        private readonly Mock<ILogger<ObterConsolidadosPorPeriodoQueryHandler>> _loggerMock;
        private readonly ObterConsolidadosPorPeriodoQueryHandler _handler;

        public ObterConsolidadosPorPeriodoQueryHandlerTests()
        {
            _repositoryMock = new Mock<IConsolidadoDiarioRepository>();
            _loggerMock = new Mock<ILogger<ObterConsolidadosPorPeriodoQueryHandler>>();
            _handler = new ObterConsolidadosPorPeriodoQueryHandler(_repositoryMock.Object, _loggerMock.Object);
        }

        [Theory]
        [InlineData("GERAL")]
        [InlineData("CATEGORIA")]
        public async Task Handle_ComQueryValida_DeveRetornarConsolidadosDoTipo(string tipoConsolidacao)
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);
            var cancellationToken = CancellationToken.None;

            var consolidadosEsperados = new List<ConsolidadoDiario>
            {
                new ConsolidadoDiario
                {
                    Id = 1,
                    Data = new DateTime(2024, 1, 15),
                    Categoria = tipoConsolidacao == "CATEGORIA" ? "ALIMENTACAO" : null,
                    TotalCreditos = 1000m,
                    TotalDebitos = -500m,
                    QuantidadeLancamentos = 10,
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                }
            };

            var ultimaConsolidacao = DateTime.UtcNow.AddHours(-1);

            _repositoryMock
                .Setup(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .ReturnsAsync(consolidadosEsperados);

            _repositoryMock
                .Setup(r => r.ObterUltimaDataAtualizacaoPorTipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .ReturnsAsync(ultimaConsolidacao);

            // Act
            var resultado = await _handler.Handle(query, cancellationToken);

            // Assert
            resultado.Should().NotBeNull();
            resultado.DataInicial.Should().Be(dataInicial);
            resultado.DataFinal.Should().Be(dataFinal);
            resultado.UltimaConsolidacao.Should().Be(ultimaConsolidacao);
            resultado.TotalRegistros.Should().Be(1);
            resultado.Consolidados.Should().HaveCount(1);

            var consolidado = resultado.Consolidados.First();
            consolidado.Data.Should().Be(consolidadosEsperados.First().Data);
            consolidado.TotalCreditos.Should().Be(consolidadosEsperados.First().TotalCreditos);
            consolidado.TotalDebitos.Should().Be(consolidadosEsperados.First().TotalDebitos);
            consolidado.QuantidadeLancamentos.Should().Be(consolidadosEsperados.First().QuantidadeLancamentos);

            _repositoryMock.Verify(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
            _repositoryMock.Verify(r => r.ObterUltimaDataAtualizacaoPorTipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_ComTipoGeralSemDados_DeveRetornarListaVazia()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var tipoConsolidacao = "GERAL";
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);
            var cancellationToken = CancellationToken.None;

            _repositoryMock
                .Setup(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .ReturnsAsync(new List<ConsolidadoDiario>());

            _repositoryMock
                .Setup(r => r.ObterUltimaDataAtualizacaoPorTipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .ReturnsAsync((DateTime?)null);

            // Act
            var resultado = await _handler.Handle(query, cancellationToken);

            // Assert
            resultado.Should().NotBeNull();
            resultado.Consolidados.Should().BeEmpty();
            resultado.TotalRegistros.Should().Be(0);
            resultado.UltimaConsolidacao.Should().BeNull();

            _repositoryMock.Verify(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
            _repositoryMock.Verify(r => r.ObterUltimaDataAtualizacaoPorTipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_ComTipoCategoriaComDados_DeveRetornarConsolidadosComCategoria()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var tipoConsolidacao = "CATEGORIA";
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);
            var cancellationToken = CancellationToken.None;

            var consolidadosEsperados = new List<ConsolidadoDiario>
            {
                new ConsolidadoDiario
                {
                    Id = 1,
                    Data = new DateTime(2024, 1, 15),
                    Categoria = "ALIMENTACAO",
                    TotalCreditos = 800m,
                    TotalDebitos = -300m,
                    QuantidadeLancamentos = 5,
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                },
                new ConsolidadoDiario
                {
                    Id = 2,
                    Data = new DateTime(2024, 1, 20),
                    Categoria = "TRANSPORTE",
                    TotalCreditos = 200m,
                    TotalDebitos = -150m,
                    QuantidadeLancamentos = 3,
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                }
            };

            var ultimaConsolidacao = DateTime.UtcNow.AddMinutes(-30);

            _repositoryMock
                .Setup(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .ReturnsAsync(consolidadosEsperados);

            _repositoryMock
                .Setup(r => r.ObterUltimaDataAtualizacaoPorTipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .ReturnsAsync(ultimaConsolidacao);

            // Act
            var resultado = await _handler.Handle(query, cancellationToken);

            // Assert
            resultado.Should().NotBeNull();
            resultado.TotalRegistros.Should().Be(2);
            resultado.Consolidados.Should().HaveCount(2);
            resultado.UltimaConsolidacao.Should().Be(ultimaConsolidacao);

            // Verificar primeiro consolidado
            var primeiroConsolidado = resultado.Consolidados.First();
            primeiroConsolidado.Categoria.Should().Be("ALIMENTACAO");
            primeiroConsolidado.TotalCreditos.Should().Be(800m);
            primeiroConsolidado.TotalDebitos.Should().Be(-300m);

            // Verificar segundo consolidado
            var segundoConsolidado = resultado.Consolidados.Skip(1).First();
            segundoConsolidado.Categoria.Should().Be("TRANSPORTE");
            segundoConsolidado.TotalCreditos.Should().Be(200m);
            segundoConsolidado.TotalDebitos.Should().Be(-150m);

            _repositoryMock.Verify(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
            _repositoryMock.Verify(r => r.ObterUltimaDataAtualizacaoPorTipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_ComExcecaoNoRepositorio_DevePropagar()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var tipoConsolidacao = "GERAL";
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);
            var cancellationToken = CancellationToken.None;
            var excecaoEsperada = new InvalidOperationException("Erro de banco");

            _repositoryMock
                .Setup(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .ThrowsAsync(excecaoEsperada);

            // Act & Assert
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(query, cancellationToken));

            excecao.Should().Be(excecaoEsperada);
            _repositoryMock.Verify(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
            _repositoryMock.Verify(r => r.ObterUltimaDataAtualizacaoPorTipoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ComCancellationToken_DeveRespeitarCancelamento()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var tipoConsolidacao = "GERAL";
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            _repositoryMock
                .Setup(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken))
                .Returns(async (DateTime _, DateTime _, string _, CancellationToken ct) =>
                {
                    await Task.Delay(100, ct);
                    return new List<ConsolidadoDiario>();
                });

            // Act - Cancelar durante a execução
            cancellationTokenSource.Cancel();

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => _handler.Handle(query, cancellationToken));

            _repositoryMock.Verify(r => r.ObterPorPeriodoETipoAsync(dataInicial, dataFinal, tipoConsolidacao, cancellationToken), Times.Once);
        }
    }
}
