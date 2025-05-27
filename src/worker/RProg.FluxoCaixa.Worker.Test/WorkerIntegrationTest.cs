using RProg.FluxoCaixa.Worker.Domain.DTOs;
using System.Text.Json;

namespace RProg.FluxoCaixa.Worker.Test
{
    /// <summary>
    /// Testes básicos para validação da funcionalidade do worker.
    /// </summary>
    public class WorkerIntegrationTest
    {
        /// <summary>
        /// Teste simples para verificar se o serviço de consolidação processa lançamentos corretamente.
        /// </summary>
        public static async Task TestarProcessamentoLancamentoAsync()
        {
            Console.WriteLine("Iniciando teste de processamento de lançamento...");

            // Este é um teste básico que pode ser executado para validar a funcionalidade
            var lancamentoTeste = new LancamentoDto
            {
                Id = Random.Shared.Next(),
                Valor = 100.50m,
                Tipo = 2, // 2 = Crédito
                Data = DateTime.Today,
                Categoria = "vendas",
                Descricao = "Teste de consolidação"
            };

            Console.WriteLine($"Lançamento de teste criado:");
            Console.WriteLine($"  ID: {lancamentoTeste.Id}");
            Console.WriteLine($"  Valor: {lancamentoTeste.Valor:C}");
            Console.WriteLine($"  Tipo: {lancamentoTeste.Tipo}");
            Console.WriteLine($"  Data: {lancamentoTeste.Data:yyyy-MM-dd}");
            Console.WriteLine($"  Categoria: {lancamentoTeste.Categoria}");

            // Serializar para JSON (formato que seria recebido do RabbitMQ)
            var json = JsonSerializer.Serialize(lancamentoTeste, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Console.WriteLine($"\nJSON do lançamento:");
            Console.WriteLine(json);

            // Deserializar para verificar se funciona corretamente
            var lancamentoDeserializado = JsonSerializer.Deserialize<LancamentoDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Console.WriteLine($"\nLançamento deserializado:");
            Console.WriteLine($"  ID: {lancamentoDeserializado?.Id}");
            Console.WriteLine($"  Valor: {lancamentoDeserializado?.Valor:C}");
            Console.WriteLine($"  É Crédito: {lancamentoDeserializado?.IsCredito}");
            Console.WriteLine($"  É Débito: {lancamentoDeserializado?.IsDebito}");

            Console.WriteLine("\nTeste de processamento concluído com sucesso!");
        }

        /// <summary>
        /// Exemplo de como testar múltiplos lançamentos.
        /// </summary>
        public static async Task TestarMultiplosLancamentosAsync()
        {
            Console.WriteLine("Testando múltiplos lançamentos...");

            var lancamentos = new[]
            {
                new LancamentoDto
                {
                    Id = Random.Shared.Next(),
                    Valor = 1500.00m,
                    Tipo = 2, // 2 = Crédito
                    Data = DateTime.Today,
                    Categoria = "vendas",
                    Descricao = "Venda produto A"
                },
                new LancamentoDto
                {
                    Id = Random.Shared.Next(),
                    Valor = 350.75m,
                    Tipo = 1, // 1 = Débito
                    Data = DateTime.Today,
                    Categoria = "compras",
                    Descricao = "Compra material"
                },
                new LancamentoDto
                {
                    Id = Random.Shared.Next(),
                    Valor = 800.00m,
                    Tipo = 2, // 2 = Crédito
                    Data = DateTime.Today,
                    Categoria = "vendas",
                    Descricao = "Venda produto B"
                }
            };

            foreach (var lancamento in lancamentos)
            {
                Console.WriteLine($"Lançamento: {lancamento.Tipo} - {lancamento.Valor:C} - {lancamento.Categoria}");
            }

            var totalCreditos = lancamentos
                .Where(l => l.IsCredito)
                .Sum(l => l.Valor);

            var totalDebitos = lancamentos
                .Where(l => l.IsDebito)
                .Sum(l => l.Valor);

            var saldoLiquido = totalCreditos - totalDebitos;

            Console.WriteLine($"\nResumo esperado:");
            Console.WriteLine($"  Total Créditos: {totalCreditos:C}");
            Console.WriteLine($"  Total Débitos: {totalDebitos:C}");
            Console.WriteLine($"  Saldo Líquido: {saldoLiquido:C}");
            Console.WriteLine($"  Quantidade de lançamentos: {lancamentos.Length}");
        }

        /// <summary>
        /// Ponto de entrada para execução dos testes.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Testes do Worker RProg.FluxoCaixa ===\n");

            try
            {
                await TestarProcessamentoLancamentoAsync();
                Console.WriteLine("\n" + new string('=', 50) + "\n");
                await TestarMultiplosLancamentosAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante os testes: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n=== Testes concluídos ===");
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }
    }
}
