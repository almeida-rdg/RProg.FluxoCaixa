using RabbitMQ.Client;

namespace RProg.FluxoCaixa.Lancamentos.Test.Infrastructure
{
    public class RabbitMqChannelWrapper
    {
        private readonly IChannel _canal;

        public RabbitMqChannelWrapper(IChannel canal)
        {
            _canal = canal;
        }

        public Task<QueueDeclareOk> DeclararFilaAsync(string nomeFila, bool duravel, bool exclusiva, bool autoDeletar, IDictionary<string, object?>? argumentos, CancellationToken token = default)
        {
            return _canal.QueueDeclareAsync(nomeFila, duravel, exclusiva, autoDeletar, argumentos, false, false, token);
        }

        public ValueTask PublicarMensagemAsync(string exchange, string routingKey, ReadOnlyMemory<byte> corpo, CancellationToken token = default)
        {
            return _canal.BasicPublishAsync(exchange, routingKey, corpo, token);
        }
    }
}
