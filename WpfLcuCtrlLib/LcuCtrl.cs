using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Renci.SshNet;

namespace WpfLcuCtrlLib
{
    public partial class LcuCtrl
    {
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        ///  SFTP Download
        /// </summary>
        /// <param name="host"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        public static void SFTP_Download(string host, string username, string password, string localFilePath, string remoteFilePath)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(password));

            var connectionInfo = new ConnectionInfo(host, username, new AuthenticationMethod[]
            {
                new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile[] { new PrivateKeyFile(ms) }),
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
        /// <param name="host"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        public static void SFTP_Upload(string host, string username, string password, string localFilePath, string remoteFilePath)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(password));

            var connectionInfo = new Renci.SshNet.ConnectionInfo(host, username, new AuthenticationMethod[]
            {
                new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile[] { new PrivateKeyFile(ms) }),
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
        public async Task<string> LCU_HttpGet(string host, string command)
        {
            var uri = new Uri($"http://{host}/LCUWeb/api/{command}");

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
        public static void FTP_Download(string ftpUrl, string username, string password, string localFilePath, string remoteFilePath)
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
        private async Task<string> LCU_Command(string host, string payload)
        {
            var uri = new Uri($"http://{host}/LCUWeb/api/lcuCommand");

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
        public async Task<bool> LCU_DiskInfo(string host)
        {
            var ret = await LCU_Command(host,LcuDiskInfo.Command());

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
        public async Task<bool> LCU_Version(string host)
        {
            string ret = await LCU_Command(host,LcuVersion.Command());
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
        public async void LCU_GetMcFileList(string lcuName, string mcName)
        {
            // FTPアカウント情報を取得
            string password = "password";

            string ret = await LCU_Command(lcuName, McFileList.Command(mcName, 1, password, @"/Data"));

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
/*
         /// <summary>
        ///  LCU "lines" の情報を取得する
        /// </summary>
        /// <param name="lcuName"></param>
        public async Task<LineInfo>? ReadLcuLineInfo(string lcuName)
        {
            string response = await LCU_HttpGet(lcuName, "lines");

            XmlSerializer serializer = new(typeof(LineInfo));

            LineInfo? lineInfo = (LineInfo)serializer.Deserialize( new StringReader(response));

            return lineInfo;
        }
*/
    }
}
