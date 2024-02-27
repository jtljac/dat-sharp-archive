namespace dat_sharp_archive.Util;

/// <summary>
/// Exception for operations with an invalid archive file
/// </summary>
public class BadArchiveException : Exception {
    public BadArchiveException() { }
    public BadArchiveException(string? message) : base(message) { }
    public BadArchiveException(string? message, Exception? innerException) : base(message, innerException) { }
}