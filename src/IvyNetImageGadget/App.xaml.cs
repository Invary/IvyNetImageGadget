using Invary.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Invary.IvyNetImageGadget
{
	public partial class App : Application
	{
		public new int Run()
		{
			Uty.SwitchResourceDictionaryByLanguage(Resources, "");

			//parse command line options
			{
				var ret = CheckAndApplyCommandLineArgs();
				if (ret == false)
				{
					var message = Uty.ResourceApp("InvalidCommandlineOption") + "\n\n";

					var options = GetCommandLineOptions();
					foreach (var option in options)
					{
						message += option.Help += "\n";
					}

					MessageBox.Show(message, Uty.ResourceApp("InvalidCommandlineOption_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
					return -1;
				}
			}

			return base.Run();
		}


		//TODO: create and set application ICON






		static bool CheckAndApplyCommandLineArgs()
		{
			var options = GetCommandLineOptions();

			var ret = CommandLineOption.AnalyzeCommandLine(options);
			if (ret == false)
				return false;

			foreach (var option in options)
			{
				option.Apply();
			}

			return true;
		}



		static List<CommandLineOption> GetCommandLineOptions()
		{
			List<CommandLineOption> options = new List<CommandLineOption>();

			options.Add(new CommandLineOption("--setting", 1, true)
			{
				Help = "--setting {file path}",
				OnApply = (option) =>
				{
					//change setting
					Setting.FilePath = option.Values[0];
				}
			});


			return options;
		}








	}
}
