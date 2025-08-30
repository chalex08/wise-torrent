using Microsoft.Win32;

namespace WiseTorrent.UI.Services
{
	internal class FilePickerService : IFilePickerService
	{
		public string? PickFile()
		{
			var dialog = new OpenFileDialog
			{
				Title = "Select a torrent file to download",
				Filter = "Torrent files (*.torrent)|*.torrent",
				Multiselect = false
			};

			return dialog.ShowDialog() == true ? dialog.FileName : null;
		}
	}
}
