namespace BananaGit.Exceptions
{
    internal class LoadDataException : GitException
    {
        public LoadDataException()
        {
        }

        public LoadDataException(string? message) : base(message)
        {
        }

        public LoadDataException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
