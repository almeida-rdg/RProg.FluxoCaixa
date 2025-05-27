using System.Data;
using Dapper;
using RProg.FluxoCaixa.Worker.Domain.Entities;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Repositório para controle de mensagens processadas usando Dapper.
    /// </summary>
    public class MensagemProcessadaRepository : IMensagemProcessadaRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<MensagemProcessadaRepository> _logger;

        public MensagemProcessadaRepository(IDbConnection connection, ILogger<MensagemProcessadaRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<bool> JaFoiProcessadaAsync(string idMensagem)
        {
            const string sql = "SELECT COUNT(1) FROM MensagemProcessada WHERE IdMensagem = @IdMensagem";

            try
            {
                var count = await _connection.QuerySingleAsync<int>(sql, new { IdMensagem = idMensagem });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se mensagem {IdMensagem} já foi processada", idMensagem);
                throw;
            }
        }

        public async Task RegistrarMensagemProcessadaAsync(MensagemProcessada mensagem)
        {
            const string sql = @"
                INSERT INTO MensagemProcessada (IdMensagem, DataProcessamento, ConteudoMensagem, NomeFila)
                VALUES (@IdMensagem, @DataProcessamento, @ConteudoMensagem, @NomeFila)";

            try
            {
                mensagem.DataProcessamento = DateTime.UtcNow;
                await _connection.ExecuteAsync(sql, mensagem);
                
                _logger.LogDebug("Mensagem {IdMensagem} registrada como processada", mensagem.IdMensagem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar mensagem processada. IdMensagem: {IdMensagem}", mensagem.IdMensagem);
                throw;
            }
        }

        public async Task InicializarEstruturaBancoAsync()
        {
            const string sqlCreateTable = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MensagemProcessada' AND xtype='U')
                BEGIN
                    CREATE TABLE MensagemProcessada (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        IdMensagem NVARCHAR(255) NOT NULL UNIQUE,
                        DataProcessamento DATETIME2 NOT NULL,
                        ConteudoMensagem NVARCHAR(MAX) NOT NULL,
                        NomeFila NVARCHAR(255) NOT NULL
                    );

                    CREATE INDEX IX_MensagemProcessada_IdMensagem ON MensagemProcessada(IdMensagem);
                    CREATE INDEX IX_MensagemProcessada_DataProcessamento ON MensagemProcessada(DataProcessamento);
                END";

            try
            {
                await _connection.ExecuteAsync(sqlCreateTable);
                _logger.LogInformation("Estrutura da tabela MensagemProcessada verificada/criada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar estrutura da tabela MensagemProcessada");
                throw;
            }
        }
    }
}
