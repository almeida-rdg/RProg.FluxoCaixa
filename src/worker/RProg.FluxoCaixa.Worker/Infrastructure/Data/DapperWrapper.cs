using System.Data;
using Dapper;

namespace RProg.FluxoCaixa.Worker.Infrastructure.Data
{
    /// <summary>
    /// Implementação do wrapper para as operações do Dapper
    /// </summary>
    public class DapperWrapper : IDapperWrapper
    {
        public async Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public async Task<T> QuerySingleAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public async Task<int> ExecuteAsync(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
        }
    }
}
