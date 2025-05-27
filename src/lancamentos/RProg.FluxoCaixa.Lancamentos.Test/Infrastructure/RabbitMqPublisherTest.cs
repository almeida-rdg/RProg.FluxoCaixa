namespace RProg.FluxoCaixa.Lancamentos.Test.Infrastructure
{
    public class RabbitMqPublisherTest
    {
        /*[Fact(DisplayName = "Dado uma mensagem válida, Quando publicar, Então deve logar a publicação e publicar na fila (mockado)")]
        public async Task DadoMensagemValida_QuandoPublicar_EntaoLogaPublicacaoEPublicaNaFila_Mockado()
        {
            // Arrange (Dado)
            var nomeFila = "testqueue";
            var mensagem = "mensagem de teste";

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["RabbitMQ:QueueName"]).Returns(nomeFila);

            var loggerMock = new Mock<ILogger<RabbitMqPublisher>>();

            var canalMock = new Mock<IChannel>();

            canalMock.Setup(m => m.QueueDeclareAsync(
                nomeFila,
                true,
                false,
                false,
                null,
                false,
                false,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueueDeclareOk(nomeFila, 0, 0));

            canalMock.Setup(m => m.BasicPublishAsync(
                string.Empty,
                nomeFila,
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
                .Returns(new ValueTask());

            var canalWrapperMock = new Mock<RabbitMqChannelWrapper>(canalMock.Object);

            var conexaoMock = new Mock<IConnection>();
            conexaoMock.Setup(c => c.CreateChannelAsync(
                null,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(canalMock.Object);

            var factoryMock = new Mock<IConnectionFactory>();
            factoryMock.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(conexaoMock.Object);

            // Substitui a criação do wrapper pelo mock
            // Para isso, seria necessário injetar o wrapper via construtor ou método factory
            // Aqui, apenas exemplificando a estrutura correta do teste

            var publisher = new RabbitMqPublisher(configMock.Object, loggerMock.Object, factoryMock.Object);

            // Act (Quando)
            await publisher.PublicarMensagemAsync(mensagem);

            // Assert (Então)
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Mensagem publicada no RabbitMQ")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            canalWrapperMock.Verify(m => m.DeclararFilaAsync(
                nomeFila, true, false, false, null, It.IsAny<CancellationToken>()), Times.Once);

            canalWrapperMock.Verify(m => m.PublicarMensagemAsync(
                string.Empty, nomeFila, It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
        }*/
    }
}
