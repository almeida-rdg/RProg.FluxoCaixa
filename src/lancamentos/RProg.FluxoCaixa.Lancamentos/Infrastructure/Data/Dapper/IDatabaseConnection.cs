namespace RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper
{
    public interface IDatabaseConnection
    {
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<T> QuerySingleAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
    }
}
