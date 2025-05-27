using System.Data;
using Dapper;
using RProg.FluxoCaixa.Consolidado.Domain.Entities;

namespace RProg.FluxoCaixa.Consolidado.Infrastructure.Data
{
    /// <summary>
    /// Repositório para acesso aos dados de consolidação diária usando consultas otimizadas.
    /// Implementa padrões de Clean Architecture focando em queries de leitura para CQRS.
    /// </summary>
    public class ConsolidadoDiarioRepository : IConsolidadoDiarioRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<ConsolidadoDiarioRepository> _logger;

        public ConsolidadoDiarioRepository(IDbConnection connection, ILogger<ConsolidadoDiarioRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<IEnumerable<ConsolidadoDiario>> ObterPorPeriodoAsync(DateTime dataInicial, DateTime dataFinal, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, 
                       QuantidadeLancamentos, DataCriacao, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE Data >= @DataInicial AND Data <= @DataFinal
                ORDER BY Data ASC, CASE WHEN Categoria IS NULL THEN 0 ELSE 1 END, Categoria ASC";

            try
            {
                var resultado = await _connection.QueryAsync<ConsolidadoDiario>(sql,
                    new { DataInicial = dataInicial, DataFinal = dataFinal });

                _logger.LogInformation("Consulta por período executada com sucesso. Período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Registros: {Count}",
                    dataInicial, dataFinal, resultado.Count());

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar consolidados por período. Período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}",
                    dataInicial, dataFinal);
                throw;
            }
        }

        public async Task<DateTime?> ObterUltimaDataAtualizacaoAsync(DateTime dataInicial, DateTime dataFinal, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT MAX(DataAtualizacao) 
                FROM ConsolidadoDiario 
                WHERE Data >= @DataInicial AND Data <= @DataFinal";

            try
            {
                var resultado = await _connection.QuerySingleOrDefaultAsync<DateTime?>(sql,
                    new { DataInicial = dataInicial, DataFinal = dataFinal });

                _logger.LogDebug("Última data de atualização obtida para período {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}: {UltimaAtualizacao}",
                    dataInicial, dataFinal, resultado?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter última data de atualização. Período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}",
                    dataInicial, dataFinal);
                throw;
            }
        }

        public async Task<IEnumerable<ConsolidadoDiario>> ObterPorPeriodoECategoriaAsync(DateTime dataInicial, DateTime dataFinal, string categoria, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, 
                       QuantidadeLancamentos, DataCriacao, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE Data >= @DataInicial AND Data <= @DataFinal
                  AND Categoria = @Categoria
                ORDER BY Data ASC";

            try
            {
                var resultado = await _connection.QueryAsync<ConsolidadoDiario>(sql,
                    new { DataInicial = dataInicial, DataFinal = dataFinal, Categoria = categoria });

                _logger.LogInformation("Consulta por período e categoria executada com sucesso. Período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}, Registros: {Count}",
                    dataInicial, dataFinal, categoria, resultado.Count());

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar consolidados por período e categoria. Período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}",
                    dataInicial, dataFinal, categoria);
                throw;
            }
        }

        public async Task<DateTime?> ObterUltimaDataAtualizacaoPorCategoriaAsync(DateTime dataInicial, DateTime dataFinal, string categoria, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT MAX(DataAtualizacao) 
                FROM ConsolidadoDiario 
                WHERE Data >= @DataInicial AND Data <= @DataFinal
                  AND Categoria = @Categoria";

            try
            {
                var resultado = await _connection.QuerySingleOrDefaultAsync<DateTime?>(sql,
                    new { DataInicial = dataInicial, DataFinal = dataFinal, Categoria = categoria });

                _logger.LogDebug("Última data de atualização por categoria obtida para período {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}: {UltimaAtualizacao}",
                    dataInicial, dataFinal, categoria, resultado?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter última data de atualização por categoria. Período: {DataInicial:yyyy-MM-dd} a {DataFinal:yyyy-MM-dd}, Categoria: {Categoria}",
                    dataInicial, dataFinal, categoria);
                throw;
            }
        }
    }
}
