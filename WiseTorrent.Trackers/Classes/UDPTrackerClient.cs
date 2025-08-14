using WiseTorrent.Peers.Types;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Trackers.Classes
{
	internal class UDPTrackerClient : ITrackerClient
    {
		private readonly ILogger<UDPTrackerClient> _logger;

		private event Action<List<Peer>> OnTrackerResponse;

		public UDPTrackerClient(ILogger<UDPTrackerClient> logger)
		{
			_logger = logger;
		}

		public void InitialiseClient(Action<List<Peer>> onTrackerResponse)
		{
			OnTrackerResponse = onTrackerResponse;
		}

		public async Task<(int, bool)> RunServiceTask(int interval, string trackerAddress, Action<List<Peer>> onTrackerResponse, CancellationToken cToken)
		{
			return await Task.FromResult((0, false));
		}
	}
}
