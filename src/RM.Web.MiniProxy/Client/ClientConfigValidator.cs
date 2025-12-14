using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace RM.Web.MiniProxy.Client;

public partial class ClientConfigValidator : IValidateOptions<ClientConfig>
{
	private const int _maxUserAgentLength = 400;

	private static readonly Regex _invalidHeaderCharsRegex = InvalidHeaderCharsRegex();

	public ValidateOptionsResult Validate(string? name, ClientConfig options)
	{
		var errors = new List<string>();

		if (options.TimeoutSeconds <= 0)
		{
			errors.Add("TimeoutSeconds must be greater than zero.");
		}

		var userAgent = options.UserAgent;

		if (!String.IsNullOrWhiteSpace(userAgent))
		{
			if (userAgent.Length > _maxUserAgentLength)
			{
				errors.Add("UserAgent exceeds maximum length.");
			}
			else if (_invalidHeaderCharsRegex.IsMatch(userAgent))
			{
				errors.Add("UserAgent contains invalid characters.");
			}
		}

        var proxyUrl = options.ProxyUrl;

        if (!String.IsNullOrWhiteSpace(proxyUrl))
		{
			if (!Uri.TryCreate(proxyUrl, UriKind.Absolute, out var proxyUri))
			{
				errors.Add("ProxyUrl must be a valid absolute URI.");
			}			
		}

		if (!String.IsNullOrWhiteSpace(options.ProxyUser)
			 && String.IsNullOrWhiteSpace(options.ProxyPassword))
		{
			errors.Add("ProxyPassword must be provided along with ProxyUser.");
		}

		switch (options.MaxResponseSizeBytes)
		{
			case null or > (1024 * 1024):
				break;

			case <= 0:
				errors.Add("MaxResponseSizeBytes must be greater than zero if specified.");
				break;

			default:
				errors.Add("MaxResponseSizeBytes must be at least 1 MB.");
				break;
		}

		return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
	}

	[GeneratedRegex(@"[\r\n\t\0]", RegexOptions.Compiled)]
	private static partial Regex InvalidHeaderCharsRegex();
}
