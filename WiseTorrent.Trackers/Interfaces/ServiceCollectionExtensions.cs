using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Trackers.Classes;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Trackers.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static void AddTrackersDependencies(this IServiceCollection services)
		{
			services.AddSingleton<HTTPTrackerClient>();
			services.AddSingleton<UDPTrackerClient>();
			services.AddSingleton<Func<PeerDiscoveryProtocol, ITrackerClient>>(provider => key =>
			{
				return key switch
				{
					PeerDiscoveryProtocol.HTTP => provider.GetRequiredService<HTTPTrackerClient>(),
					PeerDiscoveryProtocol.HTTPS => provider.GetRequiredService<HTTPTrackerClient>(),
					PeerDiscoveryProtocol.UDP => provider.GetRequiredService<UDPTrackerClient>(),
					PeerDiscoveryProtocol.DHT => throw new NotImplementedException(),
					_ => throw new ArgumentException($"Unknown parser type: {key}")
				};
			});
			services.AddTransient<ITrackerServiceTaskClient, TrackerServiceTaskClient>();
		}
	}
}
