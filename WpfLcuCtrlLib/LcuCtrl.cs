using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Renci.SshNet;

namespace WpfLcuCtrlLib
{
    public partial class LcuCtrl(string lcuName):IDisposable
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public string? Name { get; set; } = lcuName;
        public string? FtpUser { get; set; }
        public string? FtpPassword { get; set; }

        /// <summary>
        ///  SFTP Download
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        public void SFTP_Download(string localFilePath, string remoteFilePath)
        {
            if(FtpUser == null || FtpPassword == null)
            {
                return;
            }
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(FtpPassword));

            var connectionInfo = new ConnectionInfo(Name, FtpUser, new AuthenticationMethod[]
            {
                new PrivateKeyAuthenticationMethod(FtpUser, new PrivateKeyFile[] { new PrivateKeyFile(ms) }),
            });

            using (var client = new SftpClient(connectionInfo)) {
                try {
                    client.Connect();
                    using (var fs = new FileStream(localFilePath, FileMode.Create)) {
                        client.DownloadFile(remoteFilePath, fs);
                    }
                    Debug.WriteLine("Download Complete");
                }
                catch (Exception e) {
                    Debug.WriteLine($"An error accurred: {e.Message}");
                }
                finally {
                    client.Disconnect();
                }
            }
        }

        /// <summary>
        /// SFTP Upload
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        public void SFTP_Upload(string localFilePath, string remoteFilePath)
        {
            if(FtpUser == null || FtpPassword == null)
            {
                return;
            }

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(FtpPassword));

            var connectionInfo = new Renci.SshNet.ConnectionInfo(Name, FtpUser, new AuthenticationMethod[]
            {
                new PrivateKeyAuthenticationMethod(FtpUser, new PrivateKeyFile[] { new PrivateKeyFile(ms) }),
            });

            using (var client = new SftpClient(connectionInfo)) {
                try {
                    client.Connect();
                    using (var fs = new FileStream(localFilePath, FileMode.Open)) {
                        client.UploadFile(fs, remoteFilePath);
                    }
                    Debug.WriteLine("Upload File Complete");
                }
                catch (Exception e) {
                    Debug.WriteLine(e.Message);
                }
                finally {
                    client.Disconnect();
                }
            }
        }

#pragma warning disable SYSLIB0014  //Create の warning を抑制
        /// <summary>
        /// LCU に対して GET リクエストを送信する
        /// </summary>
        /// <param name="host"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<string> LCU_HttpGet(string command)
        {
            var uri = new Uri($"http://{Name}/LCUWeb/api/{command}");

            var httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
            var response = await httpWebRequest.GetResponseAsync();

            var stream = response.GetResponseStream();
            var reader = new StreamReader(stream);
            var responseBody = await reader.ReadToEndAsync();

            reader.Close();

            return responseBody;
        }
        /// <summary>
        /// FTP Download
        /// </summary>
        /// <param name="ftpUrl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        public static bool FTP_Download(string ftpUrl, string username, string password, string localFilePath, string remoteFilePath)
        {
            var request = (FtpWebRequest)WebRequest.Create(ftpUrl);

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(username, password);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            //FTPサーバからファイルをダウンロードするためのStreamを取得
            using (var response = (FtpWebResponse)request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var fs = new FileStream(localFilePath, FileMode.Create))
            {
                responseStream.CopyTo(fs);
                Debug.WriteLine($"Download File Complete, status {response.StatusDescription}");
            }
            return true;
        }

        /// <summary>
        /// FTP Upload
        /// </summary>
        /// <param name="ftpUrl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        public static void FTP_Upload(string ftpUrl, string username, string password, string localFilePath, string remoteFilePath)
        {
            var request = (FtpWebRequest)WebRequest.Create(ftpUrl);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(username, password);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;

            //FTPサーバにファイルをアップロードするためのStreamを取得
            using (var fs = new FileStream(localFilePath, FileMode.Open))
            using (var requestStream = request.GetRequestStream()) {
                fs.CopyTo(requestStream);
            }

            using (var response = (FtpWebResponse)request.GetResponse()) {
                Debug.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            }
        }
