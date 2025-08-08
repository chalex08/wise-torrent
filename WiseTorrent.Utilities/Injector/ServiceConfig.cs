using Microsoft.Extensions.DependencyInjection;
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
		public static void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(typeof(ILogger<>), typeof(BufferLogger<>));
			services.AddSingleton<ILogService, LogService>();
			services.AddParsingDependencies();
			services.AddPeersDependencies();
			services.AddPiecesDependencies();
			services.AddStorageDependencies();
			services.AddTrackersDependencies();
		}
	}
}
