using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Classes
{
	internal class LogService : ILogService
	{
		private readonly ConcurrentSet<Action<LogEntry>> _listenerActions = new();

		public LogService()
		{
			LogBuffer.LogUpdated += entry =>
			{
				foreach (var listenerAction in _listenerActions) 
					listenerAction(entry);
			};
		}

		public IReadOnlyList<LogEntry> GetLogs() => LogBuffer.GetAllLogs();

		public void Subscribe(Action<LogEntry> listenerAction) => _listenerActions.Add(listenerAction);

		public void Unsubscribe(Action<LogEntry> listenerAction) => _listenerActions.Remove(listenerAction);
		
	}

}
