using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

//using static SymmetricCryptography.Cryptography;

namespace WpfLcuCtrlLib
{
	/*
	internal class LcuCommand
	{
	}
	*/

	[XmlRoot("lines")]
	public class LineInfo
	{
		[XmlElement("line")]
		public Line? Line { get; set; }
	}

	public class Line
	{
		[XmlElement("lineName")]
		public string? LineName { get; set; }
		[XmlArray("machines")]
		[XmlArrayItem("machine")]
		public List<Machine>? Machines { get; set; }
	}
	public class Machine
	{
		[XmlElement("machineName")]
		public string? MachineName { get; set; }
		[XmlElement("boardFlow")]
		public string? BoardFlow { get; set; }
		[XmlElement("machineType")]
		public string? MachineType { get; set; }
		[XmlElement("maxLane")]
		public int? MaxLane { get; set; }
		[XmlElement("modelName")]
		public string? ModelName { get; set; }
		[XmlArray("bases")]
		[XmlArrayItem("base")]
		public List<Base>? Bases { get; set; }

		public void SetData(Machine machine)
		{
			this.MachineName = machine.MachineName;
			this.BoardFlow = machine.BoardFlow;
			this.MachineType = machine.MachineType;
			this.MaxLane = machine.MaxLane;
			this.ModelName = machine.ModelName;
		}
	}
	public class Base
	{
		[XmlElement("baseId")]
		public int BaseId { get; set; }
		[XmlElement("baseType")]
		public int BaseType { get; set; }
		[XmlElement("ipAddr")]
		public string? IpAddr { get; set; }
		[XmlElement("position")]
		public int Position { get; set; }
		[XmlElement("FSSisConveyor")]
		public string? FSSisConveyor { get; set; }
		[XmlArray("modules")]
		[XmlArrayItem("module")]
		public List<Module>? Modules { get; set; }

		public void SetData(Base data)
		{
			this.BaseId = data.BaseId;
			this.BaseType = data.BaseType;
			this.IpAddr = data.IpAddr;
			this.Position = data.Position;
			this.FSSisConveyor = data.FSSisConveyor;
		}
	}
	public class Module
	{
		[XmlElement("moduleId")]
		public int ModuleId { get; set; }
		[XmlElement("logicalPos")]
		public int LogicalPos { get; set; }
		[XmlElement("moduleType")]
		public int ModuleType { get; set; }
		[XmlElement("physicalPos")]
		public int PhysicalPos { get; set; }
		[XmlElement("dispModule")]
		public string? DispModule { get; set; }

		public void SetData(Module data)
		{
			this.ModuleId = data.ModuleId;
			this.LogicalPos = data.LogicalPos;
			this.ModuleType = data.ModuleType;
			this.PhysicalPos = data.PhysicalPos;
			this.DispModule = data.DispModule;
		}
	}

#pragma warning disable IDE1006 // 命名スタイル
	public class FtpData
	{
		public string? username { get; set; }
		public string? password { get; set; }

		public static string Command()
		{
			return "{\"cmd\":\"GetFTPAccount\"}";
		}
		public static FtpData? FromJson(string str)
		{
			if (str == "") return null;

			FtpData? data = JsonSerializer.Deserialize<FtpData>(str);

			return data;
		}
		public static string GetPasswd(string user, string passCode)
		{
			string pass = "";
			SymmetricCryptography.Cryptography.DecryptUserPassword(user, passCode, ref pass);

			if (pass == "") pass = passCode;　//復号化失敗は変換なし(仮想LCUなどはそのまま)

			return pass;
		}
	}
	public class SFtpData
	{
		public string? username { get; set; }
		public string? key { get; set; }

		internal static string Command()
		{
			return "{\"cmd\":\"GetSFTPAccount\",\"properties\":{\"accountType\":0}}";
		}
		internal static SFtpData? FromJson(string str)
		{
			SFtpData? data = JsonSerializer.Deserialize<SFtpData>(str);

			return data;
		}
		internal static string GetPasswd(string user, string keyCode)
		{
			string pass = "";
			SymmetricCryptography.Cryptography.DecryptUserPassword(user, keyCode, ref pass);

			return pass;
		}
	}

	public class LcuVersion
	{
		public string? itemName { get; set; }
		public string? itemVersion { get; set; }
		public string? message { get; set; }
		public string? errorCode { get; set; }

		public static string Command()
		{
			return @"{""cmd"":""GetLCUVersion"",""properties"":{""lang"":""jpn""}}";
		}
		public static List<LcuVersion>? FromJson(string str)
		{
			List<LcuVersion>? list = JsonSerializer.Deserialize<List<LcuVersion>>(str);

			return list;
		}
	}

