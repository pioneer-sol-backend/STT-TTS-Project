using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ST_TTS_Project.Services;
using System.Data;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Vosk;

namespace ST_TTS_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly VoskService _voskService;
        private readonly IConfiguration _configuration;
        private readonly NaturalQueryService _naturalQueryService;
        public HomeController(VoskService voskService, IConfiguration configuration,
            NaturalQueryService naturalQueryService)
        {
            _voskService = voskService;
            _configuration = configuration;
            _naturalQueryService = naturalQueryService;
        }

        public IActionResult STT()
        {
            return View();
        }

        public IActionResult TTS()
        {
            return View();
        }

        // ─── Existing WebSocket Stream (unchanged) ───
        public async Task Stream()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            using var recognizer = _voskService.CreateRecognizer();
            var buffer = new byte[8192];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    bool completed = recognizer.AcceptWaveform(buffer, result.Count);
                    string text = completed ? recognizer.Result() : recognizer.PartialResult();

                    var response = Encoding.UTF8.GetBytes(text);

                    await socket.SendAsync(
                        new ArraySegment<byte>(response),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> ProcessQuery([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Question))
            {
                return BadRequest(new { error = "Question cannot be empty." });
            }

            // 1. Convert speech text into SQL
            string generatedSql = await _naturalQueryService.AskAsync(request.Question);
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            var sb = new StringBuilder();

            using (MySqlConnection sqlConnection = new MySqlConnection(connectionString))
            {
                try
                {
                    await sqlConnection.OpenAsync();

                    using (MySqlCommand command = new MySqlCommand(generatedSql, sqlConnection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable); // Fills DataTable with query result

                        // Format database rows into line-by-line readable text
                        foreach (DataRow row in dataTable.Rows)
                        {
                            foreach (DataColumn col in dataTable.Columns)
                            {
                                sb.Append($"{col.ColumnName}: {row[col]} | ");
                            }
                            sb.AppendLine();
                        }
                    }

                    // Return formatted database output to front-end
                    return Json(new
                    {
                        success = true,
                        question = request.Question,
                        generatedSql = generatedSql,
                        resultData = sb.ToString() // Fixed: Returning the populated string data
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Execution Error: {ex.Message}");
                    return Json(new
                    {
                        success = false,
                        question = request.Question,
                        generatedSql = generatedSql,
                        error = ex.Message
                    });
                }
            }
        }

        /*       // ─── NEW: Save transcription to MySQL ───
               [HttpPost]
               public async Task<IActionResult> SaveTranscription([FromBody] TranscriptionRequest request)
               {
                   var connectionString = _configuration.GetConnectionString("DefaultConnection")!;

                   using var conn = new MySqlConnection(connectionString);
                   await conn.OpenAsync();

                   using var cmd = new MySqlCommand(
                       "INSERT INTO speech_logs (transcript) VALUES (@text)", conn);
                   cmd.Parameters.AddWithValue("@text", request.Text);

                   await cmd.ExecuteNonQueryAsync();
                   return Ok(new { saved = true, id = cmd.LastInsertedId });
               }

               // ─── NEW: Fetch recent transcriptions from MySQL ───
               [HttpGet]
               public async Task<IActionResult> GetTranscriptions(int limit = 5)
               {
                   var connectionString = _configuration.GetConnectionString("DefaultConnection")!;

                   using var conn = new MySqlConnection(connectionString);
                   await conn.OpenAsync();

                   using var cmd = new MySqlCommand(
                       "SELECT id, transcript, created_at FROM speech_logs ORDER BY id DESC LIMIT @limit", conn);
                   cmd.Parameters.AddWithValue("@limit", limit);

                   using var reader = await cmd.ExecuteReaderAsync();
                   var list = new List<object>();

                   while (await reader.ReadAsync())
                   {
                       list.Add(new
                       {
                           id = reader.GetInt32("id"),
                           transcript = reader.GetString("transcript"),
                           createdAt = reader.GetDateTime("created_at")
                       });
                   }

                   return Json(list);
               }
        */
    }
        public class QueryRequest
        {
            public string Question { get; set; } = "";
        }
        // DTO for the save endpoint
        public class TranscriptionRequest
        {
            public string Text { get; set; } = "";
        }
    }
