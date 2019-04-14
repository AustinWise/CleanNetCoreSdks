using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
