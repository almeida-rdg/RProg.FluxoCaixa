using MediatR;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;

namespace RProg.FluxoCaixa.Lancamentos.Application.Queries
{
    /// <summary>
    /// Query para obter um lançamento por ID.
    /// </summary>
    public record ObterLancamentoPorIdQuery(int Id) : IRequest<Lancamento?>;
}
