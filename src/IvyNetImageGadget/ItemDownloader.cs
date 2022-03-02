using Invary.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Invary.IvyNetImageGadget
{
	class ProgressEventArgs : EventArgs
	{
		public int Min { get; set; } = 0;
		public int Max { get; set; } = 100;
		public int Progress { get; set; } = 0;

		public string Message { get; set; } = "";

		public ProgressEventArgs()
		{
		}

		public ProgressEventArgs(string Message)
		{
			this.Message = Message;
		}

	}

	internal class ItemDownloader : IDisposable
	{

		public Item? item { get; set; }
		public Image? image { get; set; }

		public EventHandler<ProgressEventArgs>? OnProgress;
		public EventHandler? OnImageDownloadCompleted;


		readonly List<DownloadStreamTask> _listDown = new List<DownloadStreamTask>();



		public ItemDownloader(Item item, Image image)
		{
			this.item = item;
			this.image = image;
		}



		public void Dispose()
		{
			ReleaseThread(true);
		}




		public void StartDownload()
		{
			OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_StartDownload_Start")));

			ReleaseThread();

			if (item == null || image == null)
			{
				//End: Error: item or image is null
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_StartDownload_End_Error_ItemImage_null")));
				return;
			}

			if (item.strPreURL == "")
			{
				//Start: Image download
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_StartDownload_StartImageDownload")));

				string strURL = item.strURL;

				//TODO: resource
				OnProgress?.Invoke(this, new ProgressEventArgs($"  URL: {strURL}"));
				{
					//run plugin script
					string tmp = item.Plugin.OnBeforeDownload(strURL);
					if (string.IsNullOrEmpty(tmp) == false)
					{
						strURL = tmp;

						//$"  URL is modified by script: {strURL}"
						OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_StartDownload_URLmodifiedByScript") + strURL));
					}
				}
				//TODO: resource
				OnProgress?.Invoke(this, new ProgressEventArgs($"  Referer: {item.strRefererURL}"));

				item.OnStartDownload();

				DownloadStreamTask down = new DownloadStreamTask();
				down.Url = strURL;
				down.Referer = item.strRefererURL;
				down.OnDownloadCompleted += OnDownloadCompleted;
				down.Start();
				_listDown.Add(down);
			}
			else
			{
				//start pre download before image download
				item.OnStartDownload();

				//Start: Pre download
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_StartDownload_StartPreDownload")));
				//TODO: resource
				OnProgress?.Invoke(this, new ProgressEventArgs($"  PreURL: {item.strPreURL}"));
				OnProgress?.Invoke(this, new ProgressEventArgs($"  PreReferer: {item.strPreRefererURL}"));

				DownloadStreamTask down = new DownloadStreamTask();
				down.Url = item.strPreURL;
				down.Referer = item.strPreRefererURL;
				down.OnDownloadCompleted += OnPreDownloadCompleted;
				down.Start();
				_listDown.Add(down);
			}
		}




		void ReleaseThread(bool bForce = false)
		{
			bool bDisposed = false;

			for (int i = _listDown.Count - 1; i >= 0; i--)
			{
				var down = _listDown[i];

				try
				{
					if (down.IsRunning && bForce == false)
						continue;

					//"Release"
					OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_ReleaseThread_Release")));

					down.Dispose();
					_listDown.RemoveAt(i);
					bDisposed = true;
				}
				catch (Exception)
				{
				}
			}
			if (bDisposed)
				GC.Collect();
		}





		void OnDownloadCompleted(object? sender, DownEventArgs e)
		{
			//"On download completed"
			OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_OnDownloadCompleted")));

			DownloadStreamTask? down = (DownloadStreamTask?)sender;
			if (down == null)
			{
				//"  Error: task is null"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_ErrorTaskNull")));
				return;
			}

			if (e.Succeeded == false || e.stream == null)
			{
				//"  Error: Failed to download"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_ErrorFailedDownload")));
				return;
			}

			if (item == null || image == null)
			{
				//"  Error: item or image is null"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_ErrorItemOrImageNull")));
				return;
			}


			//image download completed
			item.OnEndDownload();


			{
				e.stream.Seek(0, SeekOrigin.Begin);
				var bmp = Uty.GetBitmapImageFromStream(e.stream);
				bmp.Freeze();

				//run plugin script
				try
				{
					image.Dispatcher.BeginInvoke(new Action(() =>
					{
						var drawing = item.Plugin.OnImageDownloadCompleted(new WriteableBitmap(bmp));
						if (drawing != null)
						{
							//"  Image is modified by plugin"
							//"  Set image"
							OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_ImageIsModifiedByPlugin")));
							OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_SetImage")));

							//update image
							image.Source = drawing;
							item.LastHeight = drawing.Height;
							item.LastWidth = drawing.Width;
						}
						else
						{
							//"  Set image(1)"
							OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_SetImage1")));

							image.Source = bmp;
							item.LastHeight = bmp.Height;
							item.LastWidth = bmp.Width;
						}
						image.ToolTip = CreateToolTipString();
						OnImageDownloadCompleted?.Invoke(this, new EventArgs());
					}));
					return;
				}
				catch (Exception ex)
				{
					//TODO: resource
					OnProgress?.Invoke(this, new ProgressEventArgs($"  Exception: {ex.Message}"));
				}

				//"  Set image(2)"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnDownloadCompleted_SetImage2")));

				//update image
				image.Dispatcher.BeginInvoke(new Action(() =>
				{
					image.Source = bmp;
					image.ToolTip = CreateToolTipString();
					item.LastHeight = bmp.Height;
					item.LastWidth = bmp.Width;
					OnImageDownloadCompleted?.Invoke(this, new EventArgs());
				}));
			}
		}

		string CreateToolTipString()
		{
			return Uty.ResourceApp("tooltipImage") + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
		}


		void OnPreDownloadCompleted(object? sender, DownEventArgs e)
		{
			//"On pre download completed"
			//"Start image download"
			OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnPreDownloadCompleted_OnPreDownloadCompleted")));
			OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnPreDownloadCompleted_StartImageDownload")));

			DownloadStreamTask? down = (DownloadStreamTask?)sender;
			if (down == null)
			{
				//"  Error: task is null"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnPreDownloadCompleted_ErrorTaskNull")));
				return;
			}

			if (e.Succeeded == false || e.stream == null)
			{
				//"  Error: Failed to download"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnPreDownloadCompleted_ErrorFailedDownload")));
				return;
			}

			if (item == null || image == null)
			{
				//"  Error: item or image is null"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnPreDownloadCompleted_ErrorItemOrImageNull")));
				return;
			}

			string strURL = item.strURL;

			//TODO: resource
			OnProgress?.Invoke(this, new ProgressEventArgs($"  URL: {strURL}"));

			//if regex exists, modify strURL
			if (string.IsNullOrEmpty(item.strPreRegEx) == false)
			{
				//"  Use pre regex: {item.strPreRegEx}"
				OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnPreDownloadCompleted_UsePreRegEx") + item.strPreRegEx));
				try
				{
					//auto encode detect
					using (StreamReader sr = new StreamReader(e.stream, true))
					{
						string html = sr.ReadToEnd();

						Regex re;

						if (item.bPreRegExSingleLine)
							re = new Regex(item.strPreRegEx, RegexOptions.IgnoreCase | RegexOptions.Singleline);
						else
							re = new Regex(item.strPreRegEx, RegexOptions.IgnoreCase);

						MatchCollection mc = re.Matches(html);

						//TODO: resource
						OnProgress?.Invoke(this, new ProgressEventArgs($"  Regex: count={mc.Count}"));

						for (int i = 0; i < mc.Count; i++)
						{
							Match m = mc[i];

							if (m.Success == false)
								continue;

							string ss = m.Groups[1].Value;
							strURL = strURL.Replace(@"{PreDownloadParam" + i + "}", ss);
							//ex. strURL = strURL.Replace(@"{PreDownloadParam0}", ss);
						}

						//"  URL is modified by regex: {strURL}"
						OnProgress?.Invoke(this, new ProgressEventArgs(Uty.ResourceApp("OnProgressItemDownload_OnPreDownloadCompleted_URLmodifiedByRegex") + strURL));
					}
				}
				catch (Exception ex)
				{
					//TODO: resource
					OnProgress?.Invoke(this, new ProgressEventArgs($"  Exception: {ex.Message}"));
				}
			}

			item.OnStartDownload();

			//TODO: resource
			OnProgress?.Invoke(this, new ProgressEventArgs($"  Referer: {item.strRefererURL}"));

			down = new DownloadStreamTask();
			down.Url = strURL;
			down.Referer = item.strRefererURL;
			down.OnDownloadCompleted += OnDownloadCompleted;
			down.Start();
			_listDown.Add(down);
		}
	}
}
