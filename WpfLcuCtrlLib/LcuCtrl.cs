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

using Reactive.Bindings;
using System.Runtime.InteropServices;
using System.Text.Json;
using FluentFTP;

namespace WpfLcuCtrlLib
{
	public partial class LcuCtrl(
		string lcuName,
		int id,
		IFileTransferClient? fileTransferClient = null,
		ILcuWebApiClient? webApiClient = null,
		Action<string>? log = null):IDisposable
	{
		private readonly IFileTransferClient _fileTransferClient =
			fileTransferClient ?? new FluentFtpFileTransferClient();
		private readonly ILcuWebApiClient _webApiClient =
			webApiClient ?? new HttpLcuWebApiClient(lcuName, log: log);

		[DllImport("mcAccount.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		static extern int getMcUser(StringBuilder s, Int32 len);
		[DllImport("mcAccount.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		static extern int getMcPass(StringBuilder s, Int32 len);

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
		public async Task<string> LCU_HttpGet(
			string command,
			CancellationToken? token = null)
		{
			return await _webApiClient.GetAsync(
				command,
				token ?? CancellationToken.None);
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
			catch (Exception e)
			{
				Debug.WriteLine($"Exception: {e.Message}");
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
			return await _webApiClient.SendCommandAsync(
				payload,
				token ?? CancellationToken.None);
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
		public async Task<bool> GetMachineFiles(string machineName, string ipAddress,int pos, List<string> mcFile, List<string> lcuFile, string localPath, CancellationToken token)
		{
			int errIdx = -1; // エラーコードのインデックス

			StringBuilder sb = new StringBuilder(1024);
			Int32 len = 0;
			len = getMcUser(sb, sb.Capacity);
			string McUser = sb.ToString();
			len = getMcPass(sb, sb.Capacity);
			string mcPass = sb.ToString();

			// 装置(machine)からファイル(path)を取得する(結果、LCU の <FtpRoot>/MCFiles/(UUID)/{name} に保存されている)
			List<string> files = mcFile;
			List<string> lcuFiles = lcuFile;
			//string ret =await LCU_Command(GetMcFiles.Command(machineName, pos, McUser,mcPass, files, lcuFiles),token);
			string ret =await LCU_Command(GetMcFiles.Command(ipAddress, pos, McUser,mcPass, files, lcuFiles),token);

			if( ret == "Internal Server Error")
			{
				Debug.WriteLine($"{Name}:{machineName} access Failed.");
				return false;
			}

			GetMcFile retMsg = GetMcFile.FromJson(ret);
			if (retMsg == null)
			{
				return false;
			}
			if( retMsg.HasError(ref errIdx) )
			{
				var ec = retMsg.ftp.data[errIdx].errorCode;
				Debug.WriteLine($"{retMsg.ftp.data[errIdx].mcPath}={retMsg.ftp.data[errIdx].errorMessage} errCode={ec}");
				return false;
			}

			//コマンドのUUIDを取得する
			string LcuUUID_Path = retMsg.ftp.data[0].lcuPath.Split('/')[0];

			// LCUからファイルを取得する(FTP, SFTP)
			List<(string?, string?)> ftpData = retMsg.ftp.data
				.Where(x => x.errorCode == "" && x.errorMessage == "" && x.mcPath != null && x.lcuPath != null)
				.Select(x => (x.mcPath, x.lcuPath)).ToList();

			int downloadedCount = 0;
			if (ftpData.Count != 0)
			{
				downloadedCount = await DownLoadFiles(Name, FtpUser, FtpPassword, ftpData, localPath, token);
			}

			//コマンド毎にUUIDが変わるので、コマンド終了時にフォルダを削除する
			await ClearFtpFolders("/MCFiles/" + LcuUUID_Path, token);

			return errIdx == -1
				&& ftpData.Count == mcFile.Count
				&& downloadedCount == ftpData.Count;
		}

		public async Task<int> DownLoadFiles(string host, string user, string password, List<(string?,string?)> files, string localPath, CancellationToken token)
		{
			var downloads = files
				.Where(file => file.Item1 != null && file.Item2 != null)
				.Select(file => new FileTransferRequest(
					Path.Combine(localPath, file.Item1!).Replace("/", "\\"),
					"/MCFiles/" + file.Item2!.Replace("\\", "/")))
				.ToArray();

			var session = await _fileTransferClient.ConnectAsync(
				host.Split(':')[0],
				user,
				password,
				token);
			try
			{
				var results = await session.DownloadFilesAsync(downloads, token);
				return results.Count(result => result.IsSuccess);
			}
			finally
			{
				await session.DisconnectAsync(token);
			}
		}

		public async Task<bool> GetMachineFile(string machineName, string IpAddress, int pos, string mcFile, string lcuFile, string localPath, CancellationToken token)
		{
			return await GetMachineFiles(machineName, IpAddress, pos, [mcFile], [lcuFile], localPath, token);
		}

		/// <summary>
		/// 装置へファイルを転送する
		/// </summary>
		/// <param name="machineName">装置名</param>
		/// <param name="pos">LogicalPos</param>
		/// <param name="mcFile">取得したいファイル名</param>
		/// <param name="lcuFile">LCU上の保存パス</param>
		/// <param name="localPath">取得したファイルを格納するパス</param>
		public async Task<bool> PutMachineFile(string machineName, string IpAddress, int pos, string mcFile, string lcuFile, string localPath, CancellationToken? token=null)
		{
			string fileName = Path.GetFileName(mcFile);
			int errIdx = -1; // エラーコードのインデックス

			StringBuilder sb = new StringBuilder(1024);
			Int32 len = 0;
			len = getMcUser(sb, sb.Capacity);
			string McUser = sb.ToString();
			len = getMcPass(sb, sb.Capacity);
			string mcPass = sb.ToString();

			string localFilePath = Path.Combine(localPath, fileName);
			string remoteFilePath = "/" + lcuFile.TrimStart('/');
			var transferSession = await _fileTransferClient.ConnectAsync(
				Name.Split(':')[0],
				FtpUser,
				FtpPassword,
				token ?? CancellationToken.None);
			try
			{
				var transferResult = await transferSession.UploadFileAsync(
					localFilePath,
					remoteFilePath,
					token ?? CancellationToken.None);
				if (!transferResult.IsSuccess)
				{
					return false;
				}
			}
			finally
			{
				await transferSession.DisconnectAsync(token ?? CancellationToken.None);
			}

			List<string> files = [ mcFile ];
			List<string> lcuFiles = [lcuFile];
			//string cmdRet = await LCU_Command(PostMcFile.Command(machineName, pos, McUser,mcPass, files, lcuFiles),token);
			string cmdRet = await LCU_Command(PostMcFile.Command(IpAddress, pos, McUser,mcPass, files, lcuFiles),token);

			if( cmdRet == "Internal Server Error")
			{
				Debug.WriteLine($"{Name}:{machineName} access Failed.");
				return false;
			}
			PostMcFile? retData = JsonSerializer.Deserialize<PostMcFile>(cmdRet);
			if (retData != null && retData.HasError(ref errIdx))
			{
				if( retData.ftp.data == null)
				{
					Debug.WriteLine("FTP data is null.");
					return false;
				}
				if( retData.ftp.data.Count <= errIdx)
				{
					Debug.WriteLine("FTP data length is less than error index.");
					return false;
				}
				Debug.WriteLine($"{retData.ftp.data[errIdx].mcPath}={retData.ftp.data[errIdx].errorMessage} errCode={retData.ftp.data[errIdx].errorCode}");
				return false;
			}
			return true;
		}

		/// <summary>
		/// FTP のデータ転送用フォルダを作成する
		/// </summary>
		/// <param name="files">ファイルリスト</param>
		/// <returns></returns>
		public async Task<bool> CreateFtpFolders(
			List<string> files,
			string ftpRoot,
			bool withFile = false,
			CancellationToken token = default)
		{
			var transferSession = await ConnectFileTransferAsync(token);
			try
			{
				foreach (string file in files)
				{
					token.ThrowIfCancellationRequested();
					string path = withFile
						? Path.GetDirectoryName(ftpRoot + file) ?? throw new InvalidOperationException($"Could not determine the directory for '{file}'.")
						: ftpRoot + file;

					await transferSession.CreateDirectoryAsync(path, token);
					Debug.WriteLine(path);
				}
			}
			finally
			{
				await transferSession.DisconnectAsync(CancellationToken.None);
			}

			return true;
		}

		/// <summary>
		/// FTPサーバーの指定されたフォルダ以下をクリアする
		/// </summary>
		/// <param name="startFolder"></param>
		/// <returns></returns>
		public async Task<bool> ClearFtpFolders(string startFolder, CancellationToken token)
		{
			var transferSession = await ConnectFileTransferAsync(token);
			try
			{
				await transferSession.DeleteDirectoryAsync(startFolder, token);
			}
			finally
			{
				await transferSession.DisconnectAsync(CancellationToken.None);
			}

			return true;
		}

		private Task<IFileTransferSession> ConnectFileTransferAsync(CancellationToken token) =>
			_fileTransferClient.ConnectAsync(Name.Split(":")[0], FtpUser, FtpPassword, token);

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
			var requests = files.Select(file =>
			{
				var fileName = Path.GetFileName(file);
				var remotePath = (ftpRoot + Path.GetDirectoryName(file) + "\\" + fileName)
					.Replace("\\", "/");
				var localPath = (srcRoot + file).Replace("/", "\\");
				return new FileTransferRequest(localPath, remotePath);
			}).ToArray();

			var transferSession = await ConnectFileTransferAsync(token);
			try
			{
				var results = await transferSession.DownloadFilesAsync(requests, token);
				return results.Count == requests.Length && results.All(result => result.IsSuccess);
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				return false;
			}
			finally
			{
				await transferSession.DisconnectAsync(CancellationToken.None);
			}
		}

		public void Dispose()
		{
			Debug.WriteLine("LcuCtrl::Dispose");
		}
	}
}
