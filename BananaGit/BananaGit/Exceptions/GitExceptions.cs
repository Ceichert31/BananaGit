using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BananaGit.Exceptions
{
    internal class GitException : Exception
    {
        public GitException()
        {
        }

        public GitException(string? message) : base(message)
        {
        }

        public GitException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

    internal class RepoLocationException : GitException
    {
        public RepoLocationException()
        {
        }

        public RepoLocationException(string? message) : base(message)
        {
        }

        public RepoLocationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
    internal class InvalidRepoException : GitException
    {
        public InvalidRepoException()
        {
        }

        public InvalidRepoException(string? message) : base(message)
        {
        }

        public InvalidRepoException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
