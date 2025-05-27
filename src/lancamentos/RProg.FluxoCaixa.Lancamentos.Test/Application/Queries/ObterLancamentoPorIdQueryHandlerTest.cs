using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Lancamentos.Application.Queries;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;

namespace RProg.FluxoCaixa.Lancamentos.Test.Application.Queries
{
    public class ObterLancamentoPorIdQueryHandlerTest
    {
        private readonly Mock<ILancamentoRepository> _repositorioMock;
        private readonly Mock<ILogger<ObterLancamentoPorIdQueryHandler>> _loggerMock;
        private readonly ObterLancamentoPorIdQueryHandler _handler;

        public ObterLancamentoPorIdQueryHandlerTest()
        {
            _repositorioMock = new Mock<ILancamentoRepository>();
            _loggerMock = new Mock<ILogger<ObterLancamentoPorIdQueryHandler>>();
            _handler = new ObterLancamentoPorIdQueryHandler(_repositorioMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_DadoIdExistente_DeveRetornarLancamento()
        {
            // Dado
            var id = 1;
            var query = new ObterLancamentoPorIdQuery(id);
            var cancellationToken = CancellationToken.None;

            var lancamentoEsperado = new Lancamento(
                100m,
                TipoLancamento.Credito,
                DateTime.Now.AddDays(-1),
                "Receita",
                "Pagamento cliente");

            _repositorioMock
                .Setup(r => r.ObterPorIdAsync(id, cancellationToken))
                .ReturnsAsync(lancamentoEsperado);

            // Quando
            var resultado = await _handler.Handle(query, cancellationToken);

            // Então
            resultado.Should().NotBeNull();
            resultado.Should().BeEquivalentTo(lancamentoEsperado);

            _repositorioMock.Verify(r => r.ObterPorIdAsync(id, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_DadoIdInexistente_DeveRetornarNull()
        {
            // Dado
            var id = 999;
            var query = new ObterLancamentoPorIdQuery(id);
            var cancellationToken = CancellationToken.None;

            _repositorioMock
                .Setup(r => r.ObterPorIdAsync(id, cancellationToken))
                .ReturnsAsync((Lancamento?)null);

            // Quando
            var resultado = await _handler.Handle(query, cancellationToken);

            // Então
            resultado.Should().BeNull();

            _repositorioMock.Verify(r => r.ObterPorIdAsync(id, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_DadoExcecaoNoRepositorio_DevePropagar()
        {
            // Dado
            var id = 1;
            var query = new ObterLancamentoPorIdQuery(id);
            var cancellationToken = CancellationToken.None;
            var excecaoEsperada = new InvalidOperationException("Erro de banco");

            _repositorioMock
                .Setup(r => r.ObterPorIdAsync(id, cancellationToken))
                .ThrowsAsync(excecaoEsperada);

            // Quando
            var acao = async () => await _handler.Handle(query, cancellationToken);

            // Então
            await acao.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Erro de banco");

            _repositorioMock.Verify(r => r.ObterPorIdAsync(id, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_DadoCancelamentoSolicitado_DeveRespeitarCancellationToken()
        {
            // Dado
            var id = 1;
            var query = new ObterLancamentoPorIdQuery(id);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            _repositorioMock
                .Setup(r => r.ObterPorIdAsync(id, cancellationToken))
                .Returns(async (int _, CancellationToken ct) =>
                {
                    await Task.Delay(100, ct);
                    return new Lancamento(100m, TipoLancamento.Credito, DateTime.Now.AddDays(-1), "Receita", "Teste");
                });

            cancellationTokenSource.Cancel();

            // Quando
            var acao = async () => await _handler.Handle(query, cancellationToken);

            // Então
            await acao.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
