using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FileIO = Microsoft.VisualBasic.FileIO;

namespace DriverDeleter
{
	class OemInfFile
	{
		protected enum SetupUOInfFlags : uint { NONE = 0x0000, SUOI_FORCEDELETE = 0x0001 };

		[DllImport("setupapi.dll", SetLastError = true)]
		protected static extern bool SetupUninstallOEMInf(String InfFileName, SetupUOInfFlags Flags, IntPtr Reserved);

		protected static readonly string INF_FOLDER_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "inf");
		
		public String FileName { get; set; }
		public String DeviceName { get; set; }
		public String DriverCatalog { get; set; }
		public String DriverDate { get; set; }
		public String DriverVersion { get; set; }
		public String DriverClass { get; set; }
		public String Provider { get; set; }

		public OemInfFile(string infFilePath)
		{
			FileName = Path.GetFileName(infFilePath);
			string text = File.ReadAllText(infFilePath);

			Regex regex = new Regex(@"Provider[\s]*=[\s]*(.*)", RegexOptions.IgnoreCase);
			Match match = regex.Match(text);
			if (match.Success)
			{
				string provider = match.Groups[1].Value.Trim();
				if (provider.StartsWith("%") && provider.EndsWith("%"))
				{
					provider = provider.Replace("%", String.Empty);
					regex = new Regex(String.Format(@"{0}[\s]*=[\s]*(.*)", Regex.Escape(provider)), RegexOptions.IgnoreCase);
					match = regex.Match(text);
					if (match.Success)
					{
						provider = match.Groups[1].Value.Trim();
					}
				}

				Provider = provider.Replace("\"", String.Empty);
			}

			regex = new Regex(@"DisplayName[\s]*=[\s]*(.*)", RegexOptions.IgnoreCase);
			match = regex.Match(text);
			if (match.Success)
			{
				string displayName = match.Groups[1].Value.Trim();
				if (displayName.StartsWith("%") && displayName.EndsWith("%"))
				{
					displayName = displayName.Replace("%", String.Empty);
					regex = new Regex(String.Format(@"{0}[\s]*=[\s]*(.*)", Regex.Escape(displayName)), RegexOptions.IgnoreCase);
					match = regex.Match(text);
					if (match.Success)
					{
						displayName = match.Groups[1].Value.Trim();
					}
				}

				DeviceName = displayName.Replace("\"", String.Empty);
			}

			regex = new Regex(@"Class[\s]*=[\s]*(.*)", RegexOptions.IgnoreCase);
			match = regex.Match(text);
			if (match.Success)
			{
				DriverClass = match.Groups[1].Value.Trim();
			}

			regex = new Regex(@"CatalogFile[\s]*=[\s]*(.*)", RegexOptions.IgnoreCase);
			match = regex.Match(text);
			if (match.Success)
			{
				DriverCatalog = match.Groups[1].Value.Trim();
			}

			regex = new Regex(@"DriverVer[\s]*=[\s]*(.*)", RegexOptions.IgnoreCase);
			match = regex.Match(text);
			if (match.Success)
			{
				string[] dateAndVersion = match.Groups[1].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				DriverDate = dateAndVersion[0].Trim();
				DriverVersion = dateAndVersion[1].Trim();
			}
		}

		public static void Delete(string infFileName)
		{
			FileInfo inf = new FileInfo(Path.Combine(INF_FOLDER_PATH, infFileName));
			FileInfo pnf = new FileInfo(Path.ChangeExtension(inf.FullName, ".PNF"));

			inf.Attributes = FileAttributes.Normal;
			FileIO.FileSystem.DeleteFile(inf.FullName, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin);

			if (pnf.Exists)
			{
				pnf.Attributes = FileAttributes.Normal;
				FileIO.FileSystem.DeleteFile(pnf.FullName, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin);
			}
		}

		public static string[] GetOemInfFilePaths()
		{
			return Directory.GetFiles(INF_FOLDER_PATH, "oem*.inf", SearchOption.TopDirectoryOnly);
		}

		public static void Uninstall(string infFileName)
		{
			if (!SetupUninstallOEMInf(infFileName, SetupUOInfFlags.SUOI_FORCEDELETE, IntPtr.Zero))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
	}
}
