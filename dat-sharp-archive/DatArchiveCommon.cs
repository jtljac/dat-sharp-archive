namespace dat_sharp_archive;

/// <summary>
/// Common values for Dat Archive Files
/// </summary>
public static class DatArchiveCommon {
    /// <summary>The signature for DatArchive Files</summary>
    public static readonly byte[] Signature = [0xB1, 0x44, 0x41, 0x54, 0x41, 0x52, 0x43];

    /// <summary>The supported Dat archive file version</summary>
    public const byte Version = 0x02;
}