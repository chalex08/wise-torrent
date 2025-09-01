using Microsoft.Extensions.DependencyInjection;

namespace WiseTorrent.UI.Services
{
	public static class UIServiceConfig
	{
		public static void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<FullscreenStateService>();
			services.AddTransient<IFilePickerService, FilePickerService>();
			services.AddScoped<UIStateService>();
		}
	}
}
