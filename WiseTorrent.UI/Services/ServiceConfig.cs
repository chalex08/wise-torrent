using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.Core.Interfaces;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Pieces.Interfaces;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.UI.Services
{
	public static class ServiceConfig
	{
		public static void ConfigureServices(IServiceCollection services)
		{
			services.AddUtilityDependencies();
			services.AddCoreDependencies();
			services.AddParsingDependencies();
			services.AddPeersDependencies();
			services.AddPiecesDependencies();
			services.AddStorageDependencies();
			services.AddTrackersDependencies();
		}
	}
}
