using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Interface para repositório de consolidação diária.
    /// </summary>
    public interface IConsolidadoDiarioRepository
    {
        Task<ConsolidadoDiario?> ObterPorDataECategoriaAsync(DateTime data, string? categoria);
        Task<int> InserirAsync(ConsolidadoDiario consolidado);
        Task AtualizarAsync(ConsolidadoDiario consolidado);
        Task<ConsolidadoDiario?> ObterConsolidacaoGeralPorDataAsync(DateTime data);
        Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesPorCategoriaAsync(DateTime data);
        Task InicializarEstruturaBancoAsync();
    }
}
