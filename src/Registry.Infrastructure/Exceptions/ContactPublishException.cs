namespace Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when contact publish operations fail.
/// </summary>
public class ContactPublishException : Exception
{
    public ContactPublishException()
    {
    }

    public ContactPublishException(string message)
        : base(message)
    {
    }

    public ContactPublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}