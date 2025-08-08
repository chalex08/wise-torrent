using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Interfaces
{
	public interface ILogService
	{
		IReadOnlyList<LogEntry> GetLogs();
		void Subscribe();
	}
}
