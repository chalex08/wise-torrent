using WiseTorrent.Peers.Types;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class ReceiveServiceTaskClient
	{
		private readonly ILogger<ReceiveServiceTaskClient> _logger;
		private CancellationToken CToken { get; set; }
		private Dictionary<Peer, CancellationTokenSource> PeerCTokens { get; set; } = new();
		private readonly TorrentSession _torrentSession;

		public ReceiveServiceTaskClient(ILogger<ReceiveServiceTaskClient> logger, TorrentSession torrentSession)
		{
			_logger = logger;
			_torrentSession = torrentSession;
		}

		public async Task StartServiceTask(CancellationToken cToken)
		{
			CToken = cToken;
			_logger.Info("Receive service task started");

			while (!CToken.IsCancellationRequested)
			{
				foreach (var peer in _torrentSession.Peers)
				{
					_ = Task.Run(async () =>
					{
						try
						{
							var cts = new CancellationTokenSource();
							PeerCTokens.TryAdd(peer, cts);
							await HandlePeerReceive(peer, cts.Token);
						}
						catch (OperationCanceledException)
						{
							_logger.Info("Individual peer receive task canceled cleanly");
						}
						catch (Exception ex)
						{
							_logger.Error("Individual peer receive task failed", ex);
						}
					});
				}
			}

			StopAllPeerReceivers();
			_logger.Info("Receive service task stopped");
		}

		private async Task HandlePeerReceive(Peer peer, CancellationToken pCToken)
		{
			while (!pCToken.IsCancellationRequested)
			{
				var buffer = new byte[4096];
				int bytesRead = await peer.Stream.ReadAsync(buffer, 0, buffer.Length, pCToken);
				if (bytesRead > 0)
				{
					var message = PeerMessage.FromBytes(buffer[..bytesRead]);
					DispatchMessage(peer, message);
				}
				else
				{
					peer.Disconnect();
					break;
				}
			}
		}

		private void StopAllPeerReceivers()
		{
			foreach (var cts in PeerCTokens.Values)
			{
				cts.Cancel();
			}
		}
	}
}
