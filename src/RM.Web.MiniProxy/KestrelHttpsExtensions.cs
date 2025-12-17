namespace RM.Web.MiniProxy;

public static class KestrelHttpsExtensions
{
	public static WebApplicationBuilder UseListeningPorts(this WebApplicationBuilder builder, bool forDevelopment = false)
	{
		if (!forDevelopment && builder.Environment.IsDevelopment())
		{
			// Skip configuration for development environment
			return builder;
		}

		int? httpPort = Int32.TryParse(Environment.GetEnvironmentVariable("HTTP_PORT"), out var portValue)
							? portValue
							: null;
		int? httpsPort = Int32.TryParse(Environment.GetEnvironmentVariable("HTTPS_PORT"), out portValue)
							? portValue
							: null;

		builder.WebHost.ConfigureKestrel(
										options =>
										{
											if (httpPort.HasValue)
											{
												options.ListenAnyIP(httpPort.Value); 
											}

											if (httpsPort.HasValue)
											{
												var (certPath, certPassword) = LoadCertificateInfo("HTTPS_CERT_PATH", "HTTPS_CERT_PWD_FILE");
												options.ListenAnyIP(
													httpsPort.Value,
													listenOptions =>
													{
														listenOptions.UseHttps(certPath, certPassword);
													}
												);
											}
										}
		);

		return builder;
	}

	private static (string certPath, string? certPassword) LoadCertificateInfo(string certPathVar, string certPasswordFileVar)
	{
		var (certPath, certPasswordFile) =
		(
			Environment.GetEnvironmentVariable(certPathVar) ?? throw new ApplicationException($"ENV {certPathVar} not provided!"),
			Environment.GetEnvironmentVariable(certPasswordFileVar) ?? throw new ApplicationException($"ENV {certPasswordFileVar} not provided!")
		);

		string? certPassword = null;

		if (!String.IsNullOrEmpty(certPasswordFile) && File.Exists(certPasswordFile))
		{
			certPassword = File.ReadAllText(certPasswordFile).Trim();
		}

		return (certPath, certPassword ?? throw new ApplicationException($"File {certPasswordFile} does not contain password!"));
	}
}
