using RProg.FluxoCaixa.Consolidado.Domain.Entities;

namespace RProg.FluxoCaixa.Consolidado.Infrastructure.Data
{
    /// <summary>
    /// Interface para repositório de consolidação diária otimizada com views.
    /// Segue padrões de Clean Architecture e facilita testes unitários.
    /// </summary>
    public interface IConsolidadoDiarioRepository
    {
        // Métodos específicos para a API de consultas CQRS
        /// <summary>
        /// Obtém consolidados por período para API CQRS.
        /// </summary>
        Task<IEnumerable<ConsolidadoDiario>> ObterPorPeriodoAsync(DateTime dataInicial, DateTime dataFinal, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém a última data de atualização do período para API CQRS.
        /// </summary>
        Task<DateTime?> ObterUltimaDataAtualizacaoAsync(DateTime dataInicial, DateTime dataFinal, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém consolidados por período e categoria para API CQRS.
        /// </summary>
        Task<IEnumerable<ConsolidadoDiario>> ObterPorPeriodoECategoriaAsync(DateTime dataInicial, DateTime dataFinal, string categoria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém a última data de atualização por período e categoria para API CQRS.
        /// </summary>
        Task<DateTime?> ObterUltimaDataAtualizacaoPorCategoriaAsync(DateTime dataInicial, DateTime dataFinal, string categoria, CancellationToken cancellationToken = default);
    }
}
