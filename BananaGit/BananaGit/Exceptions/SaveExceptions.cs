using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
