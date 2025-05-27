using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RProg.FluxoCaixa.Worker.Domain.DTOs;
using System.Text;
using System.Text.Json;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Services
{
    /// <summary>
    /// Implementação do serviço de consumo de mensagens RabbitMQ.
    /// </summary>
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly ILogger<RabbitMqService> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly List<string> _consumingQueues = new();
        private bool _disposed = false;

        public RabbitMqService(ILogger<RabbitMqService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool EstaConectado => _connection?.IsOpen == true && _channel?.IsOpen == true;

        public async Task IniciarEscutaAsync(string prefixoFila, Func<LancamentoDto, string, Task<bool>> onMessageReceived, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando escuta das filas RabbitMQ com prefixo: {Prefixo}", prefixoFila);

            await ConectarAsync();

            if (_channel == null)
            {
                throw new InvalidOperationException("Não foi possível estabelecer conexão com RabbitMQ");
            }            // Configurar QoS para processamento de uma mensagem por vez
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            // Descobrir filas com o prefixo especificado
            var filas = await DescobririFilasComPrefixoAsync(prefixoFila);
            
            foreach (var nomeFila in filas)
            {
                await ConfigurarConsumidorFilaAsync(nomeFila, onMessageReceived, cancellationToken);
            }

            _logger.LogInformation("Escuta iniciada para {QuantidadeFilas} filas", filas.Count);
        }        public async Task PararEscutaAsync()
        {
            _logger.LogInformation("Parando escuta das filas RabbitMQ");

            foreach (var queueName in _consumingQueues.ToList())
            {
                try
                {
                    if (_channel != null)
                    {
                        await _channel.BasicCancelAsync(queueName);
                    }
                    _consumingQueues.Remove(queueName);
                    _logger.LogDebug("Consumidor da fila {Fila} cancelado", queueName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao cancelar consumidor da fila {Fila}", queueName);
                }
            }
        }public async Task ReconectarAsync()
        {
            _logger.LogInformation("Tentando reconectar ao RabbitMQ");
            
            await FecharConexaoAsync();
            await ConectarAsync();
            
            _logger.LogInformation("Reconexão ao RabbitMQ realizada com sucesso");
        }private async Task ConectarAsync()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration.GetValue<string>("RabbitMQ:HostName") ?? "localhost",
                    Port = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
                    UserName = _configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest",
                    Password = _configuration.GetValue<string>("RabbitMQ:Password") ?? "guest",
                    VirtualHost = _configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync("WorkerRProg.FluxoCaixa");
                _channel = await _connection.CreateChannelAsync();

                _logger.LogInformation("Conectado ao RabbitMQ: {HostName}:{Port}", factory.HostName, factory.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
                throw;
            }
        }

        private async Task<List<string>> DescobririFilasComPrefixoAsync(string prefixo)
        {
            var filas = new List<string>();
            
            // Para este exemplo, vamos usar uma lista configurável de filas
            // Em um ambiente real, você poderia usar a Management API do RabbitMQ para descobrir filas
            var filasConfiguradas = _configuration.GetSection("RabbitMQ:Filas").Get<string[]>() ?? 
                                   new[] { $"{prefixo}.geral", $"{prefixo}.prioritaria" };

            foreach (var nomeFila in filasConfiguradas)
            {                try
                {
                    // Declarar a fila se não existir
                    if (_channel != null)
                    {
                        await _channel.QueueDeclareAsync(
                            queue: nomeFila,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);
                    }
                    
                    filas.Add(nomeFila);
                    _logger.LogDebug("Fila {Fila} descoberta/criada", nomeFila);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao declarar fila {Fila}", nomeFila);
                }
            }

            await Task.CompletedTask;
            return filas;
        }

        private async Task ConfigurarConsumidorFilaAsync(string nomeFila, Func<LancamentoDto, string, Task<bool>> onMessageReceived, CancellationToken cancellationToken)
        {
            if (_channel == null)
                return;

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogDebug("Mensagem recebida da fila {Fila}: {MessageId}", nomeFila, messageId);

                try
                {
                    var lancamento = JsonSerializer.Deserialize<LancamentoDto>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (lancamento != null)
                    {
                        var processado = await onMessageReceived(lancamento, nomeFila);

                        if (processado)
                        {
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            _logger.LogDebug("Mensagem {MessageId} processada e confirmada", messageId);
                        }
                        else
                        {
                            await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                            _logger.LogWarning("Mensagem {MessageId} não foi processada, retornando à fila", messageId);
                        }
                    }
                    else
                    {
                        _logger.LogError("Não foi possível deserializar a mensagem {MessageId}", messageId);
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Erro ao deserializar mensagem {MessageId} da fila {Fila}", messageId, nomeFila);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem {MessageId} da fila {Fila}", messageId, nomeFila);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            var consumerTag = await _channel.BasicConsumeAsync(
                queue: nomeFila,
                autoAck: false,
                consumer: consumer);

            if (!string.IsNullOrEmpty(consumerTag))
            {
                _consumingQueues.Add(consumerTag);
                _logger.LogInformation("Consumidor iniciado para fila {Fila} com tag {ConsumerTag}", nomeFila, consumerTag);
            }
        }        private async Task FecharConexaoAsync()
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

                _consumingQueues.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao fechar conexão RabbitMQ");
            }
        }        public void Dispose()
        {
            if (!_disposed)
            {
                // Para dispose síncrono, usamos a versão síncrona
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
                    _consumingQueues.Clear();
                    _disposed = true;
                }
            }
        }
    }
}
