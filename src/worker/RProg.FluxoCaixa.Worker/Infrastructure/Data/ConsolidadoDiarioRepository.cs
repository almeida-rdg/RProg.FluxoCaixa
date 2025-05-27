using System.Data;
using Dapper;
using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Repositório para acesso aos dados de consolidação diária usando views otimizadas.
    /// Implementa padrões de Clean Architecture focando em regras de negócio testáveis.
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

        public async Task<ConsolidadoDiario?> ObterPorDataECategoriaAsync(DateTime data, string? categoria, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, 
                       QuantidadeLancamentos, DataCriacao, DataAtualizacao
                FROM ConsolidadoDiario 
                WHERE CAST(Data AS DATE) = CAST(@Data AS DATE) 
                  AND (@Categoria IS NULL AND Categoria IS NULL OR Categoria = @Categoria)";

            try
            {
                var resultado = await _connection.QueryFirstOrDefaultAsync<ConsolidadoDiario>(sql,
                    new { Data = data, Categoria = categoria });
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter consolidação para data {Data} e categoria {Categoria}",
                    data, categoria);
                throw;
            }
        }

        public async Task<int> InserirAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                INSERT INTO ConsolidadoDiario (Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido,
                                               QuantidadeLancamentos, DataCriacao, DataAtualizacao)
                OUTPUT INSERTED.Id
                VALUES (@Data, @Categoria, @TotalCreditos, @TotalDebitos, @SaldoLiquido,
                        @QuantidadeLancamentos, @DataCriacao, @DataAtualizacao)";

            try
            {
                consolidado.DataCriacao = DateTime.UtcNow;
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

        public async Task AtualizarAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                UPDATE ConsolidadoDiario 
                SET TotalCreditos = @TotalCreditos,
                    TotalDebitos = @TotalDebitos,
                    SaldoLiquido = @SaldoLiquido,
                    QuantidadeLancamentos = @QuantidadeLancamentos,
                    DataAtualizacao = @DataAtualizacao
                WHERE Id = @Id";

            try
            {
                consolidado.DataAtualizacao = DateTime.UtcNow;
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

        public async Task<ConsolidadoDiario?> ObterConsolidacaoGeralPorDataAsync(DateTime data, CancellationToken cancellationToken = default)
        {
            return await ObterPorDataECategoriaAsync(data, null, cancellationToken);
        }

        public async Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesPorCategoriaAsync(DateTime data, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Id, Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, 
                       QuantidadeLancamentos, DataCriacao, DataAtualizacao
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

        public async Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesGeraisPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Data, TotalCreditos, TotalDebitos, SaldoLiquido, QuantidadeLancamentos, DataAtualizacao
                FROM vw_ConsolidacaoGeral
                WHERE Data BETWEEN @DataInicio AND @DataFim
                ORDER BY Data ASC";

            try
            {
                var resultado = await _connection.QueryAsync<ConsolidadoDiario>(sql,
                    new { DataInicio = dataInicio, DataFim = dataFim });

                _logger.LogInformation("Consulta de consolidação geral executada com sucesso. Período: {DataInicio} a {DataFim}, Registros: {Count}",
                    dataInicio, dataFim, resultado.Count());

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar consolidação geral. Período: {DataInicio} a {DataFim}",
                    dataInicio, dataFim);
                throw;
            }
        }

        public async Task<IEnumerable<ConsolidadoDiario>> ObterConsolidacoesPorCategoriaPeriodoAsync(DateTime dataInicio, DateTime dataFim, string? categoria = null, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, QuantidadeLancamentos, DataAtualizacao
                FROM vw_ConsolidacaoPorCategoria
                WHERE Data BETWEEN @DataInicio AND @DataFim
                  AND (@Categoria IS NULL OR Categoria = @Categoria)
                ORDER BY Data ASC, Categoria ASC";

            try
            {
                var resultado = await _connection.QueryAsync<ConsolidadoDiario>(sql,
                    new { DataInicio = dataInicio, DataFim = dataFim, Categoria = categoria });

                _logger.LogInformation("Consulta de consolidação por categoria executada com sucesso. Período: {DataInicio} a {DataFim}, Categoria: {Categoria}, Registros: {Count}",
                    dataInicio, dataFim, categoria ?? "TODAS", resultado.Count());

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar consolidação por categoria. Período: {DataInicio} a {DataFim}, Categoria: {Categoria}",
                    dataInicio, dataFim, categoria);
                throw;
            }
        }

        public async Task<IEnumerable<ConsolidadoDiario>> ObterRelatorioCompletoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, QuantidadeLancamentos
                FROM vw_ResumoConsolidacao
                WHERE Data BETWEEN @DataInicio AND @DataFim
                ORDER BY Data ASC, CASE WHEN Categoria IS NULL THEN 0 ELSE 1 END, Categoria ASC";

            try
            {
                var resultado = await _connection.QueryAsync<ConsolidadoDiario>(sql,
                    new { DataInicio = dataInicio, DataFim = dataFim });

                _logger.LogInformation("Relatório completo executado com sucesso. Período: {DataInicio} a {DataFim}, Registros: {Count}",
                    dataInicio, dataFim, resultado.Count());

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório completo. Período: {DataInicio} a {DataFim}",
                    dataInicio, dataFim);
                throw;
            }
        }

        public async Task<IEnumerable<ConsolidadoDiario>> ObterTendenciasConsolidacaoAsync(DateTime dataInicio, DateTime dataFim, string? categoria = null, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT Data, Categoria, TotalCreditos, TotalDebitos, SaldoLiquido, QuantidadeLancamentos
                FROM vw_TendenciasConsolidacao
                WHERE Data BETWEEN @DataInicio AND @DataFim
                  AND (@Categoria IS NULL OR Categoria = @Categoria)
                ORDER BY Data ASC, Categoria ASC";

            try
            {
                var resultado = await _connection.QueryAsync<ConsolidadoDiario>(sql,
                    new { DataInicio = dataInicio, DataFim = dataFim, Categoria = categoria });

                _logger.LogInformation("Análise de tendências executada com sucesso. Período: {DataInicio} a {DataFim}, Categoria: {Categoria}, Registros: {Count}",
                    dataInicio, dataFim, categoria ?? "TODAS", resultado.Count());

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter tendências. Período: {DataInicio} a {DataFim}, Categoria: {Categoria}",
                    dataInicio, dataFim, categoria);
                throw;
            }
        }

        public async Task<int> LimparRegistrosAntigosAsync(int diasParaManter = 365, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                EXEC sp_LimparConsolidacoesAntigas @DiasParaManter = @DiasParaManter";

            try
            {
                var registrosRemovidos = await _connection.QuerySingleAsync<int>(sql,
                    new { DiasParaManter = diasParaManter });

                _logger.LogInformation("Limpeza de registros antigos concluída. Registros removidos: {Count}",
                    registrosRemovidos);

                return registrosRemovidos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar limpeza de registros antigos");
                throw;
            }
        }        public async Task InicializarEstruturaBancoAsync()
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
                        SaldoLiquido DECIMAL(18,2) NOT NULL DEFAULT 0,
                        QuantidadeLancamentos INT NOT NULL DEFAULT 0,
                        DataCriacao DATETIME2 NOT NULL,
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

        // Métodos específicos para a API de consultas CQRS
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
