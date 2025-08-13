using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Classes
{
	internal static class LogBuffer
	{
		private static readonly List<LogEntry> _entries = new();
		public static event Action<LogEntry>? LogUpdated;

		public static void Write(LogLevel level, string className, string message)
		{
			var entry = new LogEntry { Level = level, ClassName = className, Message = message };
			_entries.Add(entry);
			LogUpdated?.Invoke(entry);
		}

		public static IReadOnlyList<LogEntry> GetAllLogs() => _entries.AsReadOnly();
	}
}
