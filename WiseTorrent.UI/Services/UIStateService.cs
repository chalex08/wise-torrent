namespace WiseTorrent.UI.Services
{
	public class UIStateService
	{
        // Global
        public List<string> TorrentFilepaths { get; set; } = new();

		//Dashboard
		public string? SelectedFile { get; set; }
		public bool DownloadStarted { get; set; }
	}
}
