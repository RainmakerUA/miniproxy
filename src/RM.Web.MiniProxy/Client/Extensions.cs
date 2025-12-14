using Microsoft.Extensions.Options;

namespace RM.Web.MiniProxy.Client;

public static class Extensions
{
	public static IHostApplicationBuilder AddClient(this IHostApplicationBuilder builder)
	{
		builder.Services.AddSingleton<IValidateOptions<ClientConfig>, ClientConfigValidator>();
		builder.Services.AddOptions<ClientConfig>()
						.BindConfiguration("Client")
						.ValidateOnStart();
		builder.Services.AddScoped<IClient, MiniClient>();

		return builder;
	}
}
