using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Peers.Classes;
using WiseTorrent.Peers.Classes.ServiceTaskClients;

namespace WiseTorrent.Peers.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddPeersDependencies(this IServiceCollection services)
		{
			services.AddSingleton<IHandshake, Handshake>();
			services.AddSingleton<IPeerManager, PeerManager>();
			services.AddSingleton<IPeerChildServiceTaskClient, ReceiveServiceTaskClient>();
			services.AddSingleton<IPeerChildServiceTaskClient, SendServiceTaskClient>();
			services.AddSingleton<IPeerChildServiceTaskClient, KeepAliveServiceTaskClient>();
			services.AddSingleton<IPeerSiblingServiceTaskClient, UpdateStateServiceTaskClient>();
			return services;
		}
	}
}
