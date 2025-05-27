using Bogus;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RProg.FluxoCaixa.Lancamentos.Application.Commands;
using RProg.FluxoCaixa.Lancamentos.Application.Queries;
using RProg.FluxoCaixa.Lancamentos.Controllers;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;

namespace RProg.FluxoCaixa.Lancamentos.Test.Controllers
{
    public class LancamentosControllerTest
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly LancamentosController _controller;
        private readonly Faker _faker;

        public LancamentosControllerTest()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new LancamentosController(_mediatorMock.Object);
            _faker = new Faker("pt_BR");
        }        [Fact]
        public async Task RegistrarLancamento_DadoComandoValido_DeveRetornarCreated()
        {
            // Arrange - Dado um comando válido
            var comando = CriarComandoValido();
            var lancamentoId = _faker.Random.Int(1, 1000);

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<RegistrarLancamentoCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamentoId);

            // Act - Quando registrar o lançamento
            var resultado = await _controller.RegistrarLancamento(comando);

            // Assert - Então deve retornar Created
            var createdResult = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(LancamentosController.ObterLancamentoPorId));
            createdResult.RouteValues!["id"].Should().Be(lancamentoId);
            _mediatorMock.Verify(x => x.Send(comando, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegistrarLancamento_DadoComandoValido_DeveEnviarComandoParaMediator()
        {
            // Arrange - Dado um comando válido
            var comando = CriarComandoValido();

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<RegistrarLancamentoCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act - Quando registrar o lançamento
            await _controller.RegistrarLancamento(comando);

            // Assert - Então deve enviar o comando para o mediator
            _mediatorMock.Verify(x => x.Send(comando, It.IsAny<CancellationToken>()), Times.Once);
        }        [Fact]
        public async Task ObterLancamentosPorData_DadaDataValida_DeveRetornarOk()
        {
            // Arrange - Dada uma data válida
            var data = _faker.Date.Recent(1);
            var lancamentos = CriarListaLancamentos();

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<ObterLancamentosPorDataQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamentos);

            // Act - Quando obter lançamentos por data
            var resultado = await _controller.ObterLancamentosPorData(data);            // Assert - Então deve retornar Ok com os lançamentos
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(lancamentos);
            _mediatorMock.Verify(x => x.Send(It.Is<ObterLancamentosPorDataQuery>(q => q.Data == data), It.IsAny<CancellationToken>()), Times.Once);
        }        [Fact]
        public async Task ObterLancamentosPorData_DadaDataValida_DeveEnviarQueryParaMediator()
        {
            // Arrange - Dada uma data válida
            var data = _faker.Date.Recent(1);
            var lancamentos = CriarListaLancamentos();

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<ObterLancamentosPorDataQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamentos);

            // Act - Quando obter lançamentos por data
            await _controller.ObterLancamentosPorData(data);

            // Assert - Então deve enviar a query para o mediator
            _mediatorMock.Verify(x => x.Send(It.Is<ObterLancamentosPorDataQuery>(q => q.Data == data), It.IsAny<CancellationToken>()), Times.Once);
        }[Fact]
        public async Task ObterLancamentoPorId_DadoIdExistente_DeveRetornarOk()
        {
            // Arrange - Dado um ID existente
            var id = _faker.Random.Int(1, 1000);
            var lancamento = CriarLancamento();

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<ObterLancamentoPorIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamento);

            // Act - Quando obter lançamento por ID
            var resultado = await _controller.ObterLancamentoPorId(id);

            // Assert - Então deve retornar Ok com o lançamento
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(lancamento);
            _mediatorMock.Verify(x => x.Send(It.Is<ObterLancamentoPorIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ObterLancamentoPorId_DadoIdInexistente_DeveRetornarNotFound()
        {
            // Arrange - Dado um ID inexistente
            var id = _faker.Random.Int(1, 1000);

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<ObterLancamentoPorIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Lancamento?)null);

            // Act - Quando obter lançamento por ID inexistente
            var resultado = await _controller.ObterLancamentoPorId(id);

            // Assert - Então deve retornar NotFound
            var notFoundResult = resultado.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be($"Lançamento com ID {id} não foi encontrado.");
            _mediatorMock.Verify(x => x.Send(It.Is<ObterLancamentoPorIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ObterLancamentosPorIntervalo_DadoIntervaloValido_DeveRetornarOk()
        {
            // Arrange - Dado um intervalo válido
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var lancamentos = CriarListaLancamentos();

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<ObterLancamentosPorIntervaloQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lancamentos);

            // Act - Quando obter lançamentos por intervalo
            var resultado = await _controller.ObterLancamentosPorIntervalo(dataInicial, dataFinal);

            // Assert - Então deve retornar Ok com os lançamentos
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(lancamentos);
            _mediatorMock.Verify(x => x.Send(It.Is<ObterLancamentosPorIntervaloQuery>(q => 
                q.DataInicial == dataInicial && q.DataFinal == dataFinal), It.IsAny<CancellationToken>()), Times.Once);
        }

        private RegistrarLancamentoCommand CriarComandoValido()
        {
            return new RegistrarLancamentoCommand
            {
                Valor = _faker.Random.Decimal(1, 1000),
                Tipo = _faker.PickRandom<TipoLancamento>(),
                Data = _faker.Date.Recent(),
                Categoria = _faker.Commerce.Categories(1)[0],
                Descricao = _faker.Commerce.ProductDescription()
            };        }        private Lancamento CriarLancamento()
        {
            var tipo = _faker.PickRandom<TipoLancamento>();
            var valor = tipo == TipoLancamento.Credito 
                ? _faker.Random.Decimal(1, 1000)
                : -_faker.Random.Decimal(1, 1000);
            
            return new Lancamento(
                valor,
                tipo,
                _faker.Date.Recent(),
                _faker.Commerce.Categories(1)[0],
                _faker.Commerce.ProductDescription());
        }

        private IEnumerable<Lancamento> CriarListaLancamentos()
        {
            return new[]
            {
                new Lancamento(                    100,  // Crédito positivo
                    TipoLancamento.Credito,
                    _faker.Date.Recent(),
                    "Categoria",
                    _faker.Commerce.ProductDescription()),
                new Lancamento(
                    -200,  // Débito negativo
                    TipoLancamento.Debito,
                    _faker.Date.Recent(),
                    "Categoria",
                    _faker.Commerce.ProductDescription())
            };
        }
    }
}
