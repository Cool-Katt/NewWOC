using WOC;
using Serilog;
using static WOC.Helpers;

//some setup for logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/serilog-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Warning("Starting the furnaces...");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    var app = builder.Build();
    var settings = app.Configuration.GetSection("WOC__Settings");
    
    //mapping for GET and POST, may change in the future
    app.MapGet("/", () => "Use /woc/{tech}/{siteId}");
    app.MapGet("/woc/{tech}/{siteId}", (string tech, string siteId) => Result(settings, tech, siteId));
    app.MapPost("/woc/{tech}/{siteId}", (string tech, string siteId) => Result(settings, tech, siteId));

    app.Run();
}
catch (Exception ex)
{
    Log.Warning("Umm, is that supposed to happen boss?");
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Warning("Job well done. Going home.");
    Log.CloseAndFlush();
}

return;

IResult Result(IConfiguration configurationSection, string tech, string siteId)
{
    Log.Warning($"Called for {tech} tag and {siteId} site. Baking file now...");
    Helpers.Init(configurationSection);
    var excelPackage = GenerateExcelFile(tech, siteId);
    return Results.File(excelPackage.GetAsByteArray(),
        contentType: @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileDownloadName: $"WOC_{tech}_{siteId}.xlsx");
}