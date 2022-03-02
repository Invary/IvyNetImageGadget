using Invary.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Invary.IvyNetImageGadget
{
	public class ScriptPlugin
	{
		public string strCode { set; get; } = "";


		public string strClassName { set; get; } = "";



		public string[] astrRefAsm { set; get; } = Array.Empty<string>();






		Assembly? _asm = null;

		IPlugin? _iScript = null;


		class Host : IHost
		{
			public event ClickEvent? Click;
		}


		public string OnBeforeDownload(string strURL)
		{
			if (_iScript == null)
				return "";

			return _iScript.OnBeforeDownload(strURL);
		}


		public DrawingImage? OnImageDownloadCompleted(WriteableBitmap bmp)
		{
			if (_iScript == null)
				return null;

			return _iScript.OnImageDownloadCompleted(bmp);
		}


		static MetadataReference GetMetadataReference(Type type)
		{
			return MetadataReference.CreateFromFile(type.GetTypeInfo().Assembly.Location);
		}

		static MetadataReference GetMetadataReference(string assemblyName)
		{
			return MetadataReference.CreateFromFile(Assembly.Load(assemblyName).Location);
		}

		static string GetMetadataLocationFolder(Type type)
		{
			var ret = Path.GetDirectoryName(type.Assembly.Location);
			return (ret == null) ? "" : ret;
		}

		public bool Initialize()
		{
			_iScript = null;

			if (string.IsNullOrEmpty(strCode))
				return true;

			string message;
			_asm = Compile(strCode, strClassName, astrRefAsm, out message);

			if (_asm == null)
				throw new Exception(message);

			Type? type = _asm.GetType(strClassName);
			if (type == null)
				return false;

			Type[] interfaces = type.GetInterfaces();
			if (((IList<Type>)interfaces).Contains(typeof(IPlugin)))
			{
				_iScript = (IPlugin?)Activator.CreateInstance(type);

				if (_iScript == null)
					throw new Exception("Error: Failed to create instance of plugin.");

				Host host = new Host();

				_iScript?.OnCreate(host);

				return true;
			}

			return false;
		}



		public static Assembly? Compile(string strCode, string strClassName, string[] astrRefAsm, out string Message)
		{
			Assembly asm;

			Message = "";
			try
			{
				if (strCode == "" || strClassName == "")
				{
					//TODO: resource
					Message = @"Script code or class name is missing.";
					return null;
				}

				var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

				SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(strCode);
				CSharpCompilation compilation = CSharpCompilation.Create("test.dll", options: compilationOptions, syntaxTrees: new[] { tree });

				var references = new List<MetadataReference>()
				{
						GetMetadataReference(typeof(object)),
						GetMetadataReference(typeof(System.Windows.Media.Imaging.WriteableBitmap)),
						GetMetadataReference(typeof(System.Windows.Point)),
						GetMetadataReference(typeof(System.Windows.Media.DrawingImage)),
						GetMetadataReference(typeof(System.Globalization.CultureInfo)),
				};

				var folders = new string[]
				{
						Uty.GetExecutableFolder(),
						GetMetadataLocationFolder(typeof(object)),
						GetMetadataLocationFolder(typeof(System.Windows.DataFormat)),
						GetMetadataLocationFolder(typeof(System.Xaml.AmbientPropertyValue)),
						GetMetadataLocationFolder(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)),
						GetMetadataLocationFolder(typeof(System.Windows.Xps.VisualsToXpsDocument)),
						GetMetadataLocationFolder(typeof(System.Globalization.Calendar)),
				};

				if (astrRefAsm != null)
				{
					foreach (var strDll in astrRefAsm)
					{
						string path = "";

						if (File.Exists(strDll))
							path = strDll;
						else
						{
							foreach (var item in folders)
							{
								if (string.IsNullOrEmpty(item))
									continue;
								path = Path.Combine(item, strDll);
								if (File.Exists(path))
									break;
								path = "";
							}
						}

						if (path == "")
						{
							//TODO: resource
							throw new Exception($"Error: Dll file not found. ({strDll})");
						}

						references.Add(MetadataReference.CreateFromFile(path));
					}
				}

				{
					var strDll = "Plugin.dll";

					string path = "";
					foreach (var item in folders)
					{
						if (string.IsNullOrEmpty(item))
							continue;
						path = Path.Combine(item, strDll);
						if (File.Exists(path))
							break;
						path = "";
					}

					if (path == "")
					{
						//TODO: resource
						throw new Exception($"Error: Plugin.dll file not found.");
					}

					references.Add(MetadataReference.CreateFromFile(path));
				}

				compilation = compilation.AddReferences(references);




				using (MemoryStream stream = new MemoryStream())
				{
					Microsoft.CodeAnalysis.Emit.EmitResult compileResult = compilation.Emit(stream);
					if (compileResult.Success == false)
					{
						//TODO: resource
						string message = "Error: Compile failed.";
						foreach (var item in compileResult.Diagnostics)
						{
							if (message != "")
								message += "\r\n";
							message += item.ToString();
						}
						throw new Exception(message);
					}
					asm = Assembly.Load(stream.GetBuffer());
				}

				return asm;
			}
			catch (Exception ex)
			{
				Message = ex.Message;
			}
			return null;
		}

	}
}
