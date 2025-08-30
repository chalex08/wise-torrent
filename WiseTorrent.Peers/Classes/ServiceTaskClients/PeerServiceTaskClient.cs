using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Pieces.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class PeerServiceTaskClient : IPeerServiceTaskClient
	{
		private readonly ILogger<PeerServiceTaskClient> _logger;
		private readonly ILogger<PeerConnector> _peerConnectorLogger;
		private readonly ILogger<PeerMessageHandler> _peerMessageHandlerLogger;
		private readonly IPeerManager _peerManager;
		private readonly Func<int, IPieceManager> _pieceManagerFactory;
		private IPieceManager? _pieceManager;
		private PeerMessageHandler? _peerMessageHandler;
		private readonly IEnumerable<IPeerChildServiceTaskClient> _childServiceTaskClients;
		private readonly IEnumerable<IPeerSiblingServiceTaskClient> _siblingServiceTaskClients;
		private CancellationToken CToken { get; set; }

		private readonly List<(string, SemaphoreSlim)> _childSemaphoreSlims = new()
		{
			("Receive", new(SessionConfig.MaxPeerReceiveThreads) ),
			( "Send", new(SessionConfig.MaxPeerSendThreads) ),
			( "Keep Alive", new(SessionConfig.MaxPeerKeepAliveThreads) )
		};
		
		private readonly List<(string, SemaphoreSlim)> _siblingSemaphoreSlims = new()
		{
			( "Update State", new(SessionConfig.MaxPeerUpdateStateThreads) )
		};

		public PeerServiceTaskClient(
			IPeerManager peerManager, Func<int, IPieceManager> pieceManagerFactory,
			ILogger<PeerServiceTaskClient> logger, ILogger<PeerConnector> peerConnectorLogger, ILogger<PeerMessageHandler> peerMessageHandlerLogger,
			IEnumerable<IPeerChildServiceTaskClient> childServiceTaskClients, IEnumerable<IPeerSiblingServiceTaskClient> siblingServiceTaskClients)
		{
			_peerManager = peerManager;
			_pieceManagerFactory = pieceManagerFactory;
			_logger = logger;
			_peerConnectorLogger = peerConnectorLogger;
			_peerMessageHandlerLogger = peerMessageHandlerLogger;
			_childServiceTaskClients = childServiceTaskClients;
			_siblingServiceTaskClients = siblingServiceTaskClients;
		}

		public async Task StartServiceTask(TorrentSession torrentSession, CancellationToken cToken)
		{
			CToken = cToken;
			_pieceManager = _pieceManagerFactory(torrentSession.Info.PieceHashes.Length);
			_peerMessageHandler = new PeerMessageHandler(_peerMessageHandlerLogger, torrentSession, _peerManager, _pieceManager);

			InitialisePieces(torrentSession);
			_logger.Info("Initialised pieces");

			_logger.Info("Peer initialisation started");
			_peerManager.PieceManager = _pieceManager;
			await _peerManager.HandleTrackerResponse(torrentSession, _peerConnectorLogger, CToken);
			_logger.Info("Connected to peers");

			InitialiseServiceTaskClients(torrentSession);
			
			_logger.Info("Creating peer task bundles");
			CreatePeerTaskBundles(torrentSession);
			_logger.Info("Successfully created peer task bundles");

			_logger.Info("Starting peer sibling tasks");
			StartPeerSiblingTasks(torrentSession);
			_logger.Info("Successfully started peer sibling tasks");

			SubscribeToEvents(torrentSession);
			_logger.Info("Subscribed to events");

			_logger.Info("Peer service task started");
			while (!CToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(5000, CToken);
				}
				catch (Exception ex)
				{
					_logger.Error("Peer service loop encountered error", ex);
				}
			}

			_logger.Info("Stopping peer child service tasks");
			await StopAllPeerServiceTasks(torrentSession);

			_logger.Info("Peer service task, and all child service tasks, stopped");
		}

		private void InitialisePieces(TorrentSession torrentSession)
		{
			var index = 0;
			foreach (var pieceHash in torrentSession.Info.PieceHashes)
			{
				torrentSession.Pieces.Add(new Piece(index++, pieceHash, _peerManager.GetPieceLength(index)));
			}
		}

		private void InitialiseServiceTaskClients(TorrentSession torrentSession)
		{
			foreach (var childServiceTaskClient in _childServiceTaskClients)
			{
				childServiceTaskClient.TorrentSession = torrentSession;
				childServiceTaskClient.PeerManager = _peerManager;
			}

			foreach (var siblingServiceTaskClients in _siblingServiceTaskClients)
			{
				siblingServiceTaskClients.TorrentSession = torrentSession;
				siblingServiceTaskClients.PeerManager = _peerManager;
			}
		}

		private void CreatePeerTaskBundles(TorrentSession torrentSession)
		{
			foreach (var peer in torrentSession.AllPeers)
			{
				var clientIterator = 0;
				var peerCTS = new CancellationTokenSource();
				var bundle = new PeerTaskBundle(
					_childServiceTaskClients.Select(client =>
					{
						var namedSemaphoreSlim = _childSemaphoreSlims[clientIterator++];
						return SafeRun(() => client.StartServiceTask(peer, peerCTS.Token), namedSemaphoreSlim.Item1, namedSemaphoreSlim.Item2, peer);
					}).ToList(),
					peerCTS
				);

				torrentSession.PeerTasks.TryAdd(peer, bundle);
			}
		}

		private void StartPeerSiblingTasks(TorrentSession torrentSession)
		{
			var clientIterator = 0;
			foreach (var client in _siblingServiceTaskClients)
			{
				var namedSemaphoreSlim = _siblingSemaphoreSlims[clientIterator++];
				SafeRun(() => client.StartServiceTask(CToken), namedSemaphoreSlim.Item1, namedSemaphoreSlim.Item2);
			}
		}

		private void SubscribeToEvents(TorrentSession torrentSession)
		{
			torrentSession.OnTrackerResponse.Subscribe(peers =>
			{
				var newPeers = new ConcurrentSet<Peer>();
				newPeers.AddRange(torrentSession.AllPeers.Where(p => !peers.Contains(p)));
				torrentSession.AllPeers.AddRange(newPeers);
				_peerManager.HandleTrackerResponse(torrentSession, _peerConnectorLogger, CToken, newPeers);
			});

			torrentSession.OnPeerMessageReceived.Subscribe(peeredMessage =>
			{
				_peerMessageHandler!.HandleMessage(peeredMessage.Item1, peeredMessage.Item2, CToken);
			});

			torrentSession.OnBlockReadFromDisk.Subscribe(peerBlock =>
			{
				_peerManager.TryQueueMessage(peerBlock.Item1, PeerMessage.CreatePieceMessage(peerBlock.Item2));
			});

			torrentSession.OnPeerDisconnected.Subscribe(peer =>
			{
				torrentSession.PendingRequests.TryRemove(peer, out _);
				_pieceManager?.RemovePeerFromRarity(peer.AvailablePieces);
			});
		}

		private Task SafeRun(Func<Task> taskFunc, string taskName, SemaphoreSlim semaphore, Peer? peer = null)
		{
			return Task.Run(async () =>
			{
				await semaphore.WaitAsync();
				try
				{
					var peerLabel = peer == null
						? "s"
						: " " + (peer.PeerID ?? peer.IPEndPoint.ToString());
					_logger.Info($"[SafeRun] Starting {taskName} for peer{peerLabel}");
					await taskFunc();
					_logger.Info($"[SafeRun] Finished {taskName} for peer{peerLabel}");
				}
				catch (Exception ex)
				{
					if (peer != null) _logger.Error($"Peer {peer.PeerID ?? peer.IPEndPoint.ToString()} {taskName} failed", ex);
					else _logger.Error($"{taskName} for all peers failed", ex);
				}
				finally
				{
					semaphore.Release();
				}
			});
		}

		private async Task StopAllPeerServiceTasks(TorrentSession torrentSession)
		{
			foreach (var bundle in torrentSession.PeerTasks.Values)
			{
				await bundle.CTS.CancelAsync();
				await Task.WhenAll(bundle.Tasks);
			}
		}
	}
}