using MySql.Data;
using NaturalQuery;
using NaturalQuery.Extensions;
using NaturalQuery.Models;
using NaturalQuery.Providers;
using ST_TTS_Project.Helpers;
using ST_TTS_Project.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. MVC Controllers + Views
builder.Services.AddControllersWithViews();

// 2. Register Custom Services
builder.Services.AddSingleton<VoskService>();

builder.Services.AddScoped<NaturalQueryService>();
builder.Services.AddScoped<IQueryExecutor, ST_TTS_Project.Services.MySqlQueryExecutor>();
// 3. Load Schema & Register NaturalQuery (Before builder.Build)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Server=localhost;Database=patient;Uid=root;Pwd=umer;";

var schema = SchemaLoader.FromMySql(connectionString);

/*builder.Services.AddNaturalQuery(options =>
{
    options.Tables = schema;
})
.UseOpenAiProvider(
    builder.Configuration["OpenAi:ApiKey"] ?? "YOUR_OPENAI_API_KEY",
    model: "gpt-4o"
).UseMySqlExecutor(connectionString);
*/
var anthropicKey = builder.Configuration["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Anthropic API key is not configured.");

builder.Services.AddNaturalQuery(options =>
{
    options.Tables = schema;
})
.UseAnthropicProvider(anthropicKey, "claude-3-5-sonnet-latest")
.UseMySqlExecutor(connectionString);
// -------------------------------------------------------------
// Build App Pipeline
// -------------------------------------------------------------
var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

// Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=STT}/{id?}");

app.Run();