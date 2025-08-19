using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Tests.UtilitiesTests
{
	public class TestLogger<T> : ILogger<T>
	{
		public void Info(string msg) => Console.WriteLine(msg);
		public void Warn(string msg) => Console.WriteLine(msg);
		public void Error(string msg, Exception? exception = null)
		{
			Console.WriteLine(msg);
			Console.WriteLine(exception);
		}
	}
}
