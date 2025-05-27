using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RProg.FluxoCaixa.Worker.Domain.DTOs;
using System.Text;
using System.Text.Json;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Services
{
    /// <summary>
    /// Implementação do serviço de consumo de mensagens RabbitMQ.
    /// Responsável pela conexão, escuta e processamento de mensagens das filas.
    /// </summary>
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILogger<RabbitMqService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _consumerTag;
        private bool _disposed = false;

        public RabbitMqService(ILogger<RabbitMqService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionFactory = CriarConnectionFactory();
        }

        /// <summary>
        /// Indica se a conexão com o RabbitMQ está ativa.
        /// </summary>
        public bool EstaConectado => _connection?.IsOpen == true && _channel?.IsOpen == true;

        /// <summary>
        /// Inicia a escuta das filas RabbitMQ com o prefixo especificado.
        /// </summary>
        /// <param name="onMessageReceived">Callback para processar mensagens recebidas.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task IniciarEscutaAsync(Func<LancamentoDto, string, Task<bool>> onMessageReceived, CancellationToken cancellationToken)
        {
            var nomeFila = ObterNomeFilaConfigurada();
            _logger.LogInformation("Iniciando escuta da fila RabbitMQ: {Fila}", nomeFila);

            await ConectarAsync();
            ValidarConexao();
            await ConfigurarQualidadeServicoAsync();

            var declarada = await DeclararFilaAsync(nomeFila);
            if (!declarada)
            {
                _logger.LogError("Não foi possível declarar a fila {Fila}", nomeFila);
                throw new InvalidOperationException($"Não foi possível declarar a fila {nomeFila}");
            }

            await ConfigurarConsumidorAsync(nomeFila, onMessageReceived, cancellationToken);

            _logger.LogInformation("Escuta iniciada para a fila {Fila}", nomeFila);
        }

        /// <summary>
        /// Para a escuta de todas as filas ativas.
        /// </summary>
        public async Task PararEscutaAsync()
        {
            _logger.LogInformation("Parando escuta da fila RabbitMQ");
            if (!string.IsNullOrEmpty(_consumerTag) && _channel != null)
            {
                await _channel.BasicCancelAsync(_consumerTag);
                _consumerTag = null;
            }
        }

        /// <summary>
        /// Reconecta ao RabbitMQ após uma falha de conexão.
        /// </summary>
        public async Task ReconectarAsync()
        {
            _logger.LogInformation("Tentando reconectar ao RabbitMQ");

            await FecharConexaoAsync();
            await ConectarAsync();

            _logger.LogInformation("Reconexão ao RabbitMQ realizada com sucesso");
        }

        /// <summary>
        /// Estabelece a conexão com o servidor RabbitMQ reutilizando a ConnectionFactory e a Connection.
        /// </summary>
        private async Task ConectarAsync()
        {
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _connection = await _connectionFactory.CreateConnectionAsync("WorkerRProg.FluxoCaixa");
                }

                if (_channel == null || !_channel.IsOpen)
                {
                    _channel = await _connection.CreateChannelAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
                throw;
            }
        }

        /// <summary>
        /// Cria e configura a factory de conexão RabbitMQ.
        /// </summary>
        /// <returns>Factory configurada.</returns>
        private ConnectionFactory CriarConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = _configuration.GetValue<string>("RabbitMQ:HostName") ?? "localhost",
                Port = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
                UserName = _configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest",
                Password = _configuration.GetValue<string>("RabbitMQ:Password") ?? "guest",
                VirtualHost = _configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
        }

        /// <summary>
        /// Valida se a conexão foi estabelecida corretamente.
        /// </summary>
        private void ValidarConexao()
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("Não foi possível estabelecer conexão com RabbitMQ");
            }
        }

        /// <summary>
        /// Configura a qualidade de serviço do canal RabbitMQ.
        /// </summary>
        private async Task ConfigurarQualidadeServicoAsync()
        {
            if (_channel != null)
            {
                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
            }
        }

        /// <summary>
        /// Obtém a lista de filas configuradas no appsettings.
        /// </summary>
        /// <param name="prefixo">Prefixo das filas.</param>
        /// <returns>Array com nomes das filas.</returns>
        private string ObterNomeFilaConfigurada()
        {
            var fila = _configuration.GetValue<string>("RabbitMQ:Fila");
            if (string.IsNullOrWhiteSpace(fila))
            {
                throw new InvalidOperationException("O nome da fila RabbitMQ não está configurado (RabbitMQ:Fila)");
            }
            return fila;
        }

        /// <summary>
        /// Declara uma fila no RabbitMQ se ela não existir.
        /// </summary>
        /// <param name="nomeFila">Nome da fila a ser declarada.</param>
        /// <returns>True se a fila foi declarada com sucesso.</returns>
        private async Task<bool> DeclararFilaAsync(string nomeFila)
        {
            try
            {
                if (_channel != null)
                {
                    await _channel.QueueDeclareAsync(
                        queue: nomeFila,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                }

                _logger.LogDebug("Fila {Fila} descoberta/criada", nomeFila);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao declarar fila {Fila}", nomeFila);
                return false;
            }
        }

        /// <summary>
        /// Configura consumidores para todas as filas especificadas.
        /// </summary>
        /// <param name="filas">Lista de filas.</param>
        /// <param name="onMessageReceived">Callback para processar mensagens.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        private async Task ConfigurarConsumidorAsync(string nomeFila, Func<LancamentoDto, string, Task<bool>> onMessageReceived, CancellationToken cancellationToken)
        {
            if (_channel == null)
                return;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) => await ProcessarMensagemAsync(ea, nomeFila, onMessageReceived);

            _consumerTag = await _channel.BasicConsumeAsync(
                queue: nomeFila,
                autoAck: false,
                consumer: consumer);

            if (!string.IsNullOrEmpty(_consumerTag))
            {
                _logger.LogInformation("Consumidor iniciado para fila {Fila} com tag {ConsumerTag}", nomeFila, _consumerTag);
            }
        }

        /// <summary>
        /// Processa uma mensagem recebida de uma fila.
        /// </summary>
        /// <param name="ea">Argumentos da mensagem.</param>
        /// <param name="nomeFila">Nome da fila de origem.</param>
        /// <param name="onMessageReceived">Callback para processar a mensagem.</param>
        private async Task ProcessarMensagemAsync(BasicDeliverEventArgs ea, string nomeFila, Func<LancamentoDto, string, Task<bool>> onMessageReceived)
        {
            var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogDebug("Mensagem recebida da fila {Fila}: {MessageId}", nomeFila, messageId);

            try
            {
                var lancamento = DeserializarMensagem(message);
                if (lancamento != null)
                {
                    await ProcessarLancamentoAsync(lancamento, nomeFila, ea, messageId, onMessageReceived);
                }
                else
                {
                    await RejeitarMensagemAsync(ea, messageId, "Não foi possível deserializar a mensagem");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar mensagem {MessageId} da fila {Fila}", messageId, nomeFila);
                await RejeitarMensagemAsync(ea, messageId, "Erro de deserialização");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem {MessageId} da fila {Fila}", messageId, nomeFila);
                await RejeitarComRequeueAsync(ea, messageId);
            }
        }

        /// <summary>
        /// Deserializa uma mensagem JSON em um objeto LancamentoDto.
        /// </summary>
        /// <param name="message">Mensagem JSON.</param>
        /// <returns>Objeto LancamentoDto ou null se falhar.</returns>
        private LancamentoDto? DeserializarMensagem(string message)
        {
            return JsonSerializer.Deserialize<LancamentoDto>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// Processa um lançamento e gerencia as confirmações.
        /// </summary>
        /// <param name="lancamento">Lançamento a ser processado.</param>
        /// <param name="nomeFila">Nome da fila.</param>
        /// <param name="ea">Argumentos da mensagem.</param>
        /// <param name="messageId">ID da mensagem.</param>
        /// <param name="onMessageReceived">Callback de processamento.</param>
        private async Task ProcessarLancamentoAsync(LancamentoDto lancamento, string nomeFila, BasicDeliverEventArgs ea, string messageId, Func<LancamentoDto, string, Task<bool>> onMessageReceived)
        {
            var processado = await onMessageReceived(lancamento, nomeFila);

            if (processado)
            {
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogDebug("Mensagem {MessageId} processada e confirmada", messageId);
            }
            else
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
                _logger.LogWarning("Mensagem {MessageId} não foi processada, retornando à fila", messageId);
            }
        }

        /// <summary>
        /// Rejeita uma mensagem sem retorná-la à fila.
        /// </summary>
        /// <param name="ea">Argumentos da mensagem.</param>
        /// <param name="messageId">ID da mensagem.</param>
        /// <param name="motivo">Motivo da rejeição.</param>
        private async Task RejeitarMensagemAsync(BasicDeliverEventArgs ea, string messageId, string motivo)
        {
            _logger.LogError("{Motivo}: {MessageId}", motivo, messageId);
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
        }

        /// <summary>
        /// Rejeita uma mensagem e a retorna à fila para reprocessamento.
        /// </summary>
        /// <param name="ea">Argumentos da mensagem.</param>
        /// <param name="messageId">ID da mensagem.</param>
        private async Task RejeitarComRequeueAsync(BasicDeliverEventArgs ea, string messageId)
        {
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
        }

        /// <summary>
        /// Fecha as conexões com o RabbitMQ.
        /// </summary>
        private async Task FecharConexaoAsync()
        {
            try
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync();
                    await _channel.DisposeAsync();
                    _channel = null;
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                _consumerTag = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao fechar conexão RabbitMQ");
            }
        }

        /// <summary>
        /// Libera recursos utilizados pelo serviço.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _channel?.Dispose();
                    _connection?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao fazer dispose da conexão RabbitMQ");
                }
                finally
                {
                    _channel = null;
                    _connection = null;
                    _consumerTag = null;
                    _disposed = true;
                }
            }
        }
    }
}
