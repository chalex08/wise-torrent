using System.Collections;
using System.Collections.Concurrent;

namespace WiseTorrent.Utilities.Types
{
	public class ConcurrentSet<T> : IEnumerable<T> where T : notnull
	{
		private readonly ConcurrentDictionary<T, bool> _set = new();

		public bool Add(T param) => _set.TryAdd(param, true);
		public void AddRange(IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				_set.TryAdd(item, true);
			}
		}
		public bool Remove(T param) => _set.TryRemove(param, out _);
		public bool Contains(T param) => _set.ContainsKey(param);
		public int Count => _set.Count;
		public IEnumerable<T> All => _set.Keys;

		public IEnumerator<T> GetEnumerator() => _set.Keys.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
