using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Invary.Utility
{
	internal class Uty
	{
		public static void OpenURL(string url)
		{
			ProcessStartInfo pi = new ProcessStartInfo()
			{
				FileName = url,
				UseShellExecute = true,
			};
			using (Process.Start(pi))
			{
			}
		}


		public static string ResourceApp(string Name)
		{
#if DEBUG
			var ret = Application.Current.Resources[Name] as string;
			if (ret == null)
			{
				Debug.Assert(false);
				return "";
			}
			return ret;
#else
			return "" +Application.Current.Resources[Name] as string;
#endif
		}

		public static string Resource(string Name, ResourceDictionary Resources)
		{
#if DEBUG
			var ret = Resources[Name] as string;
			if (ret == null)
			{
				Debug.Assert(false);
				return "";
			}
			return ret;
#else
			return "" + Resources[Name] as string;
#endif
		}


		public static void SwitchResourceDictionaryByLanguage(ResourceDictionary Resources, string TwoLetterISOLanguageName)
		{
			if (string.IsNullOrEmpty(TwoLetterISOLanguageName) || TwoLetterISOLanguageName.Length != 2)
				TwoLetterISOLanguageName = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

			List<ResourceDictionary> listNutral = new();
			List<ResourceDictionary> list = new();

			var dics = Resources.MergedDictionaries;
			foreach (var dic in dics)
			{
				//find language resouce such as "xxxxx.en.xaml", and adding to list

				var strName = dic.Source.ToString();

				var strBaseName = System.IO.Path.GetFileNameWithoutExtension(strName);
				var strExt = System.IO.Path.GetExtension(strName);

				if (strBaseName != System.IO.Path.GetFileNameWithoutExtension(strBaseName))
					strBaseName = System.IO.Path.GetFileNameWithoutExtension(strBaseName);

				var strNutralName = $"{strBaseName}{strExt}";
				var strCultureName = $"{strBaseName}.{TwoLetterISOLanguageName}{strExt}";

				if (strName == strNutralName)
				{
					listNutral.Add(dic);
					continue;
				}
				if (strName == strCultureName)
				{
					list.Add(dic);
					continue;
				}
			}

			//move nutral resource to tail, this will override current language resource
			foreach (var dic in listNutral)
			{
				Resources.MergedDictionaries.Remove(dic);
				Resources.MergedDictionaries.Add(dic);
			}

			//move to tail, this will activate language
			foreach (var dic in list)
			{
				Resources.MergedDictionaries.Remove(dic);
				Resources.MergedDictionaries.Add(dic);
			}
		}




		/// <summary>
		/// Get executable path
		/// 
		/// ex. @"c:\program files\test\test.exe"
		/// </summary>
		public static string GetExecutablePath()
		{
			var process = Process.GetCurrentProcess();
			if (process.MainModule == null || process.MainModule.FileName == null)
				throw new Exception();

			return process.MainModule.FileName;
		}



		/// <summary>
		/// Get executable folder
		/// 
		/// ex. @"c:\program files\test"
		/// </summary>
		public static string GetExecutableFolder()
		{
			var exe = GetExecutablePath();
			var folder = Path.GetDirectoryName(exe);
			if (folder == null)
				return "";
			return folder;
		}


		public static BitmapImage GetBitmapImageFromStream(Stream strm)
		{
			var image = new BitmapImage();
			image.BeginInit();
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.StreamSource = strm;
			image.EndInit();
			return image;
		}


		static readonly char[] _pInvalidPathChar = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

		public static bool IsContainInvalidPathChar(string text)
		{
			if (string.IsNullOrEmpty(text))
				return false;

			foreach (var item in _pInvalidPathChar)
			{
				if (text.Contains(item))
					return true;
			}

			return false;
		}

		/// <summary>
		/// replace invalid path char to cb
		/// </summary>
		public static string ReplaceInvalidPathChar(string path, char cb)
		{
			if (string.IsNullOrEmpty(path))
				return path;

			foreach (var item in _pInvalidPathChar)
			{
				path = path.Replace(item, cb);
			}

			return path;
		}

		/// <summary>
		/// remove invalid path char
		/// </summary>
		public static string RemoveInvalidPathChar(string path)
		{
			if (string.IsNullOrEmpty(path))
				return path;

			foreach (var item in _pInvalidPathChar)
			{
				path = path.Replace("{item}", "");
			}

			return path;
		}





		public static async Task<string> DownloadTextAsync(string url, double dTimeoutSec, CancellationToken? ct)
		{
			try
			{
				using (var wc = new HttpClient())
				{
					wc.DefaultRequestHeaders.Add(
						"User-Agent",
						"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36");

					//wc.DefaultRequestHeaders.Add("Accept-Language", "ja-JP");

					if (dTimeoutSec > 0)
						wc.Timeout = TimeSpan.FromSeconds(dTimeoutSec);

					if (ct == null)
						return await wc.GetStringAsync(url);
					else
						return await wc.GetStringAsync(url, (CancellationToken)ct);
				}
			}
			catch (Exception)
			{
			}
			return "";
		}



	}
}
