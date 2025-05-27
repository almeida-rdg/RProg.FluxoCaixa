using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace RProg.FluxoCaixa.Lancamentos.Infrastructure
{
    /// <summary>
    /// Implementação de publisher para RabbitMQ que publica mensagens de lançamentos.
    /// </summary>
    public class RabbitMqPublisher : IMensageriaPublisher, IDisposable
    {
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly string _queueName;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IConnection _conexao;
        private IChannel _canal;

        public RabbitMqPublisher(IConfiguration configuracao, ILogger<RabbitMqPublisher> logger, IConnectionFactory connectionFactory)
        {
            _logger = logger;
            _queueName = configuracao["RabbitMQ:QueueName"] ?? "lancamentos";
            _connectionFactory = connectionFactory;

            try
            {
                _logger.LogInformation("Conectando ao RabbitMQ. HostName: {HostName}", connectionFactory.Uri);
                _conexao = _connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
                _logger.LogInformation("Conexão com RabbitMQ estabelecida com sucesso");

                _canal = DeclararFilaAsync().GetAwaiter().GetResult();
                _logger.LogInformation("Canal RabbitMQ criado e fila '{QueueName}' declarada", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com RabbitMQ. HostName: {HostName}", connectionFactory.Uri);
                throw;
            }
        }

        private async Task<IChannel> DeclararFilaAsync()
        {
            try
            {
                if (_canal != null && _canal.IsOpen)
                {
                    _logger.LogDebug("Canal RabbitMQ já está aberto e operacional");
                    return _canal;
                }

                _logger.LogInformation("Recriando canal RabbitMQ");
                _canal?.Dispose();

                var canal = await _conexao.CreateChannelAsync();
                await canal.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                _logger.LogInformation("Canal RabbitMQ recriado com sucesso");
                return canal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao declarar fila '{QueueName}' no RabbitMQ", _queueName);
                throw;
            }
        }

        public async Task PublicarMensagemAsync<TMensagem>(TMensagem mensagem)
        {
            try
            {
                _logger.LogDebug("Iniciando publicação de mensagem no RabbitMQ");
                _canal = await DeclararFilaAsync();

                var mensagemSerializada = JsonSerializer.Serialize(mensagem);
                var corpo = Encoding.UTF8.GetBytes(mensagemSerializada);

                await _canal.BasicPublishAsync(exchange: string.Empty, routingKey: _queueName, body: corpo);

                _logger.LogInformation("Mensagem publicada com sucesso no RabbitMQ. Queue: {QueueName}, TamanhoMensagem: {TamanhoBytes} bytes", 
                    _queueName, corpo.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem no RabbitMQ. Queue: {QueueName}", _queueName);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Fechando conexões RabbitMQ");
                _canal?.Dispose();
                _conexao?.Dispose();
                _logger.LogInformation("Conexões RabbitMQ fechadas com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao fechar conexões RabbitMQ");
            }
        }
    }
}
