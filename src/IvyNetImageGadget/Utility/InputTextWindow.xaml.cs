using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Invary.Utility
{

	/// <summary>
	/// 
	/// </summary>
	/// <example>
	///var wnd = new InputTextWindow("Create shortcut file", "Enter a file name for the shortcut file to be created on the desktop.", "");
	///wnd.Owner = this;
	///wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
	///
	///wnd.OnPreTextInput += (sender, e) =>
	///		{
	///	if (Uty.IsContainInvalidPathChar(e.Text))
	///		e.Handled = true;
	///};
	///wnd.OnTextChanged += (sender, e) =>
	///		{
	///	var textbox = sender as TextBox;
	///	if (textbox == null)
	///		return;
	///
	///	textbox.Text = Uty.RemoveInvalidPathChar(textbox.Text);
	///};
	///
	///var ret = wnd.ShowDialog();
	///		if (ret == false)
	///			return;
	/// </example>

	public partial class InputTextWindow : Window
	{
		public string InputText { get; set; } = "";
		public string InputTextLabel { get; set; } = "";

		public TextCompositionEventHandler? OnPreTextInput { get; set; } = null;
		public TextChangedEventHandler? OnTextChanged { get; set; } = null;

		public InputTextWindow(string title, string label, string text, string TwoLetterISOLanguageName)
		{
			InitializeComponent();
			Uty.SwitchResourceDictionaryByLanguage(Resources, TwoLetterISOLanguageName);

			Title = title;
			InputText = text;
			InputTextLabel = label;

			DataContext = this;
		}
		


		private void InputText_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			OnPreTextInput?.Invoke(sender, e);
		}
		private void InputText_TextChanged(object sender, TextChangedEventArgs e)
		{
			OnTextChanged?.Invoke(sender, e);
		}

		private void OnOk(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}
