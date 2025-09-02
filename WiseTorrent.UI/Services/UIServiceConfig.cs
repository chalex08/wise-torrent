using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Core.Classes;
using WiseTorrent.Core.Interfaces;

namespace WiseTorrent.UI.Services
{
    public static class UIServiceConfig
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<FullscreenStateService>();
            services.AddTransient<IFilePickerService, FilePickerService>();
            services.AddScoped<UIStateService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<ITorrentSessionManager, TorrentSessionManager>();
        }
    }
}