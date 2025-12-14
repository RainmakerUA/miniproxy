using System.Net;
using Microsoft.Extensions.Options;

namespace RM.Web.MiniProxy.Client;

public sealed class MiniClient : IClient
{
	private const int _maxRedirects = 16;

	private readonly ClientConfig _config;
	private readonly HttpClient _httpClient;

	public MiniClient(IOptions<ClientConfig> clientConfig)
	{
		_config = clientConfig.Value;

		var httpHandler = new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = _maxRedirects };

		if (!String.IsNullOrEmpty(_config.ProxyUrl))
		{
			var credentials = String.IsNullOrEmpty(_config.ProxyUser)
								? null
								: new NetworkCredential(_config.ProxyUser, _config.ProxyPassword);
			httpHandler.UseProxy = true;
			httpHandler.DefaultProxyCredentials = credentials;
			httpHandler.Proxy = new WebProxy(_config.ProxyUrl)
									{
										BypassProxyOnLocal = true,
									};

		}

		_httpClient = new HttpClient(httpHandler)
							{
								DefaultRequestVersion = HttpVersion.Version20,
								DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
							};

		if (_config.TimeoutSeconds.HasValue)
		{
			_httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds.Value);
		}

		if (!String.IsNullOrEmpty(_config.UserAgent))
		{
			try
			{
				_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);
			}
			catch (FormatException formatException)
			{
				throw new ClientException(ClientException.Source.Configuration, $"Error parsing 'userAgent': {formatException.Message}.", formatException);
			}
		}
	}

	public void Dispose()
	{
		_httpClient?.Dispose();
	}

	public ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}

	public async Task<string> GetStringAsync(string url)
	{
		if (String.IsNullOrEmpty(url))
		{
			throw new ClientException(ClientException.Source.Request, "The 'url' query parameter is required.");
		}

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			throw new ClientException(ClientException.Source.Request, "The 'url' must be a valid absolute URI.");
		}

		if (uri.Scheme is not "http" and not "https")
		{
			throw new ClientException(ClientException.Source.Request, "The 'url' must use HTTP or HTTPS scheme.");
		}

		HttpResponseMessage httpResponse;

		try
		{
			httpResponse = await _httpClient.GetAsync(uri);
		}
		catch (HttpRequestException ex)
		{
			throw new ClientException(ClientException.Source.Request, $"Failed to fetch the URL: {ex.Message}", ex);
		}
		catch (OperationCanceledException ex)
		{
			throw new ClientException(ClientException.Source.Request, $"Request timeout exceeded {_config.TimeoutSeconds} seconds.", ex);
		}

		if (!httpResponse.IsSuccessStatusCode)
		{
			throw new ClientException(ClientException.Source.Request, $"Failed to fetch the URL. HTTP Status: {(int) httpResponse.StatusCode} {httpResponse.ReasonPhrase}");
		}

		try
		{
			var maxResponseSizeBytesOrNull = _config.MaxResponseSizeBytes;
			var (maxContentSizeConfigured, maxContentSizeBytes) = (maxResponseSizeBytesOrNull.HasValue, maxResponseSizeBytesOrNull ?? 0L);
			var maxContentSizeMegabytes = maxContentSizeBytes / (1024 * 1024);
			var contentLength = httpResponse.Content.Headers.ContentLength;

			if (maxContentSizeConfigured && contentLength > maxContentSizeBytes)
			{
				throw new ClientException(ClientException.Source.Request, $"Response size exceeds maximum allowed size of {maxContentSizeMegabytes} MB.");
			}

			var response = await httpResponse.Content.ReadAsStringAsync();

			if (maxContentSizeConfigured && response.Length > maxContentSizeBytes)
			{
				throw new ClientException(ClientException.Source.Request, $"Response size exceeds maximum allowed size of {maxContentSizeMegabytes} MB.");
			}

			return response;
		}
		catch (Exception ex)
		{
			throw new ClientException($"Failed to read response content: {ex.Message}", ex);
		}
	}
}
