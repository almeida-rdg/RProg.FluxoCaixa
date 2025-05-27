using System.Data;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Interface para abstrair as operações do Dapper, permitindo testes unitários
    /// </summary>
    public interface IDapperWrapper
    {
        Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<T> QuerySingleAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<int> ExecuteAsync(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
    }
}
