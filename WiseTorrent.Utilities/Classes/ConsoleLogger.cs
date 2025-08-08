using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Utilities.Classes
{
	internal class ConsoleLogger : ILogger
	{
		public void Info(string message) { Log("Info: ", message); }

		public void Warn(string message) { Log("Warn: ", message); }

		public void Error(string message) { Log("Error: ", message); }

		void Log(string prefix, string message) { Console.WriteLine(prefix + message); }
	}
}
