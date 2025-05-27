using FluentAssertions;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;
using Xunit;

namespace RProg.FluxoCaixa.Lancamentos.Test.Domain.Entities
{
    public class LancamentoTest
    {
        [Fact]
        public void ConstruirLancamento_DadoValorValidoCredito_DevePermitirCriacao()
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Receita";            var descricao = "Pagamento cliente";

            // Quando
            var lancamento = new Lancamento(valor, tipo, data, categoria, descricao);

            // Então
            lancamento.Valor.Should().Be(valor);
            lancamento.Tipo.Should().Be(tipo);
            lancamento.Data.Should().Be(data);
            lancamento.Categoria.Should().Be(categoria);
            lancamento.Descricao.Should().Be(descricao);
            lancamento.Id.Should().BeNull(); // ID é null até ser persistido
        }

        [Fact]
        public void ConstruirLancamento_DadoValorValidoDebito_DevePermitirCriacao()
        {
            // Dado
            var valor = -50m;
            var tipo = TipoLancamento.Debito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Despesa";            var descricao = "Compra material";

            // Quando
            var lancamento = new Lancamento(valor, tipo, data, categoria, descricao);

            // Então
            lancamento.Valor.Should().Be(valor);
            lancamento.Tipo.Should().Be(tipo);
            lancamento.Data.Should().Be(data);
            lancamento.Categoria.Should().Be(categoria);
            lancamento.Descricao.Should().Be(descricao);
            lancamento.Id.Should().BeNull(); // ID é null até ser persistido
        }        [Fact]
        public void ConstruirLancamento_DadoValorZero_DeveLancarExcecao()
        {
            // Dado
            var valor = 0m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Receita";
            var descricao = "Teste";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoria, descricao);

            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("O valor do lançamento não pode ser zero.");
        }        [Fact]
        public void ConstruirLancamento_DadoCreditoComValorNegativo_DeveLancarExcecao()
        {
            // Dado
            var valor = -100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Receita";
            var descricao = "Teste";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoria, descricao);            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("Lançamentos de crédito não podem ter valores negativos.");
        }        [Fact]
        public void ConstruirLancamento_DadoDebitoComValorPositivo_DeveLancarExcecao()
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Debito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Despesa";
            var descricao = "Teste";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoria, descricao);            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("Lançamentos de débito não podem ter valores positivos.");}

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public void ConstruirLancamento_DadoCategoriaVaziaOuEspacos_DeveLancarExcecao(string categoriaInvalida)
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var descricao = "Teste";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoriaInvalida, descricao);

            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("A categoria é obrigatória.");
        }

        [Fact]
        public void ConstruirLancamento_DadoCategoriaNula_DeveLancarExcecao()
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var descricao = "Teste";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, null!, descricao);

            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("A categoria é obrigatória.");
        }        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public void ConstruirLancamento_DadoDescricaoVaziaOuEspacos_DeveLancarExcecao(string descricaoInvalida)
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Receita";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoria, descricaoInvalida);

            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("A descrição é obrigatória.");
        }

        [Fact]
        public void ConstruirLancamento_DadoDescricaoNula_DeveLancarExcecao()
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Receita";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoria, null!);

            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("A descrição é obrigatória.");
        }        [Theory]
        [InlineData("AB")]
        [InlineData("A1")]
        [InlineData("123")]
        [InlineData("@@")]
        public void ConstruirLancamento_DadoCategoriaComMenosDeTresLetras_DeveLancarExcecao(string categoriaComPoucasLetras)
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var descricao = "Teste descricao";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoriaComPoucasLetras, descricao);            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("A categoria deve conter pelo menos 3 letras.");
        }        [Theory]
        [InlineData("AB")]
        [InlineData("A1")]
        [InlineData("123")]
        [InlineData("@@")]
        public void ConstruirLancamento_DadoDescricaoComMenosDeTresLetras_DeveLancarExcecao(string descricaoComPoucasLetras)
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Receita";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoria, descricaoComPoucasLetras);            // Então
            acao.Should().Throw<ExcecaoRegraDeNegocio>()
                .WithMessage("A descrição deve conter pelo menos 3 letras.");
        }

        [Theory]
        [InlineData("ABC")]
        [InlineData("AB1C")]
        [InlineData("A1B2C")]
        [InlineData("Categoria Válida")]
        public void ConstruirLancamento_DadoCategoriaComPeloMenosTresLetras_DevePermitirCriacao(string categoriaValida)
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var descricao = "Descricao valida";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoriaValida, descricao);

            // Então
            acao.Should().NotThrow();
        }

        [Theory]
        [InlineData("ABC")]
        [InlineData("AB1C")]
        [InlineData("A1B2C")]
        [InlineData("Descrição Válida")]
        public void ConstruirLancamento_DadoDescricaoComPeloMenosTresLetras_DevePermitirCriacao(string descricaoValida)
        {
            // Dado
            var valor = 100m;
            var tipo = TipoLancamento.Credito;
            var data = DateTime.Now.AddDays(-1);
            var categoria = "Receita";

            // Quando
            var acao = () => new Lancamento(valor, tipo, data, categoria, descricaoValida);

            // Então
            acao.Should().NotThrow();
        }
    }
}
