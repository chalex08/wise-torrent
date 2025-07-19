using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Pieces.Interfaces;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Classes;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Utilities.Injector
{
	public static class ServiceConfig
	{
		public static IServiceProvider ConfigureServices()
		{
			var services = new ServiceCollection();

			services.AddSingleton<ILogger, ConsoleLogger>();
			services.AddParsingDependencies();
			services.AddPeersDependencies();
			services.AddPiecesDependencies();
			services.AddStorageDependencies();
			services.AddTrackersDependencies();
			
			return services.BuildServiceProvider();
		}

	}
}
