using MediatR;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;

namespace RProg.FluxoCaixa.Lancamentos.Application.Commands
{
    public class RegistrarLancamentoCommand : IRequest<int>
    {
        public decimal Valor { get; set; }
        public TipoLancamento Tipo { get; set; } = TipoLancamento.Credito; // "credito" ou "debito"
        public DateTime Data { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
    }
}
