using RProg.FluxoCaixa.Lancamentos.Domain.Entities;

namespace RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper
{
    /// <summary>
    /// Interface para o repositório de lançamentos.
    /// </summary>
    public interface ILancamentoRepository
    {
        Task<int> CriarLancamentoAsync(Lancamento lancamento);
        Task<IEnumerable<Lancamento>> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken);
        Task<Lancamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<Lancamento>> ObterPorIntervaloAsync(DateTime dataInicial, DateTime dataFinal, CancellationToken cancellationToken);
    }
}
