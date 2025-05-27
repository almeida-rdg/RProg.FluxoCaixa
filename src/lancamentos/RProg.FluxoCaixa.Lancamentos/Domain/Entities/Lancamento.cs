using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace RProg.FluxoCaixa.Lancamentos.Domain.Entities
{
    /// <summary>
    /// Entidade de domínio para Lançamento com validações de negócio.
    /// </summary>
    public class Lancamento
    {
        private static readonly Regex _regexLetras = new(@"[a-zA-Z]", RegexOptions.Compiled);

        public int? Id { get; private set; }
        public decimal Valor { get; private set; }
        public TipoLancamento Tipo { get; private set; }
        public DateTime Data { get; private set; }
        public string Categoria { get; private set; }
        public string Descricao { get; private set; }

        public Lancamento(decimal valor, TipoLancamento tipo, DateTime data, string categoria, string descricao, int? id = null)
        {
            Validar(valor, tipo, categoria, descricao);

            Id = id;
            Valor = valor;
            Tipo = tipo;
            Data = data;
            Categoria = categoria;
            Descricao = descricao;
        }

        private static void Validar(decimal valor, TipoLancamento tipo, string categoria, string descricao)
        {
            ValidarValor(valor, tipo);
            ValidarCategoria(categoria);
            ValidarDescricao(descricao);
        }

        private static void ValidarValor(decimal valor, TipoLancamento tipo)
        {
            if (valor == 0)
            {
                throw new ExcecaoRegraDeNegocio("O valor do lançamento não pode ser zero.");
            }

            if (tipo == TipoLancamento.Credito && valor < 0)
            {
                throw new ExcecaoRegraDeNegocio("Lançamentos de crédito não podem ter valores negativos.");
            }

            if (tipo == TipoLancamento.Debito && valor > 0)
            {
                throw new ExcecaoRegraDeNegocio("Lançamentos de débito não podem ter valores positivos.");
            }
        }

        private static void ValidarCategoria(string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria))
            {
                throw new ExcecaoRegraDeNegocio("A categoria é obrigatória.");
            }

            var letrasCategoria = _regexLetras.Matches(categoria);
            if (letrasCategoria.Count < 3)
            {
                throw new ExcecaoRegraDeNegocio("A categoria deve conter pelo menos 3 letras.");
            }
        }

        private static void ValidarDescricao(string descricao)
        {
            if (string.IsNullOrWhiteSpace(descricao))
            {
                throw new ExcecaoRegraDeNegocio("A descrição é obrigatória.");
            }

            var letrasDescricao = _regexLetras.Matches(descricao);
            if (letrasDescricao.Count < 3)
            {
                throw new ExcecaoRegraDeNegocio("A descrição deve conter pelo menos 3 letras.");
            }
        }
    }
}
