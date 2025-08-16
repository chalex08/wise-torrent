namespace WiseTorrent.Utilities.Types
{
	public class TrackerResponse(int? interval = null, List<Peer>? peers = null)
	{
		public int? Interval = interval;
		public List<Peer>? Peers = peers;
		public string? FailureReason;
		public string? WarningMessage;
		public int? Complete;
		public int? Incomplete;
	}
}
