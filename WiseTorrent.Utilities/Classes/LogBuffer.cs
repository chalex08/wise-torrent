using System;
using System.Collections.Generic;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Classes
{
	internal static class LogBuffer
	{
		private static readonly Queue<LogEntry> _entries = new();
		public static event Action<LogEntry>? LogUpdated;

		public static void Write(LogLevel level, string className, string message)
		{
			var entry = new LogEntry
			{
				Timestamp = DateTime.UtcNow,
				Level = level,
				ClassName = className,
				Message = message
			};

			lock (_entries)
			{
				if (_entries.Count >= SessionConfig.MaxLogEntriesCount)
					_entries.Dequeue(); // remove oldest log

				try
				{
					_entries.Enqueue(entry);
				}
				catch
				{
					var logError = new LogEntry
					{
						Timestamp = DateTime.UtcNow,
						Level = LogLevel.Warn,
						ClassName = "LogBuffer",
						Message = $"Log buffer overloaded, can ignore, Message start: {entry.Message.Take(15)}"
					};

					_entries.Enqueue(logError);
				}
			}

			LogUpdated?.Invoke(entry);
		}

		public static IReadOnlyList<LogEntry> GetAllLogs()
		{
			lock (_entries)
			{
				return _entries.ToArray(); // snapshot to prevent race conditions
			}
		}
	}
}
