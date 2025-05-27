using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Interface para repositório de consolidação diária otimizada com views.
    /// Segue padrões de Clean Architecture e facilita testes unitários.
    /// </summary>
    public interface IConsolidadoDiarioRepository
    {
        // Métodos básicos de CRUD
        Task<ConsolidadoDiario?> ObterPorDataECategoriaAsync(DateTime data, string? categoria, CancellationToken cancellationToken = default);
        Task<int> InserirAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default);
        Task AtualizarAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default);
        
        // Métodos legados mantidos para compatibilidade
        Task<ConsolidadoDiario?> ObterConsolidacaoGeralPorDataAsync(DateTime data, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesPorCategoriaAsync(DateTime data, CancellationToken cancellationToken = default);
        
        // Novos métodos otimizados usando views (substituem stored procedures)
        /// <summary>
        /// Obtém consolidações gerais por período usando view otimizada.
        /// </summary>
        Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesGeraisPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém consolidações por categoria e período usando view otimizada.
        /// </summary>
        Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesPorCategoriaPeriodoAsync(DateTime dataInicio, DateTime dataFim, string? categoria = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém relatório completo usando view materializada para máxima performance.
        /// </summary>
        Task<IEnumerable<ConsolidadoDiario>> ObterRelatorioCompletoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém análise de tendências com campos calculados.
        /// </summary>
        Task<IEnumerable<ConsolidadoDiario>> ObterTendenciasConsolidacaoAsync(DateTime dataInicio, DateTime dataFim, string? categoria = null, CancellationToken cancellationToken = default);        /// <summary>
        /// Operação de manutenção para limpeza de dados antigos.
        /// </summary>
        Task<int> LimparRegistrosAntigosAsync(int diasParaManter = 30, CancellationToken cancellationToken = default);

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
        
        // Método de infraestrutura
        Task InicializarEstruturaBancoAsync();
    }
}
