using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Worker.Domain.DTOs;
using RProg.FluxoCaixa.Worker.Domain.Entities;
using RProg.FluxoCaixa.Worker.Infrastructure.Data;
using RProg.FluxoCaixa.Worker.Services;

namespace RProg.FluxoCaixa.Worker.Test.Services
{
    /// <summary>
    /// Testes unitários para o ConsolidacaoService focando nas regras de negócio.
    /// </summary>
    public class ConsolidacaoServiceTests
    {
        private readonly Mock<IConsolidadoRepository> _mockConsolidadoRepository;
        private readonly Mock<ILancamentoProcessadoRepository> _mockLancamentoProcessadoRepository;
        private readonly Mock<ILogger<ConsolidacaoService>> _mockLogger;
        private readonly ConsolidacaoService _service;

        public ConsolidacaoServiceTests()
        {
            _mockConsolidadoRepository = new Mock<IConsolidadoRepository>();
            _mockLancamentoProcessadoRepository = new Mock<ILancamentoProcessadoRepository>();
            _mockLogger = new Mock<ILogger<ConsolidacaoService>>();
            
            _service = new ConsolidacaoService(
                _mockConsolidadoRepository.Object,
                _mockLancamentoProcessadoRepository.Object,
                _mockLogger.Object);
        }

        #region Testes de Idempotência - Regra de Negócio Crítica

