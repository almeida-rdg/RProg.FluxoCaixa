using System.Data;
using Dapper;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;

namespace RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper
{
    /// <summary>
    /// Repositório de lançamentos usando Dapper para acesso a dados.
    /// </summary>
    public class LancamentoRepository : ILancamentoRepository
    {
        private readonly IDbConnection _conexao;

        public LancamentoRepository(IDbConnection conexao)
        {
            _conexao = conexao;
        }        public async Task<int> CriarLancamentoAsync(Lancamento lancamento)
        {
            var sql = @"INSERT INTO Lancamentos (Valor, Tipo, Data, Categoria, Descricao) VALUES (@Valor, @Tipo, @Data, @Categoria, @Descricao); SELECT CAST(SCOPE_IDENTITY() as int);";
            var id = await _conexao.QuerySingleAsync<int>(sql, lancamento);
            return id;
        }public async Task<IEnumerable<Lancamento>> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken)
        {
            var sql = "SELECT * FROM Lancamentos WHERE CAST(Data AS DATE) = @Data";

            cancellationToken.ThrowIfCancellationRequested();

            var resultados = await _conexao.QueryAsync<dynamic>(sql, new { Data = data.Date });

            return resultados.Select(r => new Lancamento(
                valor: r.Valor,
                tipo: Enum.Parse<TipoLancamento>(r.Tipo.ToString()),
                data: r.Data,
                categoria: r.Categoria,
                descricao: r.Descricao,
                id: r.Id
            ));
        }        public async Task<Lancamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken)
        {
            var sql = "SELECT * FROM Lancamentos WHERE Id = @Id";
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var resultado = await _conexao.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
            
            if (resultado == null)
                return null;

            return new Lancamento(
                valor: resultado.Valor,
                tipo: Enum.Parse<TipoLancamento>(resultado.Tipo.ToString()),
                data: resultado.Data,
                categoria: resultado.Categoria,
                descricao: resultado.Descricao,
                id: resultado.Id
            );
        }

        public async Task<IEnumerable<Lancamento>> ObterPorIntervaloAsync(DateTime dataInicial, DateTime dataFinal, CancellationToken cancellationToken)
        {
            var sql = "SELECT * FROM Lancamentos WHERE Data >= @DataInicial AND Data < @DataFinal ORDER BY Data";
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var resultados = await _conexao.QueryAsync<dynamic>(sql, new { DataInicial = dataInicial, DataFinal = dataFinal });
            
            return resultados.Select(r => new Lancamento(
                valor: r.Valor,
                tipo: Enum.Parse<TipoLancamento>(r.Tipo.ToString()),
                data: r.Data,
                categoria: r.Categoria,
                descricao: r.Descricao,
                id: r.Id
            ));
        }
    }
}
