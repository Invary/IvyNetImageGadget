using Invary.Utility;
using System.Windows;
using System.Windows.Controls;

namespace Invary.IvyNetImageGadget
{
	public partial class ItemTestWindow : Window
	{
		readonly ItemDownloader? _downloader = null;

		public string LogMessage { get; set; } = "";


		public ItemTestWindow(Item? item)
		{
			InitializeComponent();
			Uty.SwitchResourceDictionaryByLanguage(Resources, Setting.Current.TwoLetterISOLanguageName);

			DataContext = this;

			if (item != null)
			{
				_downloader = new ItemDownloader(item, imageDownloaded);

				_downloader.OnProgress += (sender, e) =>
				{
					LogMessage += e.Message + "\r\n";
				};

				_downloader.StartDownload();
			}

			Closing += delegate
			{
				if (_downloader != null)
					_downloader.Dispose();
			};
		}
	}
}
