using Microsoft.Data.SqlClient;
using RProg.FluxoCaixa.Worker.Domain.Entities;
using Dapper;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Implementação do repositório de lançamentos processados usando Dapper.
    /// </summary>
    public class LancamentoProcessadoRepository : ILancamentoProcessadoRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<LancamentoProcessadoRepository> _logger;

        public LancamentoProcessadoRepository(IConfiguration configuration, ILogger<LancamentoProcessadoRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string não configurada");
            _logger = logger;
        }

        public async Task<bool> JaFoiProcessadoAsync(int lancamentoId, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            
            const string selectSql = @"
                SELECT COUNT(1) 
                FROM LancamentoProcessado 
                WHERE LancamentoId = @LancamentoId";
            
            var count = await connection.QuerySingleAsync<int>(selectSql, new { LancamentoId = lancamentoId });
            
            return count > 0;
        }

        public async Task MarcarComoProcessadoAsync(int lancamentoId, DateTime dataProcessamento, CancellationToken cancellationToken)
        {
            await MarcarComoProcessadoAsync(lancamentoId, dataProcessamento, null, null, null, cancellationToken);
        }

        public async Task MarcarComoProcessadoAsync(int lancamentoId, DateTime dataProcessamento, string? hashConteudo, string? nomeFila, string? observacoes, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            
            var lancamentoProcessado = new LancamentoProcessado
            {
                LancamentoId = lancamentoId,
                DataProcessamento = dataProcessamento,
                HashConteudo = hashConteudo,
                NomeFila = nomeFila,
                Observacoes = observacoes
            };
            
            const string insertSql = @"
                INSERT INTO LancamentoProcessado 
                (LancamentoId, DataProcessamento, HashConteudo, NomeFila, Observacoes)
                VALUES 
                (@LancamentoId, @DataProcessamento, @HashConteudo, @NomeFila, @Observacoes)";
            
            try
            {
                await connection.ExecuteAsync(insertSql, lancamentoProcessado);
                
                _logger.LogDebug("Lançamento {LancamentoId} marcado como processado", lancamentoId);
            }
            catch (SqlException ex) when (ex.Number == 2627) // Primary key violation
            {
                _logger.LogDebug("Lançamento {LancamentoId} já estava marcado como processado", lancamentoId);
                // Ignorar se já existe (race condition entre múltiplas instâncias)
            }
        }

        public async Task<LancamentoProcessado?> ObterPorIdAsync(int lancamentoId, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            
            const string selectSql = @"
                SELECT Id, LancamentoId, DataProcessamento, HashConteudo, NomeFila, Observacoes
                FROM LancamentoProcessado 
                WHERE LancamentoId = @LancamentoId";
            
            var lancamento = await connection.QuerySingleOrDefaultAsync<LancamentoProcessado>(
                selectSql, 
                new { LancamentoId = lancamentoId });
            
            return lancamento;
        }

        public async Task<int> LimparRegistrosAntigosAsync(DateTime dataLimite, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            
            const string deleteSql = @"
                DELETE FROM LancamentoProcessado 
                WHERE DataProcessamento < @DataLimite";
            
            var deletedCount = await connection.ExecuteAsync(deleteSql, new { DataLimite = dataLimite });
            
            _logger.LogInformation("Limpeza de registros antigos: {RegistrosRemovidos} registros removidos", deletedCount);
            
            return deletedCount;
        }
    }
}
