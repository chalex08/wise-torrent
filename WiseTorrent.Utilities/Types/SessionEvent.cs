namespace WiseTorrent.Utilities.Types
{
	public class SessionEvent<T>
	{
		private readonly ConcurrentSet<Action<T>> _listenerActions = new();

		public void Subscribe(Action<T> listenerAction) => _listenerActions.Add(listenerAction);

		public void Unsubscribe(Action<T> listenerAction) => _listenerActions.Remove(listenerAction);

		public void NotifyListeners(T parameter)
		{
			foreach (var listenerAction in _listenerActions.ToList())
				listenerAction(parameter);
		}
	}
}
