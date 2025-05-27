using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Repositório de consolidação diária usando Dapper.
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

        public async Task<ConsolidadoDiario?> ObterPorDataECategoriaAsync(DateTime data, string? categoria)
        {
            const string sql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoFinal, 
                       QuantidadeLancamentos, DataProcessamento, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE CAST(Data AS DATE) = CAST(@Data AS DATE) 
                  AND (@Categoria IS NULL AND Categoria IS NULL OR Categoria = @Categoria)";

            try
            {
                var resultado = await _connection.QueryFirstOrDefaultAsync<ConsolidadoDiario>(sql, new { Data = data, Categoria = categoria });
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter consolidação para data {Data} e categoria {Categoria}", data, categoria);
                throw;
            }
        }

        public async Task<ConsolidadoDiario?> ObterConsolidacaoGeralPorDataAsync(DateTime data)
        {
            return await ObterPorDataECategoriaAsync(data, null);
        }

        public async Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesPorCategoriaAsync(DateTime data)
        {
            const string sql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoFinal, 
                       QuantidadeLancamentos, DataProcessamento, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE CAST(Data AS DATE) = CAST(@Data AS DATE) 
                  AND Categoria IS NOT NULL";

            try
            {
                var resultado = await _connection.QueryAsync<ConsolidadoDiario>(sql, new { Data = data });
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter consolidações por categoria para data {Data}", data);
                throw;
            }
        }

        public async Task<int> InserirAsync(ConsolidadoDiario consolidado)
        {
            const string sql = @"
                INSERT INTO ConsolidadoDiario (Data, Categoria, TotalCreditos, TotalDebitos, SaldoFinal, 
                                               QuantidadeLancamentos, DataProcessamento, DataAtualizacao)
                OUTPUT INSERTED.Id
                VALUES (@Data, @Categoria, @TotalCreditos, @TotalDebitos, @SaldoFinal, 
                        @QuantidadeLancamentos, @DataProcessamento, @DataAtualizacao)";

            try
            {
                consolidado.DataProcessamento = DateTime.UtcNow;
                consolidado.DataAtualizacao = DateTime.UtcNow;
                
                var id = await _connection.QuerySingleAsync<int>(sql, consolidado);
                consolidado.Id = id;
                
                _logger.LogInformation("Consolidação inserida com sucesso. Id: {Id}, Data: {Data}, Categoria: {Categoria}", 
                    id, consolidado.Data, consolidado.Categoria ?? "GERAL");
                
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inserir consolidação para data {Data} e categoria {Categoria}", 
                    consolidado.Data, consolidado.Categoria);
                throw;
            }
        }

        public async Task AtualizarAsync(ConsolidadoDiario consolidado)
        {
            const string sql = @"
                UPDATE ConsolidadoDiario 
                SET TotalCreditos = @TotalCreditos,
                    TotalDebitos = @TotalDebitos,
                    SaldoFinal = @SaldoFinal,
                    QuantidadeLancamentos = @QuantidadeLancamentos,
                    DataAtualizacao = @DataAtualizacao
                WHERE Id = @Id";

            try
            {
                consolidado.DataAtualizacao = DateTime.UtcNow;
                consolidado.RecalcularSaldo();
                
                await _connection.ExecuteAsync(sql, consolidado);
                
                _logger.LogInformation("Consolidação atualizada com sucesso. Id: {Id}, Data: {Data}, Categoria: {Categoria}", 
                    consolidado.Id, consolidado.Data, consolidado.Categoria ?? "GERAL");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar consolidação. Id: {Id}", consolidado.Id);
                throw;
            }
        }

        public async Task InicializarEstruturaBancoAsync()
        {
            const string sqlCreateTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ConsolidadoDiario' AND xtype='U')
                BEGIN
                    CREATE TABLE ConsolidadoDiario (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Data DATE NOT NULL,
                        Categoria NVARCHAR(100) NULL,
                        TotalCreditos DECIMAL(18,2) NOT NULL DEFAULT 0,
                        TotalDebitos DECIMAL(18,2) NOT NULL DEFAULT 0,
                        SaldoFinal DECIMAL(18,2) NOT NULL DEFAULT 0,
                        QuantidadeLancamentos INT NOT NULL DEFAULT 0,
                        DataProcessamento DATETIME2 NOT NULL,
                        DataAtualizacao DATETIME2 NOT NULL,
                        CONSTRAINT UK_ConsolidadoDiario_Data_Categoria UNIQUE(Data, Categoria)
                    );

                    CREATE INDEX IX_ConsolidadoDiario_Data ON ConsolidadoDiario(Data);
                    CREATE INDEX IX_ConsolidadoDiario_Categoria ON ConsolidadoDiario(Categoria);
                END";

            try
            {
                await _connection.ExecuteAsync(sqlCreateTable);
                _logger.LogInformation("Estrutura da tabela ConsolidadoDiario verificada/criada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar estrutura da tabela ConsolidadoDiario");
                throw;
            }
        }
    }
}
