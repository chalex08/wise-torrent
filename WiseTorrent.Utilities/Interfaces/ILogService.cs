using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Interfaces
{
	public interface ILogService
	{
		IReadOnlyList<LogEntry> GetLogs();
		void Subscribe(Action<LogEntry> listenerAction);
		void Unsubscribe(Action<LogEntry> listenerAction);
	}
}
