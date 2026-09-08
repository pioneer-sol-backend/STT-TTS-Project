using MySql.Data.MySqlClient;
using NaturalQuery.Models;
using NaturalQuery.Providers;

namespace ST_TTS_Project.Services
{
    public class MySqlQueryExecutor : IQueryExecutor
    {
        private readonly string _connectionString;

        public MySqlQueryExecutor(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection is not configured.");
        }

        public async Task<List<Dictionary<string, string>>> ExecuteTableQueryAsync(
            string sql,
            CancellationToken ct = default)
        {
            var results = new List<Dictionary<string, string>>();

            await using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync(ct);

            await using var command =
                new MySqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, string>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] =
                        reader.IsDBNull(i)
                            ? ""
                            : reader.GetValue(i)?.ToString() ?? "";
                }

                results.Add(row);
            }

            return results;
        }

        public async Task<List<DataPoint>> ExecuteChartQueryAsync(
            string sql,
            CancellationToken ct = default)
        {
            var results = new List<DataPoint>();

            await using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync(ct);

            await using var command =
                new MySqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                // We need to see the DataPoint constructor/properties
                // before implementing this method correctly.
            }

            return results;
        }
    }
}