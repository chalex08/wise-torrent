using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Classes
{
	internal class LogService : ILogService
	{
		public event Action<LogEntry>? OnLogReceived;

		public IReadOnlyList<LogEntry> GetLogs() => LogBuffer.GetAllLogs();

		public void Subscribe()
		{
			LogBuffer.LogUpdated += entry => OnLogReceived?.Invoke(entry);
		}
	}

}
