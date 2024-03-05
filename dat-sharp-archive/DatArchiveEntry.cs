namespace dat_sharp_archive;

/// <summary>
/// An entry in the file table of a DatArchive File
/// </summary>
public class DatArchiveEntry {
    /// <summary>The name/path of the file in the archive</summary>
    public string Name { get; }

    /// <summary>The method used to compress the file</summary>
    public CompressionMethod CompressionMethod { get; }

    /// <summary>The flags of the file</summary>
    public ArchiveFlags Flags { get; }

    /// <summary>The Crc32 checksum of the file before compression</summary>
    public byte[] Crc32 { get; set; } = [0, 0, 0, 0];

    /// <summary>The size of the file before compression</summary>
    public ulong Size { get; set; }

    /// <summary>The offset from the beginning of the archive of the first byte of the file</summary>
    public ulong DataStart { get; set; }

    /// <summary>The offset from the beginning of the archive of the byte immediately after the file</summary>
    public ulong DataEnd { get; set; }

    /// <summary>The size of the file in the archive</summary>
    public ulong SizeInArchive => DataEnd - DataStart;

    /// <summary>
    ///  The archive reader that owns this entry
    /// </summary>
    public DatArchiveReader Archive { get; set; }

    /// <summary>Create a new archive file entry</summary>
    /// <param name="name">The name/path of the file inside the archive</param>
    /// <param name="compressionMethod">The method to compress the file</param>
    /// <param name="flags">The file flags</param>
    public DatArchiveEntry(string name, CompressionMethod compressionMethod, ArchiveFlags flags) {
        Name = name;
        CompressionMethod = compressionMethod;
        Flags = flags;
    }


    internal DatArchiveEntry(DatArchiveReader archive, string name, CompressionMethod compressionMethod, ArchiveFlags flags, byte[] crc32, ulong size, ulong dataStart, ulong dataEnd) {
        Archive = archive;
        Name = name;
        CompressionMethod = compressionMethod;
        Flags = flags;
        Crc32 = crc32;
        Size = size;
        DataStart = dataStart;
        DataEnd = dataEnd;
    }
}