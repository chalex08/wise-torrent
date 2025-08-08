using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Classes
{
	internal class BufferLogger<T> : ILogger<T>
	{
		private readonly string _categoryName;
		public BufferLogger()
		{
			_categoryName = typeof(T).FullName ?? typeof(T).Name;
		}

		public void Info(string message) { Log(LogLevel.Info, message); }

		public void Warn(string message) { Log(LogLevel.Warn, message); }

		public void Error(string message, Exception? exception = null) { Log(LogLevel.Error, message, exception); }

		void Log(LogLevel logLevel, string message, Exception? exception = null)
		{
			if (exception != null)
				message += $" Exception: {exception.Message}";

			LogBuffer.Write(logLevel, $"[{_categoryName}] {message}");
		}
	}
}
