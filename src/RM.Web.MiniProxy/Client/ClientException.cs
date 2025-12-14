namespace RM.Web.MiniProxy.Client;

public class ClientException : ApplicationException
{
    public new enum Source
    {
        Unknown = 0,
        Configuration,
        Request,
	}

	public ClientException(): this("An error occurred in the MiniProxy client.")
	{
    }

    public ClientException(string? message) : base(message)
    {
    }

    public ClientException(Source source, string? message) : this(message)
    {
        ErrorSource = source;
	}

    public ClientException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public ClientException(Source source, string? message, Exception? innerException) : this(message, innerException)
    {
        ErrorSource = source;
    }

    public Source ErrorSource
    {
        get;
        init
        {
            field = value;
            base.Source = value.ToString();
		}
    }
}