#pragma warning restore SYSLIB0014

        /// <summary>
        /// LCU に対して Web API を実行する
        /// </summary>
        /// <param name="host"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<string> LCU_Command(string payload)
        {
            var uri = new Uri($"http://{Name}/LCUWeb/api/lcuCommand");

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            string ret = "";
            try {
                var result = await httpClient.PostAsync(uri, content);

                var str = await result.Content.ReadAsStringAsync();

                Debug.WriteLine(result.StatusCode);
                Debug.WriteLine(str);

                ret = str;
            }
            catch (HttpRequestException e) {
                Debug.WriteLine(e.Message);
            }
            finally {
                content.Dispose();
            }
            return ret;
        }

        /// <summary>
        /// LCUのディスク情報を取得する
        /// </summary>
        /// <param name="host"></param>
        public async Task<bool> LCU_DiskInfo()
        {
            var ret = await LCU_Command(LcuDiskInfo.Command());

            if( ret == "") {
                return false;
            }

            List<LcuDiskInfo>? list = LcuDiskInfo.FromJson(ret);
            if( list == null)
            {
                return false;
            }
            foreach (var item in list)
            {
                Debug.WriteLine($"Drive: {item.driveLetter}, Total: {item.total}, Use: {item.use}, Free: {item.free}");
            }
            return true;
        }

        /// <summary>
        ///  LCUのバージョン情報を取得する
        /// </summary>
        /// <param name="host"></param>
        public async Task<bool> LCU_Version()
        {
            string ret = await LCU_Command(LcuVersion.Command());
            if( ret == "") {
                return false;
            }
            List<LcuVersion>? list = LcuVersion.FromJson(ret);

            if(list == null)
            {
                return false;
            }
            foreach (var item in list)
            {
                Debug.WriteLine($"ItemName: {item.itemName}, ItemVersion: {item.itemVersion}, Message: {item.message}, ErrorCode: {item.errorCode}");
            }
            return true;
        }

        /// <summary>
        /// 装置のファイルリストを取得する
        /// </summary>
        /// <param name="lcuName"></param>
        /// <param name="mcName"></param>
        public async void LCU_GetMcFileList(string mcName)
        {
            // FTPアカウント情報を取得
            string password = "password";

            string ret = await LCU_Command(McFileList.Command(mcName, 1, password, @"/Data"));

            if (ret == "") {
                return;
            }
            McFileList? list = McFileList.FromJson(ret);

            if(list == null)
            {
                return;
            }

            if (list.ftp != null && list.ftp.data != null) {
                foreach (var item in list.ftp.data) {
                    Debug.WriteLine($"Path: {item.mcPath}");
                    if (item.list != null) {
                        foreach (var file in item.list) {
                            Debug.WriteLine($"File: {file.name}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 装置からファイルを取得する
        /// </summary>
        /// <param name="lcuName">LCU名</param>
        /// <param name="machineName">装置名</param>
        /// <param name="pos">LogicalPos</param>
        /// <param name="mcFilePath">取得したいファイル名</param>
        /// <param name="localPath">取得したファイルをこの名前にする</param>
        public async Task<bool> GetMachineFile(string lcuName, string machineName, int pos, string mcFilePath, string localPath)
        {
            string fileName = Path.GetFileName(mcFilePath);
            string mcPass = "password"; // 仮パスワード
            string McUser = "Administrator"; // 仮ユーザー名

            // 装置(machine)からファイル(path)を取得する(結果、LCU の <FtpRoot>/MCFiles/{name} に保存されている)
            //   (装置のFTPアカウントはAdministrator/password とする　最終的にはC++  でユーザー名、パスワードを取得するようなDLLを作る)
            string ret =await  LCU_Command(GetMcFile.Command(machineName, pos, McUser,mcPass, mcFilePath, fileName));

            // LCU FTPアカウント情報を取得
            //string? user = FtpUser;
            //string? password = FtpPassword;
            string user = "Administrator";
            string password = "password";

            // LCUからファイルを取得する(FTP, SFTP)
            // Debug 用にFTPを一つでまかなうため
            var ftpUrl = $"ftp://{lcuName.Split(":")[0]}/LCU_{pos}/MCFiles/" + fileName;
            
            // 通常の場合
            //var ftpUrl = $"ftp://{lcuName}/MCFiles/" + fileName;

            string remoteFilePath = "/MCFiles/" + fileName;
            string localFilePath = localPath;
            // ※デスクトップに保存する場合
            //string localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            return FTP_Download(ftpUrl, user, password, localFilePath, remoteFilePath);
 
        }

        public void Dispose()
        {
            // LcuCtrl のインスタンス毎(LCU毎に接続先が異なるので)に HttpClient を生成しているので、Dispose する
            //  いらないかも・・・
            httpClient.Dispose();
            Debug.WriteLine("LcuCtrl::Dispose");
        }
    }
}
