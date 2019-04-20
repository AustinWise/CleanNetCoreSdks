using System;

namespace Austin.CleanNetCoreSdks
{
    public class ExitException : Exception
    {
        public ExitException(string message)
            : base(message)
        {
        }
    }
}
