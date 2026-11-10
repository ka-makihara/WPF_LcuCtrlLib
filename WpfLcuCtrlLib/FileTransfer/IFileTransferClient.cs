namespace WpfLcuCtrlLib;

public interface IFileTransferClient
{
	Task<IFileTransferSession> ConnectAsync(
		string host,
		string user,
		string password,
		CancellationToken cancellationToken);
}

public interface IFileTransferSession
{
	Task CreateDirectoryAsync(string remoteDirectory, CancellationToken cancellationToken);

	Task DeleteDirectoryAsync(string remoteDirectory, CancellationToken cancellationToken);

	Task ClearStagingDirectoryAsync(CancellationToken cancellationToken);

	Task<FileTransferResult> UploadFileAsync(
		string localFile,
		string remoteFile,
		CancellationToken cancellationToken);

	Task<IReadOnlyList<FileTransferResult>> UploadDirectoryAsync(
		string localDirectory,
		string remoteDirectory,
		CancellationToken cancellationToken);

	Task<IReadOnlyList<FileTransferResult>> DownloadFilesAsync(
		IReadOnlyList<FileTransferRequest> files,
		CancellationToken cancellationToken);

	Task DisconnectAsync(CancellationToken cancellationToken);
}

public sealed record FileTransferRequest(string LocalPath, string RemotePath);

public sealed record FileTransferResult(
	string LocalPath,
	string RemotePath,
	bool IsSuccess,
	bool IsSkipped);
