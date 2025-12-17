using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RM.Web.MiniProxy;
using RM.Web.MiniProxy.Client;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
	var jsonOptions = options.SerializerOptions;

	jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

	jsonOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.UseListeningPorts();
builder.AddClient();

var app = builder.Build();

const int maxRegexMatches = 10_000;

// RE!: DisconSchedule\.(?'name'\w+)\s*=\s*(?'value'[^\n]+);?\n
// RE-ESC: DisconSchedule%5C.(%3F'name'%5Cw%2B)%5Cs*%3D%5Cs*(%3F'value'%5B%5E%5Cn%5D%2B)%3B%3F%5Cn

app.MapGet("/", static async ([AsParameters] FetchRequest request, IClient client) =>
{
	string response;

	try
	{
		response = await client.GetStringAsync(request.Url);
	}
	catch (ClientException clEx)
	{
		return clEx.ErrorSource switch
		{
			ClientException.Source.Configuration => ServerError($"Client configuration error: {clEx.Message}"),
			ClientException.Source.Request => BadRequest($"Failed to send request using Client: {clEx.Message}"),
			_ => ServerError($"Failed to retrieve data using Client: {clEx.Message}"),
		};
	}
	catch (Exception ex)
	{
		return ServerError($"Failed to retrieve data using Client: {ex.Message}");
	}

	var regexPattern = request.ParseRegex;

	if (String.IsNullOrEmpty(regexPattern))
	{
		return OkContent(response);
	}

	if (regexPattern.Length > 5000)
	{
		return BadRequest("The 'parseRegex' must not exceed 5000 characters.");
	}

	var matchValues = new List<IDictionary<string, string>>();

	try
	{
		var regex = new Regex(regexPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
		var matches = regex.Matches(response);

		if (matches.Count > maxRegexMatches)
		{
			return BadRequest($"Regex matched too many results (max: {maxRegexMatches}). Consider refining your pattern.");
		}

		foreach (Match match in matches)
		{
			if (match.Success)
			{
				var groups = match.Groups;
				var groupDict = new Dictionary<string, string>();

				foreach (string groupName in regex.GetGroupNames())
				{
					if (!String.Equals(groupName, "0", StringComparison.OrdinalIgnoreCase))
					{
						var group = groups[groupName];

						if (group.Success)
						{
							groupDict[groupName] = group.Value;
						} 
					}
				}

				matchValues.Add(groupDict);
			}
		}
	}
	catch (ArgumentException)
	{
		return BadRequest("The provided 'parseRegex' is not a valid regular expression.");
	}
	catch (RegexMatchTimeoutException)
	{
		return BadRequest("The provided 'parseRegex' took too long to compile.");
	}

	return OkMatches([.. matchValues]);

	static IResult OkContent(string? content) => Results.Ok(new FetchResponse(Content: content));

	static IResult OkMatches(IDictionary<string, string>[]? matches) => Results.Ok(new FetchResponse(Matches: matches));

	static IResult BadRequest(string? message) => Results.BadRequest(new FetchResponse(Error: message));

	static IResult ServerError(string? message) => Results.InternalServerError(new FetchResponse(Error: message));
});

app.Run();

public readonly record struct FetchRequest(string Url, string? ParseRegex = null, string? selector = null);

public readonly record struct FetchResponse(string? Content = null, IDictionary<string, string>[]? Matches = null, string? Error = null);

[JsonSerializable(typeof(FetchRequest))]
[JsonSerializable(typeof(FetchResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
