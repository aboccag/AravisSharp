namespace AravisSharp;

/// <summary>
/// Exception thrown by Aravis operations
/// </summary>
public class AravisException : Exception
{
    public AravisException(string message) : base(message)
    {
    }

    public AravisException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
