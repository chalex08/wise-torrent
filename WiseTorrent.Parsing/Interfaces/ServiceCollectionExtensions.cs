using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Parsing.Classes;

namespace WiseTorrent.Parsing.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddParsingDependencies(this IServiceCollection services)
		{
			services.AddSingleton<IBEncodeReader, BEncodeReader>();
			services.AddSingleton<ITorrentParser, TorrentParser>();
			services.AddSingleton<ITrackerResponseParser, TrackerResponseParser>();
			return services;
		}
	}

}