        [Fact]
        public async Task ProcessarLancamentoAsync_QuandoLancamentoJaProcessado_DeveRetornarFalseESemProcessar()
        {
            // Arrange - Given
            var lancamento = new LancamentoDto
            {
                Id = 1,
                Valor = 100.00m,
                Data = DateTime.Today,
                Categoria = "Alimentação",
                Tipo = 2 // Crédito
            };

            _mockLancamentoProcessadoRepository
                .Setup(x => x.JaFoiProcessadoAsync(lancamento.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act - When
            var resultado = await _service.ProcessarLancamentoAsync(lancamento, CancellationToken.None);

            // Assert - Then
            resultado.Should().BeFalse("lançamento já foi processado anteriormente");
            
            // Verificar que não tentou consolidar
            _mockConsolidadoRepository.Verify(
                x => x.ObterOuCriarConsolidadoAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "não deve tentar consolidar lançamento já processado");
        }

        [Fact]
        public async Task ProcessarLancamentoAsync_QuandoLancamentoNovo_DeveProcessarERetornarTrue()
        {
            // Arrange - Given
            var lancamento = new LancamentoDto
            {
                Id = 2,
                Valor = 150.00m,
                Data = DateTime.Today,
                Categoria = "Transporte",
                Tipo = 2 // Crédito
            };

            var consolidadoGeral = new ConsolidadoDiario
            {
                Data = DateTime.Today,
                Categoria = null,
                TotalCreditos = 0,
                TotalDebitos = 0,
                SaldoLiquido = 0,
                QuantidadeLancamentos = 0,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };
            
            var consolidadoCategoria = new ConsolidadoDiario
            {
                Data = DateTime.Today,
                Categoria = "Transporte",
                TotalCreditos = 0,
                TotalDebitos = 0,
                SaldoLiquido = 0,
                QuantidadeLancamentos = 0,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            _mockLancamentoProcessadoRepository
                .Setup(x => x.JaFoiProcessadoAsync(lancamento.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockConsolidadoRepository
                .Setup(x => x.ObterOuCriarConsolidadoAsync(lancamento.Data.Date, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(consolidadoGeral);

            _mockConsolidadoRepository
                .Setup(x => x.ObterOuCriarConsolidadoAsync(lancamento.Data.Date, lancamento.Categoria, It.IsAny<CancellationToken>()))
                .ReturnsAsync(consolidadoCategoria);

            // Act - When
            var resultado = await _service.ProcessarLancamentoAsync(lancamento, CancellationToken.None);

            // Assert - Then
            resultado.Should().BeTrue("lançamento novo deve ser processado com sucesso");
            
            // Verificar que marcou como processado
            _mockLancamentoProcessadoRepository.Verify(
                x => x.MarcarComoProcessadoAsync(lancamento.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Once,
                "deve marcar lançamento como processado");
        }

        #endregion

        #region Testes de Regras de Consolidação

        [Fact]
        public async Task ProcessarLancamentoAsync_LancamentoCredito_DeveAtualizarConsolidadoCorretamente()
        {
            // Arrange - Given
            var lancamento = new LancamentoDto
            {
                Id = 3,
                Valor = 500.00m,
                Data = DateTime.Today,
                Categoria = "Vendas",
                Tipo = 2 // Crédito
            };

            var consolidadoGeral = new ConsolidadoDiario
            {
                Data = DateTime.Today,
                Categoria = null,
                TotalCreditos = 0,
                TotalDebitos = 0,
                SaldoLiquido = 0,
                QuantidadeLancamentos = 0,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };
            
            var consolidadoCategoria = new ConsolidadoDiario
            {
                Data = DateTime.Today,
                Categoria = "Vendas",
                TotalCreditos = 0,
                TotalDebitos = 0,
                SaldoLiquido = 0,
                QuantidadeLancamentos = 0,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            _mockLancamentoProcessadoRepository
                .Setup(x => x.JaFoiProcessadoAsync(lancamento.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockConsolidadoRepository
                .Setup(x => x.ObterOuCriarConsolidadoAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DateTime data, string categoria, CancellationToken ct) => 
                    categoria == null ? consolidadoGeral : consolidadoCategoria);

            // Act - When
            await _service.ProcessarLancamentoAsync(lancamento, CancellationToken.None);

            // Assert - Then
            consolidadoGeral.TotalCreditos.Should().Be(500.00m, "crédito deve aumentar total de créditos");
            consolidadoGeral.TotalDebitos.Should().Be(0m, "crédito não deve afetar débitos");
            consolidadoGeral.QuantidadeLancamentos.Should().Be(1, "deve incrementar quantidade de lançamentos");

            consolidadoCategoria.TotalCreditos.Should().Be(500.00m, "categoria também deve ser atualizada");
            consolidadoCategoria.QuantidadeLancamentos.Should().Be(1);
        }

        [Fact]
        public async Task ProcessarLancamentoAsync_LancamentoDebito_DeveAtualizarConsolidadoCorretamente()
        {
            // Arrange - Given
            var lancamento = new LancamentoDto
            {
                Id = 4,
                Valor = -250.00m,
                Data = DateTime.Today,
                Categoria = "Despesas",
                Tipo = 1 // Débito
            };

            var consolidadoGeral = new ConsolidadoDiario
            {
                Data = DateTime.Today,
                Categoria = null,
                TotalCreditos = 0,
                TotalDebitos = 0,
                SaldoLiquido = 0,
                QuantidadeLancamentos = 0,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };
            
            var consolidadoCategoria = new ConsolidadoDiario
            {
                Data = DateTime.Today,
                Categoria = "Despesas",
                TotalCreditos = 0,
                TotalDebitos = 0,
                SaldoLiquido = 0,
                QuantidadeLancamentos = 0,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };

            _mockLancamentoProcessadoRepository
                .Setup(x => x.JaFoiProcessadoAsync(lancamento.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockConsolidadoRepository
                .Setup(x => x.ObterOuCriarConsolidadoAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DateTime data, string categoria, CancellationToken ct) => 
                    categoria == null ? consolidadoGeral : consolidadoCategoria);

            // Act - When
            await _service.ProcessarLancamentoAsync(lancamento, CancellationToken.None);

            // Assert - Then
            consolidadoGeral.TotalCreditos.Should().Be(0m, "débito não deve afetar créditos");
            consolidadoGeral.TotalDebitos.Should().Be(-250.00m, "débito deve aumentar total de débitos");
            consolidadoGeral.QuantidadeLancamentos.Should().Be(1, "deve incrementar quantidade de lançamentos");

            consolidadoCategoria.TotalDebitos.Should().Be(-250.00m, "categoria também deve ser atualizada");
            consolidadoCategoria.QuantidadeLancamentos.Should().Be(1);
        }

        #endregion

        #region Testes de Limpeza de Dados

        [Fact]
        public async Task LimparLancamentosProcessadosAntigosAsync_DeveExecutarLimpezaCorretamente()
        {
            // Arrange - Given
            const int diasParaManter = 45;
            const int registrosEsperados = 150;

            _mockConsolidadoRepository
                .Setup(x => x.LimparLancamentosProcessadosAntigosAsync(diasParaManter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(registrosEsperados);

            // Act - When
            var resultado = await _service.LimparLancamentosProcessadosAntigosAsync(diasParaManter, CancellationToken.None);

            // Assert - Then
            resultado.Should().Be(registrosEsperados, "deve retornar número correto de registros removidos");

            _mockConsolidadoRepository.Verify(
                x => x.LimparLancamentosProcessadosAntigosAsync(diasParaManter, It.IsAny<CancellationToken>()),
                Times.Once,
                "deve chamar repositório com parâmetros corretos");
        }

        #endregion

        #region Testes de Tratamento de Erros

        [Fact]
        public async Task ProcessarLancamentoAsync_QuandoRepositorioFalha_DevePropagar()
        {
            // Arrange - Given
            var lancamento = new LancamentoDto
            {
                Id = 6,
                Valor = 100.00m,
                Data = DateTime.Today,
                Tipo = 2 // Crédito
            };

            _mockLancamentoProcessadoRepository
                .Setup(x => x.JaFoiProcessadoAsync(lancamento.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockConsolidadoRepository
                .Setup(x => x.ObterOuCriarConsolidadoAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Erro no banco de dados"));

            // Act & Assert - When & Then
            var action = async () => await _service.ProcessarLancamentoAsync(lancamento, CancellationToken.None);
            
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Erro no banco de dados",
                "deve propagar exceções do repositório");
        }

        #endregion
    }
}
