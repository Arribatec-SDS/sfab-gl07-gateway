using Arribatec.Nexus.Client.Extensions;
using Arribatec.Nexus.Client.TaskExecution;

var builder = WebApplication.CreateBuilder(args);


// Add COMPLETE Nexus architecture in ONE call
builder.AddArribatecNexus(
    applicationName: "SfabGl07GatewayApp",
    productShortName: "sfab-gl07-gateway",
    options =>
    {
        options.Loki = new LokiOptions
        {
            Url = "http://localhost:3100",
            WriteToConsole = true
        };
    });
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();


// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CRITICAL: Correct middleware order for v2.2.0
app.UseCors();                  // 2. CORS
app.UseAuthentication();        // 3. Validate JWT token
app.UseArribatecNexus();  // ← Then context middleware
app.UseAuthorization();         // 5. Check roles/policies
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var urls = app.Urls.FirstOrDefault() ?? "http://localhost:7439";
    var port = new Uri(urls).Port;
    
    var swaggerUrl = $"http://localhost:{port}/swagger";
    var apiUrl = $"http://localhost:{port}/api";
    
    // Box inner width is 56 chars (between ║ and ║)
    // Emojis display as 2 chars wide but count as 1, so pad to 56 to compensate
    var swaggerLine = $"  🌐 Swagger:  {swaggerUrl}".PadRight(56);
    var apiLine = $"  🚀 API:      {apiUrl}".PadRight(56);
    
    logger.LogInformation("");
    logger.LogInformation("╔════════════════════════════════════════════════════════╗");
    logger.LogInformation("║                                                        ║");
    logger.LogInformation("║   ███╗   ██╗███████╗██╗  ██╗██╗   ██╗███████╗          ║");
    logger.LogInformation("║   ████╗  ██║██╔════╝╚██╗██╔╝██║   ██║██╔════╝          ║");
    logger.LogInformation("║   ██╔██╗ ██║█████╗   ╚███╔╝ ██║   ██║███████╗          ║");
    logger.LogInformation("║   ██║╚██╗██║██╔══╝   ██╔██╗ ██║   ██║╚════██║          ║");
    logger.LogInformation("║   ██║ ╚████║███████╗██╔╝ ██╗╚██████╔╝███████║          ║");
    logger.LogInformation("║   ╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝          ║");
    logger.LogInformation("║                                                        ║");
    logger.LogInformation("║            ✨ API is ready! ✨                         ║");
    logger.LogInformation("║                                                        ║");
    logger.LogInformation("╠════════════════════════════════════════════════════════╣");
    logger.LogInformation($"║{swaggerLine}║");
    logger.LogInformation($"║{apiLine}║");
    logger.LogInformation("╚════════════════════════════════════════════════════════╝");
    logger.LogInformation("");
});

app.Run();
