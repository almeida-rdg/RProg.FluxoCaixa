using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RProg.FluxoCaixa.Lancamentos.Application.Commands;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;
using RProg.FluxoCaixa.Lancamentos.Infrastructure;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;
using Xunit;

namespace RProg.FluxoCaixa.Lancamentos.Test.Application.Commands
{    public class RegistrarLancamentoHandlerTest
    {
        private readonly Mock<IMensageriaPublisher> _mensageriaPublisherMock;
        private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
        private readonly Mock<ILogger<RegistrarLancamentoHandler>> _loggerMock;
        private readonly RegistrarLancamentoHandler _handler;
        private readonly Faker _faker;        public RegistrarLancamentoHandlerTest()
        {
            _mensageriaPublisherMock = new Mock<IMensageriaPublisher>();
            _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
            _loggerMock = new Mock<ILogger<RegistrarLancamentoHandler>>();
            _handler = new RegistrarLancamentoHandler(_mensageriaPublisherMock.Object, _lancamentoRepositoryMock.Object, _loggerMock.Object);
            _faker = new Faker("pt_BR");
        }

        [Fact]
        public async Task Handle_DadoComandoValido_DeveRegistrarLancamentoComSucesso()
        {
            // Arrange - Dado um comando válido
            var comando = CriarComandoValido();
            var lancamentoId = _faker.Random.Int(1, 1000);

            _lancamentoRepositoryMock
                .Setup(x => x.CriarLancamentoAsync(It.IsAny<Lancamento>()))
                .ReturnsAsync(lancamentoId);

            _mensageriaPublisherMock
                .Setup(x => x.PublicarMensagemAsync(It.IsAny<RegistrarLancamentoCommand>()))
                .Returns(Task.CompletedTask);

            // Act - Quando processar o comando
            var resultado = await _handler.Handle(comando, CancellationToken.None);

            // Assert - Então deve registrar o lançamento com sucesso
            resultado.Should().Be(lancamentoId);
            _lancamentoRepositoryMock.Verify(x => x.CriarLancamentoAsync(It.IsAny<Lancamento>()), Times.Once);
            _mensageriaPublisherMock.Verify(x => x.PublicarMensagemAsync(comando), Times.Once);
        }        [Fact]
        public async Task Handle_DadoValorZero_DeveLancarExcecaoRegraDeNegocio()
        {
            // Arrange - Dado um comando com valor zero
            var comando = CriarComandoValido();
            comando.Valor = 0;

            // Act & Assert - Quando processar o comando então deve lançar exceção da entidade
            var excecao = await Assert.ThrowsAsync<ExcecaoRegraDeNegocio>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Message.Should().Be("O valor do lançamento não pode ser zero.");
        }        [Fact]
        public async Task Handle_DadoCreditoComValorNegativo_DeveLancarExcecaoRegraDeNegocio()
        {
            // Arrange - Dado um comando de crédito com valor negativo
            var comando = CriarComandoValido();
            comando.Tipo = TipoLancamento.Credito;
            comando.Valor = -100;

            // Act & Assert - Quando processar o comando então deve lançar exceção da entidade
            var excecao = await Assert.ThrowsAsync<ExcecaoRegraDeNegocio>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Message.Should().Be("Lançamentos de crédito não podem ter valores negativos.");
        }        [Fact]
        public async Task Handle_DadoDebitoComValorPositivo_DeveLancarExcecaoRegraDeNegocio()
        {
            // Arrange - Dado um comando de débito com valor positivo
            var comando = CriarComandoValido();
            comando.Tipo = TipoLancamento.Debito;
            comando.Valor = 100;

            // Act & Assert - Quando processar o comando então deve lançar exceção da entidade
            var excecao = await Assert.ThrowsAsync<ExcecaoRegraDeNegocio>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Message.Should().Be("Lançamentos de débito não podem ter valores positivos.");        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_DadoDescricaoNulaOuVazia_DeveLancarExcecaoDadosInvalidos(string? descricaoInvalida)
        {
            // Arrange - Dado um comando com descrição inválida
            var comando = CriarComandoValido();
            comando.Descricao = descricaoInvalida!;

            // Act & Assert - Quando processar o comando então deve lançar exceção
            var excecao = await Assert.ThrowsAsync<ExcecaoDadosInvalidos>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Message.Should().Be("A descrição é obrigatória.");
        }        [Theory]
        [InlineData("ab")]
        [InlineData("a1")]
        [InlineData("12")]
        [InlineData("!!")]
        public async Task Handle_DadoDescricaoComMenosDeTresLetras_DeveLancarExcecaoRegraDeNegocio(string descricaoInvalida)
        {
            // Arrange - Dado um comando com descrição com menos de 3 letras
            var comando = CriarComandoValido();
            comando.Descricao = descricaoInvalida;

            // Act & Assert - Quando processar o comando então deve lançar exceção da entidade
            var excecao = await Assert.ThrowsAsync<ExcecaoRegraDeNegocio>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Message.Should().Be("A descrição deve conter pelo menos 3 letras.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_DadoCategoriaNulaOuVazia_DeveLancarExcecaoDadosInvalidos(string? categoriaInvalida)
        {
            // Arrange - Dado um comando com categoria inválida
            var comando = CriarComandoValido();
            comando.Categoria = categoriaInvalida!;

            // Act & Assert - Quando processar o comando então deve lançar exceção
            var excecao = await Assert.ThrowsAsync<ExcecaoDadosInvalidos>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Message.Should().Be("A categoria é obrigatória.");
        }        [Theory]
        [InlineData("ab")]
        [InlineData("a1")]
        [InlineData("12")]
        [InlineData("!!")]
        public async Task Handle_DadoCategoriaComMenosDeTresLetras_DeveLancarExcecaoRegraDeNegocio(string categoriaInvalida)
        {
            // Arrange - Dado um comando com categoria com menos de 3 letras
            var comando = CriarComandoValido();
            comando.Categoria = categoriaInvalida;

            // Act & Assert - Quando processar o comando então deve lançar exceção da entidade
            var excecao = await Assert.ThrowsAsync<ExcecaoRegraDeNegocio>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Message.Should().Be("A categoria deve conter pelo menos 3 letras.");
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("a1b2c3")]
        [InlineData("ABC")]
        [InlineData("Comida123")]
        public async Task Handle_DadoDescricaoComPeloMenosTresLetras_DeveSerValida(string descricaoValida)
        {
            // Arrange - Dado um comando com descrição válida
            var comando = CriarComandoValido();
            comando.Descricao = descricaoValida;
            var lancamentoId = _faker.Random.Int(1, 1000);

            _lancamentoRepositoryMock
                .Setup(x => x.CriarLancamentoAsync(It.IsAny<Lancamento>()))
                .ReturnsAsync(lancamentoId);

            _mensageriaPublisherMock
                .Setup(x => x.PublicarMensagemAsync(It.IsAny<RegistrarLancamentoCommand>()))
                .Returns(Task.CompletedTask);

            // Act - Quando processar o comando
            var resultado = await _handler.Handle(comando, CancellationToken.None);

            // Assert - Então deve processar sem exceção
            resultado.Should().Be(lancamentoId);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("a1b2c3")]
        [InlineData("ABC")]
        [InlineData("Alimentacao123")]
        public async Task Handle_DadoCategoriaComPeloMenosTresLetras_DeveSerValida(string categoriaValida)
        {
            // Arrange - Dado um comando com categoria válida
            var comando = CriarComandoValido();
            comando.Categoria = categoriaValida;
            var lancamentoId = _faker.Random.Int(1, 1000);

            _lancamentoRepositoryMock
                .Setup(x => x.CriarLancamentoAsync(It.IsAny<Lancamento>()))
                .ReturnsAsync(lancamentoId);

            _mensageriaPublisherMock
                .Setup(x => x.PublicarMensagemAsync(It.IsAny<RegistrarLancamentoCommand>()))
                .Returns(Task.CompletedTask);

            // Act - Quando processar o comando
            var resultado = await _handler.Handle(comando, CancellationToken.None);

            // Assert - Então deve processar sem exceção
            resultado.Should().Be(lancamentoId);
        }

        [Fact]
        public async Task Handle_DadoCreditoComValorPositivo_DeveProcessarComSucesso()
        {
            // Arrange - Dado um comando de crédito com valor positivo
            var comando = CriarComandoValido();
            comando.Tipo = TipoLancamento.Credito;
            comando.Valor = 100;
            var lancamentoId = _faker.Random.Int(1, 1000);

            _lancamentoRepositoryMock
                .Setup(x => x.CriarLancamentoAsync(It.IsAny<Lancamento>()))
                .ReturnsAsync(lancamentoId);

            _mensageriaPublisherMock
                .Setup(x => x.PublicarMensagemAsync(It.IsAny<RegistrarLancamentoCommand>()))
                .Returns(Task.CompletedTask);

            // Act - Quando processar o comando
            var resultado = await _handler.Handle(comando, CancellationToken.None);

            // Assert - Então deve processar com sucesso
            resultado.Should().Be(lancamentoId);
        }

        [Fact]
        public async Task Handle_DadoDebitoComValorNegativo_DeveProcessarComSucesso()
        {
            // Arrange - Dado um comando de débito com valor negativo
            var comando = CriarComandoValido();
            comando.Tipo = TipoLancamento.Debito;
            comando.Valor = -100;
            var lancamentoId = _faker.Random.Int(1, 1000);

            _lancamentoRepositoryMock
                .Setup(x => x.CriarLancamentoAsync(It.IsAny<Lancamento>()))
                .ReturnsAsync(lancamentoId);

            _mensageriaPublisherMock
                .Setup(x => x.PublicarMensagemAsync(It.IsAny<RegistrarLancamentoCommand>()))
                .Returns(Task.CompletedTask);

            // Act - Quando processar o comando
            var resultado = await _handler.Handle(comando, CancellationToken.None);

            // Assert - Então deve processar com sucesso
            resultado.Should().Be(lancamentoId);
        }

        [Fact]
        public async Task Handle_DadoErroNaoCategorizado_DevePropagar()
        {
            // Arrange - Dado um erro não categorizado no repositório
            var comando = CriarComandoValido();
            var excecaoInfra = new InvalidOperationException("Erro de infraestrutura");

            _lancamentoRepositoryMock
                .Setup(x => x.CriarLancamentoAsync(It.IsAny<Lancamento>()))
                .ThrowsAsync(excecaoInfra);

            // Act & Assert - Quando processar o comando então deve propagar a exceção
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(comando, CancellationToken.None));
            excecao.Should().Be(excecaoInfra);
        }

        private RegistrarLancamentoCommand CriarComandoValido()
        {
            return new RegistrarLancamentoCommand
            {
                Valor = 100,
                Tipo = TipoLancamento.Credito,
                Data = DateTime.Now.AddDays(-1),
                Categoria = _faker.Commerce.Categories(1)[0],
                Descricao = _faker.Commerce.ProductDescription()
            };
        }
    }
}
