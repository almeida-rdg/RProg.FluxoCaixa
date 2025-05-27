using FluentAssertions;
using RProg.FluxoCaixa.Consolidado.Application.Queries;

namespace RProg.FluxoCaixa.Consolidado.Test.Application.Queries
{
    /// <summary>
    /// Testes unitários para ObterConsolidadosPorPeriodoECategoriaQuery.
    /// </summary>
    public class ObterConsolidadosPorPeriodoECategoriaQueryTests
    {
        [Fact]
        public void Constructor_ComParametrosValidos_DeveSerCriadoComSucesso()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var categoria = "ALIMENTACAO";

            // Act
            var query = new ObterConsolidadosPorPeriodoECategoriaQuery(dataInicial, dataFinal, categoria);

            // Assert
            query.DataInicial.Should().Be(dataInicial.Date);
            query.DataFinal.Should().Be(dataFinal.Date);
            query.Categoria.Should().Be(categoria);
        }

        [Fact]
        public void Constructor_ComDataFinalAnteriorADataInicial_DeveLancarArgumentException()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 31);
            var dataFinal = new DateTime(2024, 1, 1);
            var categoria = "ALIMENTACAO";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new ObterConsolidadosPorPeriodoECategoriaQuery(dataInicial, dataFinal, categoria));
            
            exception.Message.Should().Contain("A data final não pode ser anterior à data inicial.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ComCategoriaNulaOuVazia_DeveLancarArgumentException(string categoria)
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new ObterConsolidadosPorPeriodoECategoriaQuery(dataInicial, dataFinal, categoria));
            
            exception.Message.Should().Contain("Categoria é obrigatória");
        }

        [Fact]
        public void Constructor_ComCategoriaComEspacos_DeveNormalizarCategoria()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var categoria = "  ALIMENTACAO  ";

            // Act
            var query = new ObterConsolidadosPorPeriodoECategoriaQuery(dataInicial, dataFinal, categoria);

            // Assert
            query.Categoria.Should().Be("ALIMENTACAO");
        }

        [Fact]
        public void Constructor_DeveConverterCategoriaParaUpperCase()
        {
            // Arrange
            var dataInicial = new DateTime(2024, 1, 1);
            var dataFinal = new DateTime(2024, 1, 31);
            var categoria = "alimentacao";

            // Act
            var query = new ObterConsolidadosPorPeriodoECategoriaQuery(dataInicial, dataFinal, categoria);

            // Assert
            query.Categoria.Should().Be("ALIMENTACAO");
        }
    }
}
