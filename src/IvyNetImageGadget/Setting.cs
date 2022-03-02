using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Invary.IvyNetImageGadget
{
	[Serializable]
	public class Item : INotifyPropertyChanged
	{
		public string Name { set; get; } = "";


		/// <summary>
		/// Url for opening by brawser
		/// </summary>
		public string strUserURL { set; get; } = "";


		/// <summary>
		/// replace "{PreDownloadParam0}" in strURL to captured text by regex
		/// </summary>
		public string strURL { set; get; } = "";

		public string strRefererURL { set; get; } = "";

		public ScriptPlugin Plugin { set; get; } = new ScriptPlugin();



		/// <summary>
		/// url for two-step download
		/// url target is only html
		/// </summary>
		public string strPreURL { set; get; } = "";

		public string strPreRefererURL { set; get; } = "";

		/// <summary>
		/// regex string used by two-step download
		/// () is captured. and replace {PreDownloadParam0} in image URL.
		/// (imageOut/[\d/_]+)\.
		/// </summary>
		public string strPreRegEx { set; get; } = "";


		/// <summary>
		/// whether regex treat as single line
		/// </summary>
		public bool bPreRegExSingleLine { set; get; } = false;


		public double LastWidth { set; get; } = 0;
		public double LastHeight { set; get; } = 0;







		/// <summary>
		/// image update interval
		/// 
		/// 0 = no refresh
		/// </summary>
		public int nRefreshSecond { set; get; } = 0;




		[XmlIgnore]
		public DateTime dtLastDownloaded
		{
			get {return _dtLastDownloaded;}
		}
		DateTime _dtLastDownloaded = DateTime.MinValue;
		DateTime _dtDownloadStart = DateTime.MinValue;



		public event PropertyChangedEventHandler? PropertyChanged;

		public void OnPropertyChanged(string PropertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
		}




		public void OnStartDownload()
		{
			_dtDownloadStart = DateTime.Now;
		}


		public void OnEndDownload()
		{
			_dtLastDownloaded = DateTime.Now;
		}


		[XmlIgnore]
		public bool bNeedDownload
		{
			get
			{
				if (_dtDownloadStart == DateTime.MinValue)
					return true;

				TimeSpan spanLastDownloaded = DateTime.Now - _dtLastDownloaded;
				TimeSpan spanDownloadStart = DateTime.Now - _dtDownloadStart;

				if (nRefreshSecond <= 0)
				{
					//no need to update
					if (_dtLastDownloaded != DateTime.MinValue)
						return false;

					//start download, but failed?? download retry
					if (spanDownloadStart.TotalSeconds > 120)
						return true;
					return false;
				}

				if (spanLastDownloaded.TotalSeconds > nRefreshSecond
					&& spanDownloadStart.TotalSeconds > nRefreshSecond)
					return true;
				return false;
			}
		}




		public Item Clone()
		{
			var serializer = new XmlSerializer(typeof(Item));
			using (var ms = new MemoryStream())
			{
				serializer.Serialize(ms, this);

				ms.Seek(0, SeekOrigin.Begin);
				var load = (Item?)serializer.Deserialize(ms);
				if (load == null)
					throw new Exception();

				return load;
			}
		}


		public override string ToString()
		{
			if (string.IsNullOrEmpty(Name) == false)
				return Name;

			return $"(item) {strURL}";
		}
	}

	public enum Direction
	{
		HORIZON,
		VERTICAL
	}













	[Serializable]
	public class Setting
	{
		[XmlIgnore]
		public static Setting Current { get; set; } = new Setting();

		[XmlIgnore]
		public static string FilePath { get; set; } = "";


		[XmlIgnore]
		public static int nVersion { get; } = 100;

		[XmlIgnore]
		public static string strVersion { get; } = $"Ver{nVersion}";


		[XmlIgnore]
		public static string strProductGuid { get; } = "{68156996-1F11-4DB6-886C-AB426A1205BE}";

		[XmlIgnore]
		public static string strUpdateCheckUrl { get; } = @"https://raw.githubusercontent.com/Invary/Status/main/status.json";


		[XmlIgnore]
		public static string strDownloadUrl { get; } = @"https://github.com/Invary/IvyNetImageGadget/Releases";








		public Setting()
		{
			TwoLetterISOLanguageName = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
		}


		public bool bCheckAutoUpdate { set; get; } = true;
		public DateTime dtLastCheckAutoUpdate { set; get; } = DateTime.MinValue;




		public List<Item> Items { set; get; } = new List<Item>();

		public Direction enumDirection { set; get; } =  Direction.VERTICAL;



		public Point ptLocation { set; get; } = new Point(0, 0);


		public Size Size { set; get; } = new Size(100, 100);

		public bool bLockUIWhenStartApp { set; get; } = false;





		public string TwoLetterISOLanguageName { set; get; } = "";


		public Setting Clone()
		{
			var serializer = new XmlSerializer(typeof(Setting));
			using (var ms = new MemoryStream())
			{
				serializer.Serialize(ms, this);

				ms.Seek(0, SeekOrigin.Begin);
				var load = (Setting?)serializer.Deserialize(ms);
				if (load == null)
					throw new Exception();

				return load;
			}
		}


		public bool SaveSetting(string strFilePath = "")
		{
			if (string.IsNullOrEmpty(strFilePath))
				strFilePath = FilePath;

			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Setting));
				using (FileStream fs = new FileStream(strFilePath, FileMode.Create))
				{
					serializer.Serialize(fs, this);
					fs.Close();
				}
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}



		public static bool LoadSetting(string strFilePath)
		{
			if (string.IsNullOrEmpty(strFilePath))
				return false;
			if (File.Exists(strFilePath) == false)
				return false;

			FilePath = strFilePath;

			try
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Setting));

				using (FileStream fs = new FileStream(strFilePath, FileMode.Open))
				{
					var load = (Setting?)serializer.Deserialize(fs);
					fs.Close();

					if (load == null)
						return false;

					Current = load;
				}

				if (string.IsNullOrEmpty(Current.TwoLetterISOLanguageName) || Current.TwoLetterISOLanguageName.Length != 2)
					Current.TwoLetterISOLanguageName = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;


				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
