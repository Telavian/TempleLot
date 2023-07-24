using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TempleLotViewer.Services;
using TempleLotViewer.Services.FileService.Interfaces;

namespace TempleLotViewer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Task.Yield();

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddMudServices();
            builder.Services.AddSingleton<IFileService>(p => new EmbeddedResourceFileService());

            await builder.Build().RunAsync();
        }
    }
}