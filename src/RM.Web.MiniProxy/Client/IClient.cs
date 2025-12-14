namespace RM.Web.MiniProxy.Client;

public interface IClient: IAsyncDisposable, IDisposable
{
	Task<string> GetStringAsync(string url);
}
