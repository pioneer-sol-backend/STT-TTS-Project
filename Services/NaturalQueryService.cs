using MySql.Data.MySqlClient;
using NaturalQuery;
using System.Text.Json;

namespace ST_TTS_Project.Services
{
    public class NaturalQueryService
    {
        private readonly INaturalQueryEngine _engine;
        private readonly string _connectionString;

        public NaturalQueryService(
            INaturalQueryEngine engine,
            IConfiguration configuration)
        {
            _engine = engine;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<string> AskAsync(string question)
        {
            try
            {
                // 1. Generate SQL via GPT
                
                var result = await _engine.AskAsync(question);
                string sql = result.Sql;

                /*// 2. Safety guard — block anything that's not SELECT
                var upperSql = sql.Trim().ToUpper();
                if (!upperSql.StartsWith("SELECT") && !upperSql.StartsWith("WITH"))
                {
                    return JsonSerializer.Serialize(new
                    {
                        error = "Only read-only queries (SELECT) are allowed."
                    });
                }

                // 3. Execute against MySQL
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var rows = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(row);
                }

             c*/

                return sql.ToString();
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    question
                });
        
            }
        
        }
    }
}