using System.Runtime.Serialization;

namespace Application.Exceptions
{
    public class BlobStorageOperationException : Exception
    {
        public BlobStorageOperationException()
        {
        }

        public BlobStorageOperationException(string? message) : base(message)
        {
        }

        public BlobStorageOperationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
