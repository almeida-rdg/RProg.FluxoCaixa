using FluentAssertions;
using RProg.FluxoCaixa.Consolidado.Application.Queries;
using Xunit;

namespace RProg.FluxoCaixa.Consolidado.Test.Application.Queries
{
    /// <summary>
    /// Testes unitários para ObterConsolidadosPorPeriodoQuery.
    /// </summary>
    public class ObterConsolidadosPorPeriodoQueryTests
    {        [Fact]
        public void Constructor_ComPeriodoValido_DeveSerCriadoComSucesso()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var tipoConsolidacao = "GERAL";

            // Act
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);

            // Assert
            query.DataInicial.Should().Be(dataInicial.Date);
            query.DataFinal.Should().Be(dataFinal.Date);
            query.TipoConsolidacao.Should().Be(tipoConsolidacao);
        }        [Fact]
        public void Constructor_ComDataFinalAnteriorADataInicial_DeveLancarArgumentException()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 31);
            var dataFinal = new DateTime(2024, 1, 1);
            var tipoConsolidacao = "GERAL";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao));
            
            exception.Message.Should().Contain("A data final não pode ser anterior à data inicial.");
        }        [Fact]
        public void Constructor_ComDataIgual_DeveSerCriadoComSucesso()
        {
            // Arrange
            var data = new DateTime(2024, 1, 15);
            var tipoConsolidacao = "CATEGORIA";

            // Act
            var query = new ObterConsolidadosPorPeriodoQuery(data, data, tipoConsolidacao);

            // Assert
            query.DataInicial.Should().Be(data.Date);
            query.DataFinal.Should().Be(data.Date);
            query.TipoConsolidacao.Should().Be(tipoConsolidacao);
        }        [Theory]
        [InlineData("2024-01-01 14:30:00", "2024-01-01")]
        [InlineData("2024-01-01 23:59:59", "2024-01-01")]
        [InlineData("2024-01-01 00:00:01", "2024-01-01")]
        public void Constructor_DeveNormalizarDatasParaDate(string dataHoraInput, string dataEsperada)
        {
            // Arrange
            var dataInicial = DateTime.Parse(dataHoraInput);
            var dataFinal = dataInicial.AddDays(1);
            var dataEsperadaParsed = DateTime.Parse(dataEsperada);
            var tipoConsolidacao = "GERAL";

            // Act
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);

            // Assert
            query.DataInicial.Should().Be(dataEsperadaParsed.Date);
            query.DataInicial.TimeOfDay.Should().Be(TimeSpan.Zero);
            query.TipoConsolidacao.Should().Be(tipoConsolidacao);
        }

        [Theory]
        [InlineData("GERAL")]
        [InlineData("CATEGORIA")]
        public void Constructor_ComTipoConsolidacaoValido_DeveSerCriadoComSucesso(string tipoConsolidacao)
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);

            // Act
            var query = new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacao);

            // Assert
            query.TipoConsolidacao.Should().Be(tipoConsolidacao);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("INVALIDO")]
        public void Constructor_ComTipoConsolidacaoInvalido_DeveLancarArgumentException(string tipoConsolidacaoInvalido)
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new ObterConsolidadosPorPeriodoQuery(dataInicial, dataFinal, tipoConsolidacaoInvalido));
            
            exception.Message.Should().Contain("Tipo de consolidação deve ser 'GERAL' ou 'CATEGORIA'");
        }
    }
}
