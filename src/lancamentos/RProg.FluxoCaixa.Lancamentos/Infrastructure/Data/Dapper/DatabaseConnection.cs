using Dapper;
using System.Data;

namespace RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper
{
    public class DatabaseConnection : IDatabaseConnection
    {
        private readonly IDbConnection _connection;

        public DatabaseConnection(IDbConnection connection)
        {
            _connection = connection;
        }        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await _connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await _connection.QuerySingleAsync<T>(sql, param);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await _connection.QueryAsync<T>(sql, param);
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await _connection.ExecuteAsync(sql, param);
        }
    }
}
