using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Plugin
{
	public delegate void ClickEvent(int x, int y);


	public interface IHost
	{
//		Form wndMain { get; }

		event ClickEvent? Click;
	}


	public interface IPlugin
	{
		//string Name {get;}
		//string Description { get;}

		void OnCreate(IHost host);

		DrawingImage? OnImageDownloadCompleted(WriteableBitmap bmp);
		string OnBeforeDownload(string strURL);

		void OnClose();
	}
}
