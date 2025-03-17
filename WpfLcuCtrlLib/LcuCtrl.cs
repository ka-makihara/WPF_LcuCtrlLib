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

using FluentFTP;
using Reactive.Bindings;

namespace WpfLcuCtrlLib
{
	public partial class LcuCtrl(string lcuName, int id):IDisposable
	{
		private static readonly HttpClient httpClient = new HttpClient();

		public string Name { get; set; } = lcuName; //LCUのPC名(IPアドレス)
		public string FtpUser { get; set; }
		public string FtpPassword { get; set; }
		public int Id { get; set; } = id;

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
		/// <param name="host">LCU名</param>
		/// <param name="command">コマンド</param>
		/// <returns></returns>
		public async Task<string> LCU_HttpGet(string command)
		{
			var uri = new Uri($"http://{Name}/LCUWeb/api/{command}");

			try
			{
				var httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);

				// 404 などエラーが発生した場合に例外を発生する
				HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();

				//var response = await httpWebRequest.GetResponseAsync();

				var stream = response.GetResponseStream();
				var reader = new StreamReader(stream);
				var responseBody = await reader.ReadToEndAsync();
				reader.Close();

				return responseBody;
			}
			catch (WebException e)
			{
				if( e.Response == null)
				{
					Debug.WriteLine($"WebException: {e.Message}");
					return "errorCode"; // 呼び出し側でこれを見ているので・・・
				}
				HttpWebResponse response = (HttpWebResponse)e.Response;

				Debug.WriteLine($"ResponseCode={response.StatusCode} Desc={response.StatusDescription}");

				var stream = response.GetResponseStream();
				var reader = new StreamReader(stream);
				var responseBody = await reader.ReadToEndAsync();
				reader.Close();

				return responseBody;
			}
		}
		/// <summary>
		/// FTP Download
		/// </summary>
		/// <param name="ftpUrl"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <param name="localFilePath"></param>
		public static bool FTP_Download(string ftpUrl, string username, string password, string localFilePath)
		{
			var request = (FtpWebRequest)WebRequest.Create(ftpUrl);

			request.Method = WebRequestMethods.Ftp.DownloadFile;
			request.Credentials = new NetworkCredential(username, password);
			request.UseBinary = true;
			request.UsePassive = true;
			request.KeepAlive = false;

			try
			{
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
			catch (WebException e)
			{
				Debug.WriteLine($"WebException: {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// FTP Upload
		/// </summary>
		/// <param name="ftpUrl">FTP URL</param>
		/// <param name="username">User Name</param>
		/// <param name="password">PassWord</param>
		/// <param name="localFilePath">local File</param>
		public static bool FTP_Upload(string ftpUrl, string username, string password, string localFilePath)
		{
			var request = (FtpWebRequest)WebRequest.Create(ftpUrl);

			request.Method = WebRequestMethods.Ftp.UploadFile;
			request.Credentials = new NetworkCredential(username, password);
			request.UseBinary = true;
			request.UsePassive = true;
			request.KeepAlive = false;

			try
			{
				//FTPサーバにファイルをアップロードするためのStreamを取得
				using (var fs = new FileStream(localFilePath, FileMode.Open))
				using (var requestStream = request.GetRequestStream())
				{
					fs.CopyTo(requestStream);
				}

				using (var response = (FtpWebResponse)request.GetResponse())
				{
					Debug.WriteLine($"Upload File Complete, status {response.StatusDescription}");
				}
				return true;
			}
			catch (WebException e)
			{
				Debug.WriteLine($"WebException: {e.Message}");
				return false;
			}
		}
#pragma warning restore SYSLIB0014

		/// <summary>
		/// LCU に対して Web API を実行する
		/// </summary>
		/// <param name="host"></param>
		/// <param name="payload"></param>
		/// <returns></returns>
		public async Task<string> LCU_Command(string payload, CancellationToken? token=null)
		{
			var uri = new Uri($"http://{Name}/LCUWeb/api/lcuCommand");

			var content = new StringContent(payload, Encoding.UTF8, "application/json");
			string ret = "";
			try {
				HttpResponseMessage result;
				string str;
				if (token != null)
				{
					//result = await httpClient.PostAsync(uri, content, (CancellationToken)token);
					//str = await result.Content.ReadAsStringAsync((CancellationToken)token);
					result = await httpClient.PostAsync(uri, content);
					str = await result.Content.ReadAsStringAsync();
				}
				else
				{
					result = await httpClient.PostAsync(uri, content);
					str = await result.Content.ReadAsStringAsync();
				}

				//Debug.WriteLine(result.StatusCode);
				//Debug.WriteLine(str);

				ret = str;
			}
			catch (HttpRequestException e) {
				Debug.WriteLine(e.Message);
				throw;
			}
			catch (TaskCanceledException e)
			{
				Debug.WriteLine(e.Message);
				throw;
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
		public async Task<List<LcuDiskInfo>?> LCU_DiskInfo(CancellationToken token)
		{
			var ret = await LCU_Command(LcuDiskInfo.Command(),token);

			if( ret == "") {
				return null;
			}

			List<LcuDiskInfo>? list = LcuDiskInfo.FromJson(ret);
			if( list == null)
			{
				return null;
			}
			/*
			foreach (var item in list)
			{
				Debug.WriteLine($"Drive: {item.driveLetter}, Total: {item.total}, Use: {item.use}, Free: {item.free}");
			}
			*/
			return list;
		}

		/// <summary>
		///  LCUのバージョン情報を取得する
		/// </summary>
		/// <param name="host"></param>
		public async Task<bool> LCU_Version(CancellationToken token)
		{
			string ret = await LCU_Command(LcuVersion.Command(),token);
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
		public async void LCU_GetMcFileList(string mcName, CancellationToken token)
		{
			// FTPアカウント情報
			string user = FtpUser;
			string password = FtpPassword;

			string ret = await LCU_Command(GetMcFileList.Command(mcName, 1, user, password, @"/Data"),token);

			if (ret == "") {
				return;
			}
			GetMcFileList? list = GetMcFileList.FromJson(ret);

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
		/// <param name="machineName">装置名</param>
		/// <param name="pos">LogicalPos</param>
		/// <param name="mcFilePath">取得したいファイル名</param>
		/// <param name="lcuPath">LCU上の保存パス</param>
		/// <param name="localPath">取得したファイルを格納するパス</param>
		public async Task<bool> GetMachineFile(string machineName, int pos, string mcFile, string lcuFile, string localPath, CancellationToken? token=null)
		{
			string fileName = Path.GetFileName(mcFile);
			string McUser = "Administrator"; // 仮ユーザー名
			string mcPass = "password"; // 仮パスワード

			// 装置(machine)からファイル(path)を取得する(結果、LCU の <FtpRoot>/MCFiles/{name} に保存されている)
			//   (装置のFTPアカウントはAdministrator/password とする　最終的にはC++  でユーザー名、パスワードを取得するようなDLLを作る)
			//mcFilePath = "Fuji/System3/Program/Peripheral/UpdateCommon.inf"; // 仮ファイル名
			List<string> files = [ mcFile ];
			List<string> lcuFiles = [lcuFile];
			string ret =await LCU_Command(GetMcFiles.Command(machineName, pos, McUser,mcPass, files, lcuFiles),token);

			if( ret == "Internal Server Error")
			{
				Debug.WriteLine($"{Name}:{machineName} access Failed.");
				return false;
			}
			// LCU FTPアカウント情報
			string user = FtpUser;
			string password = FtpPassword;

			// LCUからファイルを取得する(FTP, SFTP)
			var ftpUrl = $"ftp://{Name.Split(":")[0]}/LCU_{Id}/{lcuFile}";
			//var ftpUrl = $"ftp://{Name.Split(":")[0]}/{lcuFile}";

			string localFilePath = localPath + fileName;
			// ※デスクトップに保存する場合
			//string localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

			return FTP_Download(ftpUrl, user, password, localFilePath);
 
		}

		/// <summary>
		/// 装置へファイルを転送する
		/// </summary>
		/// <param name="machineName">装置名</param>
		/// <param name="pos">LogicalPos</param>
		/// <param name="mcFile">取得したいファイル名</param>
		/// <param name="lcuFile">LCU上の保存パス</param>
		/// <param name="localPath">取得したファイルを格納するパス</param>
		public async Task<bool> PutMachineFile(string machineName, int pos, string mcFile, string lcuFile, string localPath, CancellationToken? token=null)
		{
			string fileName = Path.GetFileName(mcFile);
			string McUser = "Administrator"; // 仮ユーザー名
			string mcPass = "password"; // 仮パスワード

			// LCU FTPアカウント情報
			string user = FtpUser;
			string password = FtpPassword;

			// LCUへファイルを転送する(FTP, SFTP)
			var ftpUrl = $"ftp://{Name.Split(":")[0]}/LCU_{pos}/{lcuFile}";

			string localFilePath = localPath + fileName;

			bool ret = FTP_Upload(ftpUrl, user, password, localFilePath);
			if(ret == false)
			{
				return false;
			}

			List<string> files = [ mcFile ];
			List<string> lcuFiles = [lcuFile];
			string cmdRet = await LCU_Command(PostMcFile.Command(machineName, pos, McUser,mcPass, files, lcuFiles),token);

			if( cmdRet == "Internal Server Error")
			{
				Debug.WriteLine($"{Name}:{machineName} access Failed.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// FTP のデータ転送用フォルダを作成する
		/// </summary>
		/// <param name="files">ファイルリスト</param>
		/// <returns></returns>
		public bool CreateFtpFolders(List<string> files, string ftpRoot)
		{
			string user = FtpUser;
			string password = FtpPassword;

			var ftpClient = new FtpClient(Name.Split(":")[0], user, password);

			ftpClient.AutoConnect();
			foreach (string file in files) {
				//フォルダ名を取り出す
				var path = Path.GetDirectoryName(ftpRoot + file);
				//var path = ftpRoot + file;

				//途中のフォルダも自動で生成してくれるFTPサーバーなら
				//  LCU 借りてテストしてOKだったので。
				ftpClient.CreateDirectory(path);
				Debug.WriteLine(path);

				/*
				// 途中のフォルダを分割する必要がある場合
				var ff = path.Split("\\");
				var path2 = "";
				foreach (string f in ff)
				{
					if( f == "") { continue; }

					//パスの区切り文字は環境によって異なるので、適宜変更する
					path2 += (("\\") + f);
					//path2 += (("/") + f);

					ftpClient.CreateDirectory(path2);
					Debug.WriteLine(path2);
				}
				*/
			}
			ftpClient.Disconnect();

			return true;
		}

		/// <summary>
		/// FTPサーバーの指定されたフォルダ以下をクリアする
		/// </summary>
		/// <param name="ftpClient"></param>
		/// <param name="folder"></param>
		private void DeleteFtpFolder(FtpClient ftpClient, string folder)
		{
			var files = ftpClient.GetListing(folder);
			foreach (FtpListItem file in files)
			{
				if (file.Type == FtpObjectType.Directory)
				{
					DeleteFtpFolder(ftpClient, file.FullName);
				}
				else
				{
					ftpClient.DeleteFile(file.FullName);
				}
			}
			ftpClient.DeleteDirectory(folder);
		}

		/// <summary>
		/// FTPサーバーの指定されたフォルダ以下をクリアする
		/// </summary>
		/// <param name="startFolder"></param>
		/// <returns></returns>
		public bool ClearFtpFolders(string startFolder)
		{
			string user = FtpUser;
			string password = FtpPassword;

			var ftpClient = new FtpClient(Name.Split(":")[0], user, password);

			ftpClient.AutoConnect();

			var files = ftpClient.GetListing(startFolder);
			foreach (FtpListItem file in files)
			{
				if (file.Type == FtpObjectType.Directory)
				{
					DeleteFtpFolder(ftpClient, file.FullName);
				}
				else
				{
					ftpClient.DeleteFile(file.FullName);
				}
			}

			ftpClient.Disconnect();

			return true;
		}

		/// <summary>
		/// FTP によるファイルアップロード
		/// </summary>
		/// <param name="ftpRoot">LCUの転送先フォルダ名</param>
		/// <param name="srcRoot">転送用データのルートパス</param>
		/// <param name="files">ファイルリスト</param>
		/// <returns></returns>
		public bool UploadFiles(string ftpRoot,string srcRoot, List<string> files)
		{
			string user = FtpUser;
			string password = FtpPassword;

			var ftpClient = new FtpClient(Name.Split(":")[0], user, password);

			ftpClient.AutoConnect();
			foreach (string file in files)
			{
				var fileName = Path.GetFileName(file);
				var remotePath = ftpRoot + Path.GetDirectoryName(file) + "\\" + fileName;
				var localPath = srcRoot + file;

				try
				{
					localPath = localPath.Replace("/", "\\");
					remotePath = remotePath.Replace("\\","/");
					ftpClient.UploadFile(localPath, remotePath);

					Debug.WriteLine($"Upload File: {localPath} to {remotePath}");
				}
				catch(Exception e) {
					Debug.WriteLine(e.Message);
					break;
				}
			}
			ftpClient.Disconnect();

			return true;
		}

		/// <summary>
		///  FTP によるファイルアップロード2
		/// </summary>
		/// <param name="ftpRoot">FTP先のパス</param>
		/// <param name="srcRoot">転送元のルートパス</param>
		/// <param name="files">ファイルリスト(path, file1, file2)[]</param>
		/// <returns></returns>
		public bool UploadFiles2(string ftpRoot,string srcRoot, List<(string?,string,string)> files)
		{
			string user = FtpUser;
			string password = FtpPassword;

			var ftpClient = new FtpClient(Name.Split(":")[0], user, password);

			ftpClient.AutoConnect();
			foreach ((string path, string f1, string f2) file in files)
			{
				try
				{
					var remotePath = ftpRoot + file.path + "\\" + file.f1;
					var localPath = srcRoot + file.path + '/' + file.f1;

					localPath = localPath.Replace("/", "\\");
					remotePath = remotePath.Replace("\\","/");

					ftpClient.UploadFile(localPath, remotePath);

					Debug.WriteLine($"Upload File: {localPath} to {remotePath}");

					if( file.f2 != "" )
					{
						remotePath = ftpRoot + file.path + "\\" + file.f2;
						localPath = srcRoot + file.path + '/' + file.f2;

						localPath = localPath.Replace("/", "\\");
						remotePath = remotePath.Replace("\\","/");

						ftpClient.UploadFile(localPath, remotePath);
						Debug.WriteLine($"Upload File: {localPath} to {remotePath}");
					}
				}
				catch(Exception e) {
					Debug.WriteLine(e.Message);
					break;
				}
			}
			ftpClient.Disconnect();

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="folders"></param>
		/// <param name="lcuRoot"></param>
		/// <returns></returns>
		public bool UploadFiles3(List<string> folders, string lcuRoot)
		{
			string user = FtpUser;
			string password = FtpPassword;

			var ftpClient = new FtpClient(Name.Split(":")[0], user, password);

			ftpClient.AutoConnect();
			foreach (string folder in folders)
			{
				try
				{
					ftpClient.UploadDirectory(folder, lcuRoot, FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite);
				}
				catch (Exception e)
				{
					break;
				}
			}
			ftpClient.Disconnect();
			return true;
		}

		/// <summary>
		///  (指定フォルダに含まれるファイルをすべてLCU にファイルをアップロードする(フォルダ構造を維持する)
		/// </summary>
		/// <param name="folders">フォルダリスト</param>
		/// <param name="lcuRoot">LCUのFTPルートパス</param>
		/// <param name="srcRoot">元フォルダからLCUフォルダへ変換する起点フォルダ</param>
		/// <returns></returns>
		public bool UploadFilesWithFolder(List<string> folders, string lcuRoot, string srcRoot)
		{
			string user = FtpUser;
			string password = FtpPassword;

			var ftpClient = new FtpClient(Name.Split(":")[0], user, password);

			ftpClient.AutoConnect();
			foreach (string folder in folders)
			{
				// 元フォルダからLCUフォルダへ変換する
				string lcuPath = lcuRoot + folder[srcRoot.Length..];
				try
				{
					// folder 下の全ファイルを lcuPath フォルダへアップロードする(フォルダがない場合は作成してくれる)
					ftpClient.UploadDirectory(folder, lcuPath, FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite);
				}
				catch (Exception e)
				{
					break;
				}
			}
			ftpClient.Disconnect();
			return true;
		}
		/// <summary>
		/// FTP によるファイルダウンロード
		/// </summary>
		/// <param name="ftpRoot"></param>
		/// <param name="srcRoot"></param>
		/// <param name="files">ファイルリスト</param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task<bool> DownloadFiles(string ftpRoot, string srcRoot, List<string> files, CancellationToken token)
		{
			string user = FtpUser;
			string password = FtpPassword;
			bool ret = true;

			var ftpClient = new FtpClient(Name.Split(":")[0], user, password);

			ftpClient.AutoConnect();
			foreach (string file in files)
			{
				var fileName = Path.GetFileName(file);
				var remotePath = ftpRoot + Path.GetDirectoryName(file) + "\\" + fileName;
				var localPath = srcRoot + file;

				try
				{
					localPath = localPath.Replace("/", "\\");
					remotePath = remotePath.Replace("\\", "/");

					// フォルダがない場合は作成する(既に存在していてもエラーにならないので、無条件で作成)
					System.IO.Directory.CreateDirectory(Path.GetDirectoryName(localPath));

					ftpClient.DownloadFile(localPath, remotePath);
					
					Debug.WriteLine($"Download File: {remotePath} to {localPath}");

					if( token.IsCancellationRequested)
					{
						ret = false;
						break;
					}
				}
				catch (Exception e)
				{
					Debug.WriteLine(e.Message);
					ret = false;
					break;
				}
			}
			ftpClient.Disconnect();

			return ret;
		}

		public void Dispose()
		{
			
			//  いらないかも・・・
			httpClient.Dispose();
			Debug.WriteLine("LcuCtrl::Dispose");
		}
	}
}
