using Microsoft.Data.SqlClient;
using RProg.FluxoCaixa.Worker.Domain.Entities;
using System.Data;
using Dapper;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Implementação do repositório de consolidações usando Dapper.
    /// </summary>
    public class ConsolidadoRepository : IConsolidadoRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ConsolidadoRepository> _logger;

        public ConsolidadoRepository(IConfiguration configuration, ILogger<ConsolidadoRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string não configurada");
            _logger = logger;
        }
        public async Task<ConsolidadoDiario> ObterOuCriarConsolidadoAsync(DateTime data, string? categoria, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            var consolidado = await ObterPorDataECategoriaAsync(data, categoria, cancellationToken);

            if (consolidado == null)
            {
                consolidado = new ConsolidadoDiario
                {
                    Data = data.Date,
                    Categoria = categoria,
                    TotalCreditos = 0,
                    TotalDebitos = 0,
                    QuantidadeLancamentos = 0,
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                };

                // O SaldoLiquido é calculado automaticamente pelo banco
                const string insertSql = @"
                    INSERT INTO ConsolidadoDiario 
                    (Data, Categoria, TotalCreditos, TotalDebitos, QuantidadeLancamentos, DataCriacao, DataAtualizacao)
                    OUTPUT INSERTED.Id
                    VALUES 
                    (@Data, @Categoria, @TotalCreditos, @TotalDebitos, @QuantidadeLancamentos, @DataCriacao, @DataAtualizacao)";

                consolidado.Id = await connection.QuerySingleAsync<long>(insertSql, consolidado);

                _logger.LogDebug("Novo consolidado criado: ID={Id}, Data={Data}, Categoria={Categoria}",
                    consolidado.Id, consolidado.Data, categoria ?? "GERAL");
            }

            return consolidado;
        }
        public async Task SalvarConsolidadoAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            consolidado.AtualizarDataModificacao();

            // O SaldoLiquido é calculado automaticamente pelo banco via coluna computed
            const string updateSql = @"
                UPDATE ConsolidadoDiario 
                SET TotalCreditos = @TotalCreditos,
                    TotalDebitos = @TotalDebitos,
                    QuantidadeLancamentos = @QuantidadeLancamentos,
                    DataAtualizacao = @DataAtualizacao
                WHERE Id = @Id";

            var rowsAffected = await connection.ExecuteAsync(updateSql, consolidado);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Consolidado com ID {consolidado.Id} não encontrado para atualização");
            }

            _logger.LogDebug("Consolidado atualizado: ID={Id}, Créditos={Creditos}, Débitos={Debitos}",
                consolidado.Id, consolidado.TotalCreditos, consolidado.TotalDebitos);
        }
        public async Task<ConsolidadoDiario?> ObterPorDataECategoriaAsync(DateTime data, string? categoria, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            // Usa índice otimizado baseado no tipo de consolidação
            const string selectSql = @"
                SELECT Id, Data, Categoria, TipoConsolidacao, TotalCreditos, TotalDebitos, SaldoLiquido, 
                       QuantidadeLancamentos, DataCriacao, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE Data = @Data AND 
                      ((@Categoria IS NULL AND Categoria IS NULL) OR Categoria = @Categoria)";

            var consolidado = await connection.QuerySingleOrDefaultAsync<ConsolidadoDiario>(
                selectSql,
                new { Data = data.Date, Categoria = categoria });

            return consolidado;
        }
        public async Task<IEnumerable<ConsolidadoDiario>> ListarPorDataAsync(DateTime data, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            // Usa índice otimizado para busca por data
            const string selectSql = @"
                SELECT Id, Data, Categoria, TipoConsolidacao, TotalCreditos, TotalDebitos, SaldoLiquido, 
                       QuantidadeLancamentos, DataCriacao, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE Data = @Data
                ORDER BY CASE WHEN Categoria IS NULL THEN 0 ELSE 1 END, Categoria";

            var consolidados = await connection.QueryAsync<ConsolidadoDiario>(
                selectSql,
                new { Data = data.Date });

            return consolidados;
        }

        /// <summary>
        /// Obtém consolidação geral por período usando stored procedure otimizada.
        /// </summary>
        public async Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacaoGeralPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            var consolidados = await connection.QueryAsync<ConsolidadoDiario>(
                "sp_ObterConsolidacaoGeralPorPeriodo",
                new { DataInicio = dataInicio.Date, DataFim = dataFim.Date },
                commandType: CommandType.StoredProcedure);

            return consolidados;
        }

        /// <summary>
        /// Obtém consolidação por categoria e período usando stored procedure otimizada.
        /// </summary>
        public async Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacaoPorCategoriaPeriodoAsync(DateTime dataInicio, DateTime dataFim, string? categoria, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            var consolidados = await connection.QueryAsync<ConsolidadoDiario>(
                "sp_ObterConsolidacaoPorCategoriaPeriodo",
                new { DataInicio = dataInicio.Date, DataFim = dataFim.Date, Categoria = categoria },
                commandType: CommandType.StoredProcedure);

            return consolidados;
        }

        /// <summary>
        /// Obtém relatório completo de consolidação usando stored procedure otimizada.
        /// </summary>
        public async Task<IEnumerable<ConsolidadoDiario>> ObterRelatorioCompletoConsolidacaoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            var consolidados = await connection.QueryAsync<ConsolidadoDiario>(
                "sp_ObterRelatorioCompletoConsolidacao",
                new { DataInicio = dataInicio.Date, DataFim = dataFim.Date },
                commandType: CommandType.StoredProcedure);

            return consolidados;
        }
        public async Task RecalcularConsolidacoesDataAsync(DateTime data, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            _logger.LogInformation("Iniciando recálculo de consolidações para data: {Data}", data.Date);

            // O SaldoLiquido é recalculado automaticamente pela coluna computed
            // Apenas atualizamos a data de modificação
            const string recalcularSql = @"
                UPDATE ConsolidadoDiario 
                SET DataAtualizacao = GETUTCDATE()
                WHERE Data = @Data";

            var rowsAffected = await connection.ExecuteAsync(recalcularSql, new { Data = data.Date });

            _logger.LogInformation("Recálculo concluído: {RegistrosAtualizados} registros atualizados", rowsAffected);
        }

        /// <summary>
        /// Executa limpeza de lançamentos processados antigos usando stored procedure otimizada.
        /// </summary>
        public async Task<int> LimparLancamentosProcessadosAntigosAsync(int diasParaManter, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);

            var registrosRemovidos = await connection.QuerySingleAsync<int>(
                "sp_LimparLancamentosProcessadosAntigos",
                new { DiasParaManter = diasParaManter },
                commandType: CommandType.StoredProcedure);

            _logger.LogInformation("Limpeza de lançamentos processados concluída: {RegistrosRemovidos} registros removidos", registrosRemovidos);

            return registrosRemovidos;
        }
    }
}
