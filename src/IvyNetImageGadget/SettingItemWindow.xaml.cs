using Invary.Utility;
using System;
using System.Collections.Generic;
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

namespace Invary.IvyNetImageGadget
{
	public partial class SettingItemWindow : Window
	{
		public Item item { get; protected set; }




		public SettingItemWindow(Item item)
		{
			InitializeComponent();
			Uty.SwitchResourceDictionaryByLanguage(Resources, Setting.Current.TwoLetterISOLanguageName);

			this.item = item.Clone();
			DataContext = this.item;
		}




		private void OnTest(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(item.Plugin.strCode) == false)
			{
				string message;
				var asm = ScriptPlugin.Compile(item.Plugin.strCode, item.Plugin.strClassName, item.Plugin.astrRefAsm, out message);
				if (asm == null)
				{
					//compile error

					var wnd = new ItemTestWindow(null);
					wnd.LogMessage = message;
					wnd.Owner = this;
					wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					wnd.ShowDialog();
					return;
				}
				item.Plugin.Initialize();
			}

			{
				var wnd = new ItemTestWindow(item);
				wnd.Owner = this;
				wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				wnd.ShowDialog();
			}
		}








		private void OnEditScript(object sender, RoutedEventArgs e)
		{
			var wnd = new SettingItemScriptWindow(item);
			wnd.Owner = this;
			wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			var ret = wnd.ShowDialog();
			if (ret == false)
				return;

			item = wnd.item;
			DataContext = item;
		}




		private void OnOk(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

	}
}
