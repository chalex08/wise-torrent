using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Core.Classes;

namespace WiseTorrent.Core.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
		{
			services.AddSingleton<ITorrentEngine, TorrentEngine>();
			services.AddSingleton<ITorrentSessionManager, TorrentSessionManager>();
			return services;
		}
	}

}
