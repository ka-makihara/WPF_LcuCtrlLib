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
            FtpData? data = JsonSerializer.Deserialize<FtpData>(str);

            return data;
        }
        public static string GetPasswd(string user, string passCode)
        {
            string pass = "";
            SymmetricCryptography.Cryptography.DecryptUserPassword(user, passCode, ref pass);

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

    public class GetMcFile
    {
        public McLockUnlock? mcLockUnlock { get; set; }
        public Ftp<FileGetData>? ftp { get; set; }

        public static string Command(string mcName, int moduleNo, string user, string password, string mcPath, string lcuPath)
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
                                $"\"data\":[{{" +
                            $"\"mcPath\":\"{mcPath}\"," +
                            $"\"lcuPath\":\"{lcuPath}\"}}" +
                            $"]" +
                        "}}}";
        }
        public static GetMcFile? FromJson(string str)
        {
            GetMcFile? file = JsonSerializer.Deserialize<GetMcFile>(str);

            return file;
        }
    }
    public class McFileList
    {
        public McLockUnlock? mcLockUnlock { get; set; }
        public Ftp<FileListData>? ftp { get; set; }

        public static string Command(string mcName, int moduleNo, string password, string folder)
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
                                $"\"userName\":\"Administrator\",\"password\":\"{password}\"," +
                                $"\"data\":[{{" +
                                    $"\"mcPath\":\"{folder}\"}}" +
                                $"]" +
                        "}}}";
        }
        public static McFileList? FromJson(string str)
        {
            McFileList? list = JsonSerializer.Deserialize<McFileList>(str);

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
}
