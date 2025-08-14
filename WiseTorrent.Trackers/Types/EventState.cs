namespace WiseTorrent.Trackers.Types
{
	public enum EventState
	{
		None = 0,
		Completed = 1,
		Started = 2,
		Stopped = 3
	}

	public static class EventStateExtensions
	{
		public static string ToURLString(this EventState? state)
		{
			return state switch
			{
				EventState.Completed => "completed",
				EventState.Started => "started",
				EventState.Stopped => "stopped",
				_ => String.Empty
			};
		}
	}
}