	public class LcuDiskInfo
	{
		public string? driveLetter { get; set; }
		public string? use { get; set; }
		public string? total { get; set; }
		public string? free { get; set; }

		public static List<LcuDiskInfo>? FromJson(string str)
		{
			List<LcuDiskInfo>? list = JsonSerializer.Deserialize<List<LcuDiskInfo>>(str);

			return list;
		}
		public static string Command()
		{
			return @"{""cmd"":""GetDiskInformation""}";
		}
	}

	public class LcuMachineVersion
	{
		public string? machineName { get; set; }
		public string? machineType { get; set; }
		public string? machineVersion { get; set; }
		public string? message { get; set; }
		public string? errorCode { get; set; }

		public static List<LcuMachineVersion>? FromJson(string str)
		{
			List<LcuMachineVersion>? list = JsonSerializer.Deserialize<List<LcuMachineVersion>>(str);

			return list;
		}
		public static string Command(string lineName)
		{
			return $"{{\"cmd\":\"GetMachineVersionList\",\"properties\":{{\"lineName\":\"{lineName}\",\"lang\":\"jpn\"}}";
		}

	}

	public class GetMcFiles
	{
		public McLockUnlock? mcLockUnlock { get; set; }
		public Ftp<FileGetData>? ftp { get; set; }

		public static string Command(string mcName, int moduleNo, string user, string password, List<string> mcFiles, List<string> lcuFiles = null)
		{
			return $"{{\"cmd\":\"GetMCFile\"," +
						$"\"properties\":{{" +
							$"\"machineName\":\"{mcName}\"," +
							$"\"moduleNo\":{moduleNo}," +
							$"\"gantry\":2," +
							$"\"mcLockUnlock\":{{" +
								$"\"lock\":{{\"cmdNo\":\"0x01000071\"}}," +
								$"\"unlock\":{{\"cmdNo\":\"0x01000072\"}}," +
								$"\"gantry\":1}}," +
							$"\"ftp\":{{" +
								$"\"userName\":\"{user}\",\"password\":\"{password}\"," +
								$"\"data\":[" + CreateFileList(mcFiles, lcuFiles) + $"]" +
						"}}}";
		}
		public static GetMcFiles? FromJson(string str)
		{
			GetMcFiles? file = JsonSerializer.Deserialize<GetMcFiles>(str);

			return file;
		}
		public static string CreateFileList(List<string> mcFiles, List<string> lcuFiles = null)
		{
			string fileList = "";

			foreach (string file in mcFiles)
			{
				string mcPath = file.Trim('/');
				string lcuPath = lcuFiles[ mcFiles.IndexOf(file) ].Trim('/');
				if (fileList != "")
				{
					fileList += ",";
				}
				fileList += $"{{\"mcPath\":\"{mcPath}\",\"lcuPath\":\"{lcuPath}\"}}";
			}
			fileList = fileList.TrimEnd(',');

			return fileList;
		}
	}

	public class GetMcFile
	{
		public McLockUnlock? mcLockUnlock { get; set; }
		public Ftp<FileGetData>? ftp { get; set; }

		public static string Command(string mcName, int moduleNo, string user, string password, string file, string? lcuPath = null)
		{
			return $"{{\"cmd\":\"GetMCFile\"," +
						$"\"properties\":{{" +
							$"\"machineName\":\"{mcName}\"," +
							$"\"moduleNo\":{moduleNo}," +
							$"\"gantry\":2," +
							$"\"mcLockUnlock\":{{" +
								$"\"lock\":{{\"cmdNo\":\"0x01000071\"}}," +
								$"\"unlock\":{{\"cmdNo\":\"0x01000072\"}}," +
								$"\"gantry\":1}}," +
							$"\"ftp\":{{" +
								$"\"userName\":\"{user}\",\"password\":\"{password}\"," +
								$"\"data\":[" + CreateFileList(file, lcuPath) + $"]" +
						"}}}";
		}
		public static GetMcFile? FromJson(string str)
		{
			GetMcFile? file = JsonSerializer.Deserialize<GetMcFile>(str);

			return file;
		}
		public static string CreateFileList(string file, string? path = null)
		{
			string fileList = "";

			string mcPath = file.Trim('/');
			string lcuPath = path + Path.GetFileName(file.Trim('/'));
			fileList = $"{{\"mcPath\":\"{mcPath}\",\"lcuPath\":\"{lcuPath}\"}}";

			return fileList;
		}
		public bool HasError()
		{
			if (ftp == null) return true;
			if (ftp.data == null) return true;

			foreach (var data in ftp.data)
			{
				if (data.errorCode == null ||  data.errorCode.Length != 0) return true;
			}
			return false;
		}
	}

	public class GetMcFileList
	{
		public McLockUnlock? mcLockUnlock { get; set; }
		public Ftp<FileListData>? ftp { get; set; }

