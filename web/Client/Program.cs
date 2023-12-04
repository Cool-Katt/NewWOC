using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

using WOC2.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRadzenComponents();
builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// ------- this is for loading appsettings from WASM part of the app --------
var http = new HttpClient()
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
};

//builder.Services.AddScoped(sp => http);

using var response = await http.GetAsync("ApiAddress.json");
using var stream = await response.Content.ReadAsStreamAsync();

builder.Configuration.AddJsonStream(stream);
// ---------------------------------------------------------------------------

var host = builder.Build();
await host.RunAsync();