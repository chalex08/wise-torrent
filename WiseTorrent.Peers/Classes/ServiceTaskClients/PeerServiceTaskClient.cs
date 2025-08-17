using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Peers.Types;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class PeerServiceTaskClient : IPeerServiceTaskClient
	{
		private readonly ILogger<PeerServiceTaskClient> _logger;
		private CancellationToken CToken { get; set; }
		private readonly TorrentSession _torrentSession;

		private readonly SemaphoreSlim _receiveSemaphore = new(100);
		private readonly SemaphoreSlim _sendSemaphore = new(100);
		private readonly SemaphoreSlim _keepAliveSemaphore = new(50);
		private readonly SemaphoreSlim _updateSemaphore = new(50);

		public PeerServiceTaskClient(ILogger<PeerServiceTaskClient> logger, TorrentSession torrentSession)
		{
			_logger = logger;
			_torrentSession = torrentSession;
		}

		public async Task StartServiceTask(CancellationToken cToken)
		{
			CToken = cToken;
			_logger.Info("Peer initialisation started");

			foreach (var peerEndpoint in _torrentSession.KnownPeers)
			{
				_torrentSession.PeerManager.AddPeer(peerEndpoint);
			}

			_torrentSession.PeerManager.ConnectToPeers();
			_logger.Info("Connected to peers");

			foreach (var peer in _torrentSession.Peers)
			{
				var peerCTS = CancellationTokenSource.CreateLinkedTokenSource(CToken);
				var bundle = new PeerTaskBundle(
					SafeRun(() => ReceiveServiceTaskClient.HandlePeerReceive(peer, peerCTS.Token), peer, "Receive", _receiveSemaphore),
					SafeRun(() => SendServiceTaskClient.HandlePeerSend(peer, peerCTS.Token), peer, "Send", _sendSemaphore),
					SafeRun(() => KeepAliveServiceTaskClient.HandlePeerKeepAlive(peer, peerCTS.Token), peer, "Keep Alive", _keepAliveSemaphore),
					SafeRun(() => UpdateServiceTaskClient.HandlePeerUpdate(peer, peerCTS.Token), peer, "Update State", _updateSemaphore),
					peerCTS
				);

				_torrentSession.PeerTasks[peer] = bundle;
			}

			_logger.Info("Peer service task started");

			while (!CToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(5000, CToken);
					// period cleanup
					// metric updates
				}
				catch (Exception ex)
				{
					_logger.Error("Peer service loop encountered error", ex);
				}
			}

			foreach (var bundle in _torrentSession.PeerTasks.Values)
			{
				bundle.CTS.Cancel();
				await Task.WhenAll(bundle.ReceiveTask, bundle.SendTask, bundle.KeepAliveTask, bundle.UpdateStateTask);
				// proper cleanup
			}

			_logger.Info("Peer service task stopped");
		}

		private Task SafeRun(Func<Task> taskFunc, Peer peer, string taskName, SemaphoreSlim semaphore)
		{
			return Task.Run(async () =>
			{
				await semaphore.WaitAsync();
				try
				{
					await taskFunc();
				}
				catch (Exception ex)
				{
					_logger.Error($"Peer {peer.PeerID} {taskName} failed", ex);
				}
				finally
				{
					semaphore.Release();
				}
			});
		}
	}
}