using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Peers.Classes;

namespace WiseTorrent.Peers.Interfaces
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddPeersDependencies(this IServiceCollection services)
		{
			services.AddSingleton<IHandshake, Handshake>();
			services.AddSingleton<IPeerManager, PeerManager>();
			return services;
		}
	}
}
