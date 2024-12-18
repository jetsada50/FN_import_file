using Demo;
using Demo.Shared.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5142") });

builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<FileValidationService>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
