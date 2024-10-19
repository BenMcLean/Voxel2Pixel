using BenVoxel;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Voxel2Pixel.Model.FileFormats;

namespace Voxel2Pixel.Web;

public class Program
{
	public static async Task Main(string[] args)
	{
		WebAssemblyHostBuilder? builder = WebAssemblyHostBuilder.CreateDefault(args);
		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");
		builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
		builder.Services.AddMudServices();
		await builder.Build().RunAsync();
	}
}
