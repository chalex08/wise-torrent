namespace WiseTorrent.UI.Services
{
	public class UIStateService
	{
        // Global
        public Dictionary<string, double> TorrentFilepaths { get; set; } = new();

        //Dashboard
		public string? SelectedFile { get; set; }
		public bool DownloadStarted { get; set; }
	}
}
