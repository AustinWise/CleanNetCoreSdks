namespace Austin.CleanDotNetSdks;

public class ExitException : Exception
{
    public ExitException(string message)
        : base(message)
    {
    }
}
