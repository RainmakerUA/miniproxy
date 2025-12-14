namespace RM.Web.MiniProxy.Client;

public class ClientConfig
{
	public int? TimeoutSeconds { get; set; }
	public string? UserAgent { get; set; }

	public string? ProxyUrl { get; set; }
	public string? ProxyUser { get; set; }
	public string? ProxyPassword { get; set; }

	public int? MaxResponseSizeBytes { get; set; }
}
