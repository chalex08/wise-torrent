using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Parsing.Types;
using WiseTorrent.Trackers.Classes;

namespace WiseTorrent.Trackers.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddTrackersDependencies(this IServiceCollection services)
		{
			services.AddSingleton<HTTPTrackerClient>();
			services.AddSingleton<UDPTrackerClient>();
			services.AddSingleton<ITrackerServiceTaskClient, TrackerServiceTaskClient>();
			services.AddSingleton<Func<PeerDiscoveryProtocol, ITrackerClient>>(provider => key =>
			{
				return key switch
				{
					PeerDiscoveryProtocol.HTTP => provider.GetRequiredService<HTTPTrackerClient>(),
					PeerDiscoveryProtocol.UDP => provider.GetRequiredService<UDPTrackerClient>(),
					PeerDiscoveryProtocol.DHT => throw new NotImplementedException(),
					_ => throw new ArgumentException($"Unknown parser type: {key}")
				};
			});
			return services;
		}
	}
}
