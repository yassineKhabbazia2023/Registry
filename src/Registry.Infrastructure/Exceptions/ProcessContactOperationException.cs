namespace Infrastructure.Exceptions;

public class ProcessContactOperationException : Exception
{
    public ProcessContactOperationException()
    {
    }

    public ProcessContactOperationException(string message)
        : base(message)
    {
    }

    public ProcessContactOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
