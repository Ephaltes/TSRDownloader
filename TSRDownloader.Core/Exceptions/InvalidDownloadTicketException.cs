namespace TSRDownloader.Core.Exceptions;

/// <summary>Thrown when the download ticket is invalid or expired.</summary>
public class InvalidDownloadTicketException : Exception
{
    public InvalidDownloadTicketException(string itemId)
        : base($"Invalid download ticket for '{itemId}'.") { }
}
