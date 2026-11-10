using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WpfLcuCtrlLib;

public sealed class HttpLcuWebApiClient : ILcuWebApiClient
{
	private static readonly HttpClient SharedHttpClient = new();
	private readonly HttpClient _httpClient;
	private readonly Uri _apiBaseUri;
	private readonly Action<string>? _log;

	public HttpLcuWebApiClient(
		string host,
		HttpClient? httpClient = null,
		Action<string>? log = null)
	{
		var handler = new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromSeconds(60) // サーバーのtimeout_keep_aliveより短く
		};
		_httpClient = httpClient ?? new HttpClient(handler);
		_apiBaseUri = new Uri($"http://{host}/LCUWeb/api/");
		_log = log;
	}

	public async Task<string> SendCommandAsync(
		string payload,
		CancellationToken cancellationToken)
	{
		Uri requestUri = new(_apiBaseUri, "lcuCommand");
		Stopwatch stopwatch = Stopwatch.StartNew();
		_log?.Invoke($"HTTP POST {requestUri} started (payload: {Encoding.UTF8.GetByteCount(payload)} bytes)");
		using var content = new StringContent(payload, Encoding.UTF8, "application/json");
		try
		{
			using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
			string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			_log?.Invoke(
				$"HTTP POST {requestUri} completed: {(int)response.StatusCode} {response.StatusCode}, "
				+ $"{Encoding.UTF8.GetByteCount(responseBody)} response bytes, {stopwatch.ElapsedMilliseconds} ms");
			return responseBody;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			_log?.Invoke($"HTTP POST {requestUri} cancelled after {stopwatch.ElapsedMilliseconds} ms");
			throw;
		}
		catch (Exception exception)
		{
			_log?.Invoke($"HTTP POST {requestUri} failed after {stopwatch.ElapsedMilliseconds} ms: {exception.Message}");
			throw;
		}
	}

	public async Task<string> GetAsync(
		string command,
		CancellationToken cancellationToken)
	{
		Uri requestUri = new(_apiBaseUri, command);
		Stopwatch stopwatch = Stopwatch.StartNew();
		_log?.Invoke($"HTTP GET {requestUri} started");
		try
		{
			using var response = await _httpClient.GetAsync(
				requestUri,
				HttpCompletionOption.ResponseHeadersRead,
				cancellationToken);

			string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			_log?.Invoke(
				$"HTTP GET {requestUri} completed: {(int)response.StatusCode} {response.StatusCode}, "
				+ $"{Encoding.UTF8.GetByteCount(responseBody)} response bytes, {stopwatch.ElapsedMilliseconds} ms");
			return responseBody;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			_log?.Invoke($"HTTP GET {requestUri} cancelled after {stopwatch.ElapsedMilliseconds} ms");
			throw;
		}
		catch (HttpRequestException exception)
		{
			Debug.WriteLine($"LCU WebAPI GET failed: {exception.Message}");
			_log?.Invoke($"HTTP GET {requestUri} failed after {stopwatch.ElapsedMilliseconds} ms: {exception.Message}");
			return "errorCode";
		}
	}
}
