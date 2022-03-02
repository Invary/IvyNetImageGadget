using Invary.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Invary.IvyNetImageGadget
{
    public partial class SettingItemScriptWindow : Window
    {
		public Item item { get; protected set; }

		public SettingItemScriptWindow(Item item)
        {
            InitializeComponent();
			Uty.SwitchResourceDictionaryByLanguage(Resources, Setting.Current.TwoLetterISOLanguageName);

			this.item = item.Clone();
			DataContext = this.item;

			foreach (var text in this.item.Plugin.astrRefAsm)
			{
				listboxRefAsm.Items.Add(text);
			}

		}




		private void OnSampleScript(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(item.Plugin.strCode) == false)
			{
				//messagebox to ask overwrite
				MessageBoxResult dlgret = MessageBox.Show(Uty.Resource("MessageBoxSample_Text", Resources), Uty.Resource("MessageBoxSample_Title", Resources), MessageBoxButton.OKCancel);
				if (dlgret != MessageBoxResult.OK)
					return;
			}

			item.Plugin.strClassName = Uty.Resource("SampleScriptClassName", Resources);
			item.Plugin.strCode = Uty.Resource("SampleScriptCode", Resources);
			item.OnPropertyChanged("Plugin");
		}




		void RefAsm_ListboxToItem()
		{
			int nCount = listboxRefAsm.Items.Count;
			item.Plugin.astrRefAsm = new string[nCount];
			for (int i = 0; i < nCount; i++)
			{
				item.Plugin.astrRefAsm[i] = (string)listboxRefAsm.Items[i];
			}
		}



		/// <summary>
		/// Add new reference assembly dll
		/// </summary>
		private void Menu_OnAddDll(object sender, RoutedEventArgs e)
		{
			var wnd = new InputTextWindow(Uty.ResourceApp("InputTextRefAsmAddNewItem_Title"), Uty.ResourceApp("InputTextRefAsmAddNewItem_Label"), "", Setting.Current.TwoLetterISOLanguageName);
			wnd.Owner = this;
			wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			var ret = wnd.ShowDialog();
			if (ret == false)
				return;
			if (string.IsNullOrEmpty(wnd.InputText))
				return;

			listboxRefAsm.Items.Add(wnd.InputText);
		}

		/// <summary>
		/// Edit reference assembly dll
		/// </summary>
		private void Menu_OnEditDll(object sender, RoutedEventArgs e)
		{
			var select = listboxRefAsm.SelectedIndex;
			if (select < 0)
				return;

			var wnd = new InputTextWindow(Uty.ResourceApp("InputTextRefAsmEditItem_Title"), Uty.ResourceApp("InputTextRefAsmEditItem_Label"), (string)listboxRefAsm.SelectedItem, Setting.Current.TwoLetterISOLanguageName);
			wnd.Owner = this;
			wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			var ret = wnd.ShowDialog();
			if (ret == false)
				return;
			if (string.IsNullOrEmpty(wnd.InputText))
				return;

			listboxRefAsm.Items[select] = wnd.InputText;
		}

		private void Menu_OnRemoveDll(object sender, RoutedEventArgs e)
		{
			var select = listboxRefAsm.SelectedIndex;
			if (select < 0)
				return;
			listboxRefAsm.Items.RemoveAt(select);
		}




		private void OnOk(object sender, RoutedEventArgs e)
		{
			RefAsm_ListboxToItem();

			DialogResult = true;
			Close();
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
