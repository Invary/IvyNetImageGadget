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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Invary.Utility
{
	public partial class DonationControl : UserControl
	{
		public DonationControl()
		{
			InitializeComponent();
			//TODO: donation language is only auto detecting
			Uty.SwitchResourceDictionaryByLanguage(Resources, "");
		}

		private void OnClick_Kifi(object sender, MouseButtonEventArgs e)
		{
			Uty.OpenURL("https://ko-fi.com/E1E7AC6QH");
		}
		private void OnClick_QR1(object sender, MouseButtonEventArgs e)
		{
			ClipboardUty.SetText("0xCbd4355d13CEA25D87F324E9f35A075adce6507c");
		}
		private void OnClick_QR2(object sender, MouseButtonEventArgs e)
		{
			ClipboardUty.SetText("1FvzxYriyNDdeA12eaUGXTGSJxkzpQdxPd");
		}


	}
}
