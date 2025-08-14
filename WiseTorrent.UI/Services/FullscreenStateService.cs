namespace WiseTorrent.UI.Services
{
	public class FullscreenStateService(bool initialState = false)
	{
		public event Action? OnChange;

		public bool IsFullscreen { get; private set; } = initialState;

		public void Toggle()
		{
			IsFullscreen = !IsFullscreen;
			OnChange?.Invoke();
		}
	}
}