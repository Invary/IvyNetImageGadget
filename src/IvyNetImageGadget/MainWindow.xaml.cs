using Invary.Utility;
using Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Timer = System.Timers.Timer;

namespace Invary.IvyNetImageGadget
{
	public partial class MainWindow : Window
	{
		readonly Timer _timer = new Timer();
		readonly List<Image> _listPictureBox = new List<Image>();

		bool _bNoSaveSettingWhenClose = false;



		public MainWindow()
		{

			{
				string strSettingFile = Setting.FilePath;

				if (string.IsNullOrEmpty(strSettingFile))
				{
					strSettingFile = Path.Combine(Uty.GetExecutableFolder(), Path.GetFileNameWithoutExtension(Uty.GetExecutablePath()));
					strSettingFile += ".xml";
				}

				bool ret = Setting.LoadSetting(strSettingFile);

				if (ret == false)
				{
					MessageBoxResult dlgret = MessageBox.Show(Uty.ResourceApp("MessageBoxInitSetting_Text"), Uty.ResourceApp("MessageBoxInitSetting_Title"), MessageBoxButton.OKCancel);
					if (dlgret != MessageBoxResult.OK)
					{
						_bNoSaveSettingWhenClose = true;
						Close();
						return;
					}
					Setting.FilePath = strSettingFile;
				}
			}






			InitializeComponent();
			// initialize language will be done at BuildAndStart()
			//Uty.SwitchResourceDictionaryByLanguage(Resources, Setting.Current.TwoLetterISOLanguageName);


			SizeChanged += delegate
			{
				AdjustGridSizeToImage(false);
			};


			MouseLeftButtonDown += (sender, e) =>
			{
				if (e.ButtonState != MouseButtonState.Pressed)
					return;

				//move window, only resizable mode
				if (ResizeMode != ResizeMode.NoResize)
					DragMove();
			};


			Closing += delegate
			{
				Destroy();

				//if (WindowState == FormWindowState.Normal)
				{
					Setting.Current.ptLocation = new Point(Left, Top);
					Setting.Current.Size = new Size(Width, Height);
				}

				if(_bNoSaveSettingWhenClose == false)
					Setting.Current.SaveSetting();
			};

			ContextMenuOpening += Menu_Opening;

			Loaded += Form1_Shown;


			if (Setting.Current.bLockUIWhenStartApp)
			{
				ResizeMode = ResizeMode.NoResize;
			}
		}





		void BuildAndStart()
		{
			Uty.SwitchResourceDictionaryByLanguage(Application.Current.Resources, Setting.Current.TwoLetterISOLanguageName);
			Uty.SwitchResourceDictionaryByLanguage(Resources, Setting.Current.TwoLetterISOLanguageName);

			//initialize plugin
			foreach (Item item in Setting.Current.Items)
			{
				item.Plugin.Initialize();
			}



			//if ctrl pressed, not restore position.
			if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down
				|| (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == KeyStates.Down)
			{
			}
			else
			{
				WindowStartupLocation = WindowStartupLocation.Manual;
				Left = Setting.Current.ptLocation.X;
				Top = Setting.Current.ptLocation.Y;
				Width = Setting.Current.Size.Width;
				Height = Setting.Current.Size.Height;
			}



			for (int i = 0; i < Setting.Current.Items.Count; i++)
			{
				Image image = new Image();

				if (Setting.Current.enumDirection == Direction.VERTICAL)
				{
					var row = new RowDefinition();
					gridMain.RowDefinitions.Add(row);
					Grid.SetRow(image, i);
				}
				else
				{
					var column = new ColumnDefinition();
					gridMain.ColumnDefinitions.Add(column);
					Grid.SetColumn(image, i);
				}

				gridMain.Children.Add(image);
				_listPictureBox.Add(image);
				var down = new ItemDownloader(Setting.Current.Items[i], image);
				_listDownloader.Add(down);
				down.OnImageDownloadCompleted += (sender, e) =>
				{
					AdjustGridSizeToImage(false);
				};

				StartDownload(i);
			}
			AdjustGridSizeToImage(false);

			_timer.Start();
		}



		void Destroy()
		{
			_timer.Stop();

			foreach (var item in _listDownloader)
			{
				item.Dispose();
			}
			_listDownloader.Clear();

			_listPictureBox.Clear();

			gridMain.Children.Clear();
			gridMain.RowDefinitions.Clear();
			gridMain.ColumnDefinitions.Clear();
		}














		private void OnOpenDownloadURL(object sender, MouseButtonEventArgs e)
		{
			Uty.OpenURL(Setting.strDownloadUrl);
		}






		int _nLastMenuItem = -1;