		public static string Command(string mcName, int moduleNo, string user, string password, string folder)
		{
			return $"{{\"cmd\":\"GetMCFileList\"," +
						$"\"properties\":{{" +
							$"\"machineName\":\"{mcName}\"," +
							$"\"moduleNo\":{moduleNo}," +
							$"\"gantry\":2," +
							$"\"mcLockUnlock\":{{" +
								$"\"lock\":{{\"cmdNo\":\"0x01000071\"}}," +
								$"\"unlock\":{{\"cmdNo\":\"0x01000072\"}}," +
								$"\"gantry\":1}}," +
							$"\"ftp\":{{" +
								$"\"userName\":\"{user}\",\"password\":\"{password}\"," +
								$"\"data\":[{{" +
									$"\"mcPath\":\"{folder}\"}}" +
								$"]" +
						"}}}";
		}
		public static GetMcFileList? FromJson(string str)
		{
			GetMcFileList? list = JsonSerializer.Deserialize<GetMcFileList>(str);

			return list;
		}
	}

	public class McLockUnlock
	{
		public LockUnlock? lock_ { get; set; }
		public LockUnlock? unlock { get; set; }
	}

	public class LockUnlock
	{
		public string? errorCode { get; set; }
	}

	public class Ftp<Type>
	{
		public List<Type>? data { get; set; }
	}

	public class FileListData
	{
		public string? mcPath { get; set; }
		public string? errorCode { get; set; }
		public string? errorMessage { get; set; }
		public List<File>? list { get; set; }
	}

	public class File
	{
		public string? name { get; set; }
	}

	public class FileGetData
	{
		public string? mcPath { get; set; }
		public string? lcuPath { get; set; }
		public string? errorCode { get; set; }
		public string? errorMessage { get; set; }
	}
#pragma warning restore IDE1006 // 命名スタイル 

	public class UserUpdate
	{
		public string? userName { get; set; }
		public string? password { get; set; }
	}

	public class PostMcFile
	{
		public McLockUnlock? mcLockUnlock { get; set; }
		public Ftp<FileListData>? ftp { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mcName">装置名</param>
		/// <param name="moduleNo">モジュール番号</param>
		/// <param name="user">FTPユーザー名</param>
		/// <param name="password">FTPパスワード</param>
		/// <param name="files">ファイルリスト</param>
		/// <returns></returns>
		public static string Command(string mcName, int moduleNo, string user, string password, List<string> mcFiles, List<string> lcuFiles = null)
		{

			string ret = $"{{\"cmd\":\"PostMCFile\"," +
						$"\"properties\":{{" +
							$"\"machineName\":\"{mcName}\"," +
							$"\"moduleNo\":{moduleNo}," +
							$"\"gantry\":2," +
							$"\"mcLockUnlock\":{{" +
								$"\"lock\":{{\"cmdNo\":\"0x01000071\"}}," +
								$"\"unlock\":{{\"cmdNo\":\"0x01000072\"}}," +
								$"\"gantry\":1}}," +
							$"\"ftp\":{{" +
								$"\"userName\":\"{user}\",\"password\":\"{password}\"," +
								$"\"data\":[" + CreateFileList(mcFiles, lcuFiles) + $"]" +
						"}}}";

			return ret;
		}
		public static GetMcFileList? FromJson(string str)
		{
			GetMcFileList? list = JsonSerializer.Deserialize<GetMcFileList>(str);

			return list;
		}
		private static string CreateFileList(List<string> mcFiles, List<string> lcuFiles = null)
		{
			string fileList = "";

			foreach (string file in mcFiles)
			{
				if (file != "")
				{
					string mcPath = file.Trim('/');
					string lcuPath = lcuFiles[mcFiles.IndexOf(file)].Trim('/');
					if (fileList != "")
					{
						fileList += ",";
					}
					fileList += $"{{\"mcPath\":\"{mcPath}\",\"lcuPath\":\"{lcuPath}\",\"overwrite\":1}}";
				}
			}
			fileList = fileList.TrimEnd(',');

			return fileList;
		}
	}


	public class GetDir
	{
		public string? type { get; set; }
		public string? name { get; set; }
		public string? update { get; set; }

		public static List<GetDir>? FromJson(string str)
		{
			List<GetDir>? list = JsonSerializer.Deserialize<List<GetDir>>(str);

			return list;
		}
		public static string Command(string path)
		{
			return $"{{\"cmd\":\"GetDir\",\"properties\":{{\"path\":\"{path}\"}}}}";
		}
	}

	public class SetLcu
	{
		public static string Command(int id)
		{
			return $"{{\"cmd\":\"SetLCU\",\"properties\":{{\"Name\":\"LCU_{id}\"}}}}";
		}
	}
}
