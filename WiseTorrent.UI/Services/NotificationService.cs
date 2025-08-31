public class NotificationService
{
	public event Action<string, NotificationType>? OnNotify;

	public void Show(string message, NotificationType type = NotificationType.Success)
	{
		OnNotify?.Invoke(message, type);
	}
}

public enum NotificationType
{
	Success,
	Warning,
	Error,
	Info
}
