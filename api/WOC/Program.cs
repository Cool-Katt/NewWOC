using Serilog;
using WOC;
using static WOC.Helpers;

// some setup for logging
// changed minimum logging level for brevity of log files
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.File("logs/serilog-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Warning("Starting the furnaces...");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    // Exception handling
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions.Add("message", ctx.Exception?.Message);
            ctx.ProblemDetails.Extensions.Add("query", ctx.Exception?.Data["query"]);
        });

    var app = builder.Build();
    app.UseExceptionHandler();

    var settings = app.Configuration.GetSection("WOC__Settings");

    // mapping for GET and POST, may change in the future
    app.MapGet("/", () => "Use /json/{tech}/{siteId}");
    app.MapGet("/woc/{tech}/{siteId}", (string tech, string siteId) => DeprecatedResult(settings, tech, siteId));
    app.MapPost("/woc/{tech}/{siteId}", (string tech, string siteId) => DeprecatedResult(settings, tech, siteId));
    // new endpoints for JSON response
    app.MapGet("/json/{tech}/{siteId}", (string tech, string siteId) => ResultJson(settings, tech, siteId));
    app.MapPost("/json/{tech}/{siteId}", (string tech, string siteId) => ResultJson(settings, tech, siteId));

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

[Obsolete("Use of this response has been deprecated, API should return JSON")]
IResult DeprecatedResult(IConfiguration configurationSection, string tech, string siteId)
{
    var techUpper = tech.ToUpper();
    var siteIdUpper = siteId.ToUpper();
    Log.Warning($"Called for {techUpper} tag and {siteIdUpper} site. Baking file now...");
    HelperInit(configurationSection, techUpper, siteIdUpper);
    var excelPackage = GenerateExcelFile();
    return Results.File(excelPackage.GetAsByteArray(),
        @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        techUpper.Equals(@"CONS") ? $"WOC_{siteIdUpper}.xlsx" : $"WOC_{techUpper}_{siteIdUpper}.xlsx");
}


IResult ResultJson(IConfiguration configurationSection, string tech, string siteId)
{
    var techUpper = tech.ToUpper();
    var siteIdUpper = siteId.ToUpper();
    Log.Warning($"Called for {techUpper} tag and {siteIdUpper} site. Baking JSON response now...");
    HelperInit(configurationSection, techUpper, siteIdUpper);
    var excelPackage = GenerateExcelFile();
    var fileDownloadName =
        techUpper.Equals(@"CONS") ? $"WOC_{siteIdUpper}.xlsx" : $"WOC_{techUpper}_{siteIdUpper}.xlsx";
    var contentType = @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    return Results.Ok(
        new ResponseBodyAsJson(excelPackage.GetAsByteArray(), fileDownloadName, contentType)
    );
}