		private void Menu_Opening(object sender, ContextMenuEventArgs e)
		{
			//memory item index to _nLastMenuItem
			{
				_nLastMenuItem = -1;
				if (e.Source is Image)
				{
					for (int i = 0; i < _listPictureBox.Count; i++)
					{
						if (_listPictureBox[i] != e.Source)
							continue;

						_nLastMenuItem = i;
						break;
					}
				}
			}

			foreach (object menuitem in ContextMenu.Items)
			{
				if (menuitem is not MenuItem item)
					continue;

				if (item.Name == "OpenUserURL")
				{
					item.IsEnabled = false;
					item.ToolTip = null;
					if (_nLastMenuItem >= 0)
					{
						if (string.IsNullOrEmpty(Setting.Current.Items[_nLastMenuItem].strUserURL) == false)
						{
							item.ToolTip = Setting.Current.Items[_nLastMenuItem].strUserURL;
							item.IsEnabled = true;
						}
					}
				}

				if (item.Name == "UILock")
					item.IsEnabled = (ResizeMode != ResizeMode.NoResize);
				if (item.Name == "UIUnlock")
					item.IsEnabled = (ResizeMode == ResizeMode.NoResize);
			}
		}


		private void Menu_OnOpenUserURL(object sender, RoutedEventArgs e)
		{
			if (_nLastMenuItem < 0 || _nLastMenuItem >= Setting.Current.Items.Count)
				return;

			var url = Setting.Current.Items[_nLastMenuItem].strUserURL;
			if (string.IsNullOrEmpty(url))
				return;

			Uty.OpenURL(url);
		}


		private void Menu_OnLock(object sender, RoutedEventArgs e)
		{
			ResizeMode = ResizeMode.NoResize;
		}


		private void Menu_OnUnlock(object sender, RoutedEventArgs e)
		{
			ResizeMode = ResizeMode.CanResize;
		}



		void AdjustGridSizeToImage(bool bResizeWindow)
		{
			if (Setting.Current.Items.Count != _listPictureBox.Count)
				return;
			if (Setting.Current.Items.Count != gridMain.RowDefinitions.Count && Setting.Current.Items.Count != gridMain.ColumnDefinitions.Count)
				return;

			var wndWidth = Width;
			var wndHeight = Height;

			double sumWidth = 0;
			double sumHeight = 0;

			for (int i = 0; i < Setting.Current.Items.Count; i++)
			{
				var imgWidth = Setting.Current.Items[i].LastWidth;
				var imgHeight = Setting.Current.Items[i].LastHeight;
				if (imgWidth < 10 || double.IsNaN(imgWidth))
					imgWidth = 10;
				if (imgHeight < 10 || double.IsNaN(imgHeight))
					imgHeight = 10;
				var scale = imgWidth / imgHeight;

				if (Setting.Current.enumDirection == Direction.VERTICAL)
				{
					//row
					double h;
					if (imgHeight <= 10)
						h = 10;
					else
						h = wndWidth / scale;
					var row = gridMain.RowDefinitions[i];
					row.Height = new GridLength(h);
					sumHeight += h;
				}
				else
				{
					double w;
					if (imgWidth <= 10)
						w = 10;
					else
						w = wndHeight / scale;
					var column = gridMain.ColumnDefinitions[i];
					column.Width = new GridLength(w);
					sumWidth += w;
				}
			}

			if (bResizeWindow)
			{
				if (sumWidth > 0)
					Width = sumWidth;
				if (sumHeight > 0)
					Height = sumHeight;
			}
		}




		private void Menu_OnAdjust(object sender, RoutedEventArgs e)
		{
			AdjustGridSizeToImage(true);
		}




		private void Menu_OnSetting(object sender, RoutedEventArgs e)
		{
			var wnd = new SettingWindow();
			wnd.Owner = this;
			var ret = wnd.ShowDialog();
			if (ret == false)
				return;

			var setting = wnd.NewSetting;
			if (setting == null)
				return;

			Setting.Current = setting;
			{
				Setting.Current.ptLocation = new Point(Left, Top);
				Setting.Current.Size = new Size(Width, Height);
			}

			//rebuild window
			Destroy();
			BuildAndStart();
		}


		private void Menu_OnQuit(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Menu_OnQuitWithoutSave(object sender, RoutedEventArgs e)
		{
			_bNoSaveSettingWhenClose = true;
			Close();
		}







		private void Form1_Shown(object sender1, EventArgs e1)
		{
			_timer.Interval = 1000;
			_timer.Elapsed += delegate
			{
				for (int i = 0; i < Setting.Current.Items.Count; i++)
				{
					if (Setting.Current.Items[i].bNeedDownload == false)
						continue;

					StartDownload(i);
				}
			};

			BuildAndStart();


			if (Setting.Current.bCheckAutoUpdate)
			{
				TimeSpan span = DateTime.Now - Setting.Current.dtLastCheckAutoUpdate;
				if (span.TotalDays >= 0)
				{
					//Update the date, regardless of the result.
					Setting.Current.dtLastCheckAutoUpdate = DateTime.Now;

					UpdateStatus.CheckUpdate((sender, e) =>
					{
						if (e.IsNewVersiionExists)
						{
							try
							{
								imageInformation.Dispatcher.BeginInvoke(new Action(() =>
								{
									imageInformation.Visibility = Visibility.Visible;
								}));
							}
							catch (Exception)
							{
							}
						}
					}, 30, null);
				}
			}
		}

		readonly List<ItemDownloader> _listDownloader = new List<ItemDownloader>();




		void StartDownload(int nIndex)
		{
			_listDownloader[nIndex].StartDownload();
		}

	}
}
