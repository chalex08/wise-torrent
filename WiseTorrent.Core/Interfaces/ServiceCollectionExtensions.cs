using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Core.Classes;

namespace WiseTorrent.Core.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static void AddCoreDependencies(this IServiceCollection services)
		{
			services.AddSingleton<ITorrentEngine, TorrentEngine>();
			services.AddSingleton<ITorrentSessionManager, TorrentSessionManager>();
		}
	}

}
