using MySql.Data.MySqlClient;
using NaturalQuery.Models;

namespace ST_TTS_Project.Helpers
{
    public static class SchemaLoader
    {
        public static List<TableSchema> FromMySql(string connectionString)
        {
            var tables = new Dictionary<string, List<ColumnDef>>();

            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            using var cmd = new MySqlCommand(@"
                SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE()
                ORDER BY TABLE_NAME, ORDINAL_POSITION", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var tableName = reader.GetString("TABLE_NAME");
                var colName = reader.GetString("COLUMN_NAME");
                var dataType = reader.GetString("DATA_TYPE");

                if (!tables.ContainsKey(tableName))
                    tables[tableName] = new List<ColumnDef>();

                tables[tableName].Add(new ColumnDef(colName, dataType));
            }

            return tables
                .Select(t => new TableSchema(t.Key, t.Value))
                .ToList();
        }
    }
}