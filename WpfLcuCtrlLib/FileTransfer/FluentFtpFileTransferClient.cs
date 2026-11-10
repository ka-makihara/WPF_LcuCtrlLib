using System.Diagnostics;
using System.IO;
using FluentFTP;

namespace WpfLcuCtrlLib;

public sealed class FluentFtpFileTransferClient : IFileTransferClient
{
	private readonly Action<string>? _log;

	public FluentFtpFileTransferClient(Action<string>? log = null)
	{
		_log = log;
	}

	public async Task<IFileTransferSession> ConnectAsync(
		string host,
		string user,
		string password,
		CancellationToken cancellationToken)
	{
		_log?.Invoke($"FTP connect to {host} started");
		var client = new AsyncFtpClient(host, user, password);
		try
		{
			await client.Connect(cancellationToken);
			_log?.Invoke($"FTP connected to {host}");
			return new FluentFtpFileTransferSession(client, _log);
		}
		catch (Exception exception)
		{
			client.Dispose();
			_log?.Invoke($"FTP connection to {host} failed: {exception.Message}");
			throw;
		}
	}

	private sealed class FluentFtpFileTransferSession(
		AsyncFtpClient client,
		Action<string>? log) : IFileTransferSession
	{
		public async Task CreateDirectoryAsync(string remoteDirectory, CancellationToken cancellationToken)
		{
			await RunLoggedAsync($"create directory {remoteDirectory}", () =>
				client.CreateDirectory(remoteDirectory, cancellationToken));
		}

		public async Task DeleteDirectoryAsync(string remoteDirectory, CancellationToken cancellationToken)
		{
			await RunLoggedAsync($"delete directory {remoteDirectory}", () =>
				client.DeleteDirectory(remoteDirectory, cancellationToken));
		}

		public async Task ClearStagingDirectoryAsync(CancellationToken cancellationToken)
		{
			const string root = "/MCFiles";
			cancellationToken.ThrowIfCancellationRequested();
			if (!await client.DirectoryExists(root, cancellationToken))
			{
				await CreateDirectoryAsync(root, cancellationToken);
				return;
			}
			var entries = await client.GetListing(root, cancellationToken);
			foreach (var entry in entries)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (entry.Name is "." or "..")
				{
					continue;
				}
				if (string.IsNullOrEmpty(entry.Name) || entry.Name.IndexOfAny(['/', '\\']) >= 0)
				{
					throw new IOException($"Invalid FTP staging entry: {entry.Name}");
				}
				string path = root + "/" + entry.Name;
				if (entry.Type == FtpObjectType.Directory)
					await DeleteDirectoryAsync(path, cancellationToken);
				else
					await RunLoggedAsync($"delete file {path}", () => client.DeleteFile(path, cancellationToken));
			}
			if ((await client.GetListing(root, cancellationToken)).Any(entry => entry.Name is not "." and not ".."))
			{
				throw new IOException("FTP staging cleanup did not empty /MCFiles.");
			}
		}

		public async Task<FileTransferResult> UploadFileAsync(
			string localFile,
			string remoteFile,
			CancellationToken cancellationToken)
		{
			log?.Invoke($"FTP upload {localFile} -> {remoteFile} started");
			try
			{
				var status = await client.UploadFile(
					localFile,
					remoteFile,
					FtpRemoteExists.Overwrite,
					createRemoteDir: true,
					token: cancellationToken);

				var result = new FileTransferResult(
					localFile,
					remoteFile,
					status == FtpStatus.Success,
					status == FtpStatus.Skipped);
				LogResult("upload", result);
				return result;
			}
			catch (Exception exception)
			{
				log?.Invoke($"FTP upload {localFile} -> {remoteFile} failed: {exception.Message}");
				throw;
			}
		}

		public async Task<IReadOnlyList<FileTransferResult>> UploadDirectoryAsync(
			string localDirectory,
			string remoteDirectory,
			CancellationToken cancellationToken)
		{
			log?.Invoke($"FTP upload directory {localDirectory} -> {remoteDirectory} started");
			try
			{
				var results = await client.UploadDirectory(
					localDirectory,
					remoteDirectory,
					FtpFolderSyncMode.Update,
					FtpRemoteExists.Overwrite,
					FtpVerify.OnlyVerify,
					null,
					null,
					cancellationToken);

				FileTransferResult[] transferResults = results
				.Select(result => new FileTransferResult(
					result.LocalPath,
					result.RemotePath,
					result.IsSuccess,
					result.IsSkipped))
				.ToArray();
				foreach (FileTransferResult result in transferResults)
				{
					LogResult("upload", result);
				}
				return transferResults;
			}
			catch (Exception exception)
			{
				log?.Invoke($"FTP upload directory {localDirectory} -> {remoteDirectory} failed: {exception.Message}");
				throw;
			}
		}

		public async Task<IReadOnlyList<FileTransferResult>> DownloadFilesAsync(
			IReadOnlyList<FileTransferRequest> files,
			CancellationToken cancellationToken)
		{
			List<FileTransferResult> results = new(files.Count);
			foreach (var file in files)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					string? directory = Path.GetDirectoryName(file.LocalPath);
					if (!string.IsNullOrEmpty(directory))
					{
						Directory.CreateDirectory(directory);
					}

					var status = await client.DownloadFile(
						file.LocalPath,
						file.RemotePath,
						FtpLocalExists.Overwrite,
						token: cancellationToken);

					results.Add(new FileTransferResult(
						file.LocalPath,
						file.RemotePath,
						status == FtpStatus.Success,
						status == FtpStatus.Skipped));
					LogResult("download", results[^1]);
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					log?.Invoke($"FTP download {file.RemotePath} cancelled");
					throw;
				}
				catch (Exception exception)
				{
					Debug.WriteLine($"FTP download failed: {file.RemotePath}: {exception.Message}");
					log?.Invoke($"FTP download {file.RemotePath} failed: {exception.Message}");
					results.Add(new FileTransferResult(
						file.LocalPath,
						file.RemotePath,
						IsSuccess: false,
						IsSkipped: false));
				}
			}

			return results;
		}

		public async Task DisconnectAsync(CancellationToken cancellationToken)
		{
			try
			{
				if (client.IsConnected)
				{
					await client.Disconnect(cancellationToken);
					log?.Invoke($"FTP disconnected from {client.Host}");
				}
			}
			catch (Exception exception)
			{
				log?.Invoke($"FTP disconnect from {client.Host} failed: {exception.Message}");
				throw;
			}
			finally
			{
				client.Dispose();
			}
		}

		private async Task RunLoggedAsync(string operation, Func<Task> action)
		{
			log?.Invoke($"FTP {operation} started");
			try
			{
				await action();
				log?.Invoke($"FTP {operation} completed");
			}
			catch (Exception exception)
			{
				log?.Invoke($"FTP {operation} failed: {exception.Message}");
				throw;
			}
		}

		private void LogResult(string operation, FileTransferResult result)
		{
			string outcome = result.IsSuccess ? "succeeded" : result.IsSkipped ? "skipped" : "failed";
			log?.Invoke($"FTP {operation} {result.LocalPath} -> {result.RemotePath}: {outcome}");
		}
	}
}
