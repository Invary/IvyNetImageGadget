using Invary.Utility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Invary.IvyNetImageGadget
{
	public partial class SettingWindow : Window
	{
		public Setting NewSetting { get; private set; }



		readonly LanguageItem[] _pLanguages = new LanguageItem[]
		{
			new LanguageItem("English", "en")
			, new LanguageItem("Japanese", "ja")
		};


		public SettingWindow()
		{
			InitializeComponent();
			Uty.SwitchResourceDictionaryByLanguage(Resources, Setting.Current.TwoLetterISOLanguageName);


			NewSetting = Setting.Current.Clone();
			DataContext = NewSetting;


			//init language combobox
			{
				//add item to combobox
				foreach (var language in _pLanguages)
				{
					comboboxLanguage.Items.Add(language);
				}
				//default select is 0 = English
				comboboxLanguage.SelectedIndex = 0;

				//select current language
				foreach (LanguageItem language in comboboxLanguage.Items)
				{
					if (language.TwoCharacterName != NewSetting.TwoLetterISOLanguageName)
						continue;
					comboboxLanguage.SelectedItem = language;
					break;
				}
			}




			if (NewSetting.enumDirection == Direction.VERTICAL)
				Vertical.IsChecked = true;
			else
				Horizon.IsChecked = true;

			foreach(var item in NewSetting.Items)
			{
				ImageItemListbox.Items.Add(item);
			}

			ImageItemListbox.MouseDoubleClick += (sender, e)=>
			{
				if (e.ChangedButton != MouseButton.Left)
					return;

				Menu_OnEditItem(this, new RoutedEventArgs());
			};


			labelVersion.Content = $"IvyNetImageGadget {Setting.strVersion}";
		}


		void ApplyToNewSetting()
		{
			if (Vertical.IsChecked == true)
				NewSetting.enumDirection = Direction.VERTICAL;
			if (Horizon.IsChecked == true)
				NewSetting.enumDirection = Direction.HORIZON;

			NewSetting.Items.Clear();
			foreach (Item item in ImageItemListbox.Items)
			{
				NewSetting.Items.Add(item);
			}

			{
				LanguageItem? language = comboboxLanguage.SelectedItem as LanguageItem;
				if (language != null)
					NewSetting.TwoLetterISOLanguageName = language.TwoCharacterName;
			}
		}



		class LanguageItem
		{
			public string Name { get; set; } = "";
			public string TwoCharacterName { get; set; } = "";


			public LanguageItem(string Name, string TwoCharacterName)
			{
				this.Name = Name;
				this.TwoCharacterName = TwoCharacterName;
			}

			public override string ToString()
			{
				return Name;
			}
		}





		string _strSavedSettingPath = "";


		private void OnSaveSetting(object sender, RoutedEventArgs e)
		{
			var dlg = new SaveFileDialog();
			dlg.Title = Uty.ResourceApp("SettingSaveAsDialog_Title");
			dlg.Filter = Uty.ResourceApp("SettingSaveAsDialog_Filter");
			dlg.InitialDirectory = Uty.GetExecutableFolder();
			dlg.OverwritePrompt = true;
			if (dlg.ShowDialog() == false)
				return;

			_strSavedSettingPath = dlg.FileName;

			ApplyToNewSetting();

			NewSetting.SaveSetting(_strSavedSettingPath);
		}



		private void OnSaveShortcut(object sender, RoutedEventArgs e)
		{
			string strSettingPath = _strSavedSettingPath;
			if (string.IsNullOrEmpty(strSettingPath) || File.Exists(strSettingPath) == false)
				strSettingPath = Setting.FilePath;

			var wnd = new InputTextWindow(Uty.ResourceApp("InputTextCreateShortcut_Title"), Uty.ResourceApp("InputTextCreateShortcut_Label") + strSettingPath, "", Setting.Current.TwoLetterISOLanguageName);
			wnd.Owner = this;
			wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;

			wnd.OnPreTextInput += (sender, e) =>
			{
				if (Uty.IsContainInvalidPathChar(e.Text))
					e.Handled = true;
			};
			wnd.OnTextChanged += (sender, e) =>
			{
				if (sender is not TextBox textbox)
					return;

				textbox.Text = Uty.RemoveInvalidPathChar(textbox.Text);
			};

			var ret = wnd.ShowDialog();
			if (ret == false || string.IsNullOrEmpty(wnd.InputText))
				return;

			var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var shortcutpath = Path.Combine(desktop, $"{wnd.InputText}");

			if (shortcutpath.ToLower().EndsWith(".lnk") == false)
				shortcutpath += ".lnk";

			if (File.Exists(shortcutpath))
			{
				//Shortcut file already exists. Overwrite it?
				var dlgret = MessageBox.Show(Uty.ResourceApp("MessageBoxCreateShortcutOverwrite_Text"), Uty.ResourceApp("MessageBoxCreateShortcutOverwrite_Title"), MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
				if (dlgret != MessageBoxResult.OK)
					return;
			}

			Shortcut.Create(shortcutpath, Uty.GetExecutablePath(), $"--setting \"{strSettingPath}\"", "", "", "", "");
		}




		private void Menu_OnAddItem(object sender, RoutedEventArgs e)
		{
			var item = new Item();
			ImageItemListbox.Items.Add(item);
		}


		private void Menu_OnEditItem(object sender, RoutedEventArgs e)
		{
			var item = ImageItemListbox.SelectedItem as Item;
			if (item == null)
				return;

			var wnd = new SettingItemWindow(item);
			wnd.Owner = this;
			wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			var ret = wnd.ShowDialog();
			if (ret == false)
				return;

			ImageItemListbox.Items[ImageItemListbox.SelectedIndex] = wnd.item;
		}


		private void Menu_OnUpItem(object sender, RoutedEventArgs e)
		{
			var item = ImageItemListbox.SelectedItem as Item;
			if (item == null)
				return;

			var index = ImageItemListbox.SelectedIndex;
			if (index <= 0)
				return;

			ImageItemListbox.Items.RemoveAt(index);
			ImageItemListbox.Items.Insert(index - 1, item);
		}


		private void Menu_OnDownItem(object sender, RoutedEventArgs e)
		{
			var item = ImageItemListbox.SelectedItem as Item;
			if (item == null)
				return;

			var index = ImageItemListbox.SelectedIndex;
			if (index < 0 || index == ImageItemListbox.Items.Count - 1)
				return;

			ImageItemListbox.Items.RemoveAt(index);
			ImageItemListbox.Items.Insert(index + 1, item);
		}


		private void Menu_OnRemoveItem(object sender, RoutedEventArgs e)
		{
			var index = ImageItemListbox.SelectedIndex;
			if (index < 0)
				return;

			var ret = MessageBox.Show(Uty.Resource("MessageBoxRemoveItem_Text", Resources), Uty.Resource("MessageBoxRemoveItem_Title", Resources), MessageBoxButton.OKCancel,MessageBoxImage.Question);
			if (ret != MessageBoxResult.OK)
				return;

			ImageItemListbox.Items.RemoveAt(index);
		}












		private void OnOpenProjectURL(object sender, MouseButtonEventArgs e)
		{
			Uty.OpenURL("https://github.com/Invary/IvyNetImageGadget/");
		}










		private void OnOk(object sender, RoutedEventArgs e)
		{
			ApplyToNewSetting();

			// initialize language will be done at MainWindow.BuildAndStart()
			//Uty.SwitchResourceDictionaryByLanguage(Application.Current.Resources);


			DialogResult = true;
			Close();
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
