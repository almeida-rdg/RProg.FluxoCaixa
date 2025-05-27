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
                
                consolidado.RecalcularSaldo();
                
                const string insertSql = @"
                    INSERT INTO ConsolidadoDiario 
                    (Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, QuantidadeLancamentos, DataCriacao, DataAtualizacao)
                    OUTPUT INSERTED.Id
                    VALUES 
                    (@Data, @Categoria, @TotalCreditos, @TotalDebitos, @SaldoLiquido, @QuantidadeLancamentos, @DataCriacao, @DataAtualizacao)";
                
                consolidado.Id = await connection.QuerySingleAsync<int>(insertSql, consolidado);
                
                _logger.LogDebug("Novo consolidado criado: ID={Id}, Data={Data}, Categoria={Categoria}", 
                    consolidado.Id, consolidado.Data, categoria ?? "GERAL");
            }
            
            return consolidado;
        }

        public async Task SalvarConsolidadoAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            
            consolidado.DataAtualizacao = DateTime.UtcNow;
            consolidado.RecalcularSaldo();
            
            const string updateSql = @"
                UPDATE ConsolidadoDiario 
                SET TotalCreditos = @TotalCreditos,
                    TotalDebitos = @TotalDebitos,
                    SaldoLiquido = @SaldoLiquido,
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
            
            const string selectSql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, 
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
            
            const string selectSql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, 
                       QuantidadeLancamentos, DataCriacao, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE Data = @Data
                ORDER BY CASE WHEN Categoria IS NULL THEN 0 ELSE 1 END, Categoria";
            
            var consolidados = await connection.QueryAsync<ConsolidadoDiario>(
                selectSql, 
                new { Data = data.Date });
            
            return consolidados;
        }

        public async Task RecalcularConsolidacoesDataAsync(DateTime data, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_connectionString);
            
            _logger.LogInformation("Iniciando recálculo de consolidações para data: {Data}", data.Date);
            
            // Este método poderia implementar uma lógica de recálculo completo
            // baseado nos lançamentos originais, se necessário
            const string recalcularSql = @"
                UPDATE ConsolidadoDiario 
                SET SaldoLiquido = TotalCreditos - TotalDebitos,
                    DataAtualizacao = GETUTCDATE()
                WHERE Data = @Data";
            
            var rowsAffected = await connection.ExecuteAsync(recalcularSql, new { Data = data.Date });
            
            _logger.LogInformation("Recálculo concluído: {RegistrosAtualizados} registros atualizados", rowsAffected);
        }
    }
}
