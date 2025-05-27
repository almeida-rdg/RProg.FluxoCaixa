using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Lancamentos.Application.Queries;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;

namespace RProg.FluxoCaixa.Lancamentos.Test.Application.Queries
{
    public class ObterLancamentosPorIntervaloQueryHandlerTest
    {
        private readonly Mock<ILancamentoRepository> _repositorioMock;
        private readonly Mock<ILogger<ObterLancamentosPorIntervaloQueryHandler>> _loggerMock;
        private readonly ObterLancamentosPorIntervaloQueryHandler _handler;

        public ObterLancamentosPorIntervaloQueryHandlerTest()
        {
            _repositorioMock = new Mock<ILancamentoRepository>();
            _loggerMock = new Mock<ILogger<ObterLancamentosPorIntervaloQueryHandler>>();
            _handler = new ObterLancamentosPorIntervaloQueryHandler(_repositorioMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_DadoIntervaloValido_DeveRetornarLancamentos()
        {
            // Dado
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var query = new ObterLancamentosPorIntervaloQuery(dataInicial, dataFinal);
            var cancellationToken = CancellationToken.None;

            var lancamentosEsperados = new List<Lancamento>
            {
                new Lancamento(100m, TipoLancamento.Credito, new DateTime(2024, 1, 15), "Receita", "Pagamento 1"),
                new Lancamento(-50m, TipoLancamento.Debito, new DateTime(2024, 1, 20), "Despesa", "Compra 1")
            };

            _repositorioMock
                .Setup(r => r.ObterPorIntervaloAsync(dataInicial, dataFinal, cancellationToken))
                .ReturnsAsync(lancamentosEsperados);

            // Quando
            var resultado = await _handler.Handle(query, cancellationToken);

            // Então
            resultado.Should().NotBeNull();
            resultado.Should().HaveCount(2);
            resultado.Should().BeEquivalentTo(lancamentosEsperados);

            _repositorioMock.Verify(r => r.ObterPorIntervaloAsync(dataInicial, dataFinal, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_DadoIntervaloSemResultados_DeveRetornarListaVazia()
        {
            // Dado
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var query = new ObterLancamentosPorIntervaloQuery(dataInicial, dataFinal);
            var cancellationToken = CancellationToken.None;

            _repositorioMock
                .Setup(r => r.ObterPorIntervaloAsync(dataInicial, dataFinal, cancellationToken))
                .ReturnsAsync(new List<Lancamento>());

            // Quando
            var resultado = await _handler.Handle(query, cancellationToken);

            // Então
            resultado.Should().NotBeNull();
            resultado.Should().BeEmpty();

            _repositorioMock.Verify(r => r.ObterPorIntervaloAsync(dataInicial, dataFinal, cancellationToken), Times.Once);
        }        [Fact]
        public void Handle_DadoDataInicialIgualDataFinal_DeveLancarExcecao()
        {
            // Dado
            var data = new DateTime(2024, 1, 15);

            // Quando
            var acao = () => new ObterLancamentosPorIntervaloQuery(data, data);

            // Então
            acao.Should().Throw<ExcecaoDadosInvalidos>()
                .WithMessage("A data inicial deve ser inferior à data final.");

            _repositorioMock.Verify(r => r.ObterPorIntervaloAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }        [Fact]
        public void Handle_DadoDataInicialMaiorQueDataFinal_DeveLancarExcecao()
        {
            // Dado
            var dataInicial = new DateTime(2024, 1, 31);
            var dataFinal = new DateTime(2024, 1, 1);

            // Quando
            var acao = () => new ObterLancamentosPorIntervaloQuery(dataInicial, dataFinal);

            // Então
            acao.Should().Throw<ExcecaoDadosInvalidos>()
                .WithMessage("A data inicial deve ser inferior à data final.");
        }

        [Fact]
        public async Task Handle_DadoExcecaoNoRepositorio_DevePropagar()
        {
            // Dado
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var query = new ObterLancamentosPorIntervaloQuery(dataInicial, dataFinal);
            var cancellationToken = CancellationToken.None;
            var excecaoEsperada = new InvalidOperationException("Erro de banco");

            _repositorioMock
                .Setup(r => r.ObterPorIntervaloAsync(dataInicial, dataFinal, cancellationToken))
                .ThrowsAsync(excecaoEsperada);

            // Quando
            var acao = async () => await _handler.Handle(query, cancellationToken);

            // Então
            await acao.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Erro de banco");

            _repositorioMock.Verify(r => r.ObterPorIntervaloAsync(dataInicial, dataFinal, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_DadoCancelamentoSolicitado_DeveRespeitarCancellationToken()
        {
            // Dado
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var query = new ObterLancamentosPorIntervaloQuery(dataInicial, dataFinal);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            _repositorioMock
                .Setup(r => r.ObterPorIntervaloAsync(dataInicial, dataFinal, cancellationToken))
                .Returns(async (DateTime _, DateTime _, CancellationToken ct) =>
                {
                    await Task.Delay(100, ct);
                    return new List<Lancamento>();
                });

            cancellationTokenSource.Cancel();

            // Quando
            var acao = async () => await _handler.Handle(query, cancellationToken);

            // Então
            await acao.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
