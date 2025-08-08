using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Interfaces
{
	public interface ILogService
	{
		event Action<LogEntry>? OnLogReceived;
		IReadOnlyList<LogEntry> GetLogs();
		void Subscribe();
	}
}
