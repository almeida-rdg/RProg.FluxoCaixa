using Bogus;
using FluentAssertions;
using Moq;
using RProg.FluxoCaixa.Lancamentos.Application.Queries;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;
using Xunit;

namespace RProg.FluxoCaixa.Lancamentos.Test.Application.Queries
{    public class ObterLancamentosPorDataQueryHandlerTest
    {
        private readonly Mock<ILancamentoRepository> _repositorioMock;
        private readonly ObterLancamentosPorDataQueryHandler _handler;
        private readonly Faker _faker;

        public ObterLancamentosPorDataQueryHandlerTest()
        {
            _repositorioMock = new Mock<ILancamentoRepository>();
            _handler = new ObterLancamentosPorDataQueryHandler(_repositorioMock.Object);
            _faker = new Faker("pt_BR");
        }

        [Fact]
        public async Task Handle_DadaQueryValida_DeveRetornarLancamentosDaData()
        {
            // Arrange - Dada uma query válida
            var data = _faker.Date.Recent();
            var query = new ObterLancamentosPorDataQuery(data);
            var lancamentosEsperados = CriarListaLancamentos();            _repositorioMock
                .Setup(x => x.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamentosEsperados);

            // Act - Quando processar a query
            var resultado = await _handler.Handle(query, CancellationToken.None);

            // Assert - Então deve retornar os lançamentos da data
            resultado.Should().BeEquivalentTo(lancamentosEsperados);
            _repositorioMock.Verify(x => x.ObterPorDataAsync(data, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DadaDataSemLancamentos_DeveRetornarListaVazia()
        {
            // Arrange - Dada uma data sem lançamentos
            var data = _faker.Date.Recent();
            var query = new ObterLancamentosPorDataQuery(data);
            var lancamentosVazios = Enumerable.Empty<Lancamento>();            _repositorioMock
                .Setup(x => x.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamentosVazios);

            // Act - Quando processar a query
            var resultado = await _handler.Handle(query, CancellationToken.None);

            // Assert - Então deve retornar lista vazia            resultado.Should().BeEmpty();
            _repositorioMock.Verify(x => x.ObterPorDataAsync(data, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DadaDataEspecifica_DeveChamarRepositorioComDataCorreta()
        {
            // Arrange - Dada uma data específica
            var dataEspecifica = new DateTime(2024, 12, 25);
            var query = new ObterLancamentosPorDataQuery(dataEspecifica);
            var lancamentos = CriarListaLancamentos();            _repositorioMock
                .Setup(x => x.ObterPorDataAsync(dataEspecifica, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamentos);

            // Act - Quando processar a query
            await _handler.Handle(query, CancellationToken.None);

            // Assert - Então deve chamar o repositório com a data correta
            _repositorioMock.Verify(x => x.ObterPorDataAsync(dataEspecifica, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DadoErroNoRepositorio_DevePropagar()
        {
            // Arrange - Dado um erro no repositório
            var data = _faker.Date.Recent();
            var query = new ObterLancamentosPorDataQuery(data);
            var excecaoRepositorio = new InvalidOperationException("Erro no banco de dados");            _repositorioMock
                .Setup(x => x.ObterPorDataAsync(data, It.IsAny<CancellationToken>()))
                .ThrowsAsync(excecaoRepositorio);

            // Act & Assert - Quando processar a query então deve propagar a exceção
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(query, CancellationToken.None));
            excecao.Should().Be(excecaoRepositorio);
        }

        [Fact]
        public async Task Handle_DadoCancellationTokenCancelado_DeveRespeitarCancelamento()
        {
            // Arrange - Dado um token de cancelamento cancelado
            var data = _faker.Date.Recent();
            var query = new ObterLancamentosPorDataQuery(data);
            var cancellationToken = new CancellationToken(canceled: true);            _repositorioMock
                .Setup(x => x.ObterPorDataAsync(data, It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
                .ThrowsAsync(new OperationCanceledException());// Act & Assert - Quando processar a query com token cancelado então deve respeitar o cancelamento
            await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.Handle(query, cancellationToken));
        }        private IEnumerable<Lancamento> CriarListaLancamentos()
        {
            return new List<Lancamento>
            {
                new(
                    100,  // Crédito positivo
                    TipoLancamento.Credito,
                    _faker.Date.Recent(),
                    "Categoria",
                    _faker.Commerce.ProductDescription()
                ),
                new(
                    -200,  // Débito negativo
                    TipoLancamento.Debito,
                    _faker.Date.Recent(),
                    "Categoria",
                    _faker.Commerce.ProductDescription()
                )
            };
        }
    }
}
