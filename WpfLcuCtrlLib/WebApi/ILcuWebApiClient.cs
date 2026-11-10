namespace WpfLcuCtrlLib;

public interface ILcuWebApiClient
{
	Task<string> SendCommandAsync(string payload, CancellationToken cancellationToken);

	Task<string> GetAsync(string command, CancellationToken cancellationToken);
}
