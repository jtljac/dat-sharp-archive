using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Text;
using dat_sharp_archive.Util;

namespace dat_sharp_archive;

/// <summary>
/// A class for parsing DatArchive files
/// </summary>
public class DatArchiveReader {
    /// <summary>A memory mapped file representing the archive file being read</summary>
    private readonly MemoryMappedFile _archiveFile;

    /// <summary>
    /// A mapping of filenames to the entries in the archive file they represent
    /// </summary>
    private readonly Dictionary<string, DatArchiveEntry> _entries = new();

    /// <param name="archiveFilePath">A path to a archive file to read</param>
    public DatArchiveReader(string archiveFilePath) {
        _archiveFile = MemoryMappedFile.CreateFromFile(archiveFilePath);

        using var archiveStream = new BinaryReader(_archiveFile.CreateViewStream());
        ValidateArchive(archiveStream);

        var offset = archiveStream.ReadUInt64();
        ParseFileTable(archiveStream, (long) offset);
    }

    ~DatArchiveReader() {
        _archiveFile.Dispose();
    }

    /// <summary>
    /// Validate the file in the archive stream is in fact a DatArchive file and is the version supported by this library
    /// </summary>
    /// <param name="archiveReader">A reader for accessing the Archive File </param>
    /// <exception cref="EndOfStreamException">Thrown if the reader reaches the end of the archive unexpectedly</exception>
    /// <exception cref="BadArchiveException">
    /// Thrown if the file in the reader has the wrong signature, or is an incompatible archive file version
    /// </exception>
    private static void ValidateArchive(BinaryReader archiveReader) {
        var buffer = new byte[8];
        var read = archiveReader.Read(buffer, 0, 7);
        if (read != 7) throw new EndOfStreamException();

        if (!buffer.SkipLast(1).SequenceEqual(DatArchiveCommon.Signature))
            throw new BadArchiveException("Archive has incorrect signature");

        // Reuse buffer, why use more variables?
        buffer[0] = archiveReader.ReadByte();
        if (DatArchiveCommon.Version != buffer[0]) throw new BadArchiveException("Incompatible archive version");
    }

    /// <summary>
    /// Parse the Archive File Table
    /// <para/>
    /// This populates <see cref="_entries"/>
    /// </summary>
    /// <param name="archiveReader">A reader for accessing the Archive File</param>
    /// <param name="offset">The offset for the File Table from the beginning of the file</param>
    private void ParseFileTable(BinaryReader archiveReader, long offset) {
        archiveReader.BaseStream.Seek(offset, SeekOrigin.Begin);
        // Until end of file
        while (archiveReader.PeekChar() != -1) {
            var nameLength = archiveReader.ReadUInt16();
            var nameChars = archiveReader.ReadBytes(nameLength);
            var name = Encoding.UTF8.GetString(nameChars);

            var compressionMethod = (CompressionMethod) archiveReader.ReadByte();
            var flags = (ArchiveFlags) archiveReader.ReadByte();

            var crc = archiveReader.ReadBytes(4);

            var size = archiveReader.ReadUInt64();
            var start = archiveReader.ReadUInt64();
            var end = archiveReader.ReadUInt64();
            _entries[name] = new DatArchiveEntry(this, name, compressionMethod, flags, crc, size, start, end);
        }
    }

    /// <summary>
    /// Get a <see cref="DatArchiveEntry"/> from the archive
    /// </summary>
    /// <param name="name">The name/path of the file in the archive</param>
    /// <returns>The file entry with the given name, or null if there isn't one</returns>
    public DatArchiveEntry? GetFileEntry(string name) {
        return _entries.GetValueOrDefault(name);
    }

    /// <summary>
    /// Get a stream for accessing a file in the Archive
    /// </summary>
    /// <param name="fileEntry">The file entry for the file</param>
    /// <returns>A stream representing the file in the archive</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the fileEntry contains a bad <see cref="CompressionMethod"/> value
    /// </exception>
    /// <exception cref="BadArchiveException">Thrown if the file entry does not belong to this archive</exception>
    public Stream GetFile(DatArchiveEntry fileEntry) {
        if (fileEntry.archive != this)
            throw new BadArchiveException("That fileEntry does not belong to this Archive");

        var stream = _archiveFile.CreateViewStream((long) fileEntry.dataStart, (long) fileEntry.sizeInArchive);
        return fileEntry.compressionMethod switch {
            CompressionMethod.None => stream,
            CompressionMethod.ZLib => new ZLibStream(stream, CompressionMode.Decompress, false),
            CompressionMethod.Brotli => new BrotliStream(stream, CompressionMode.Decompress, false),
            _ => throw new IndexOutOfRangeException()
        };
    }

    /// <summary>
    /// Get a stream for accessing a file in the Archive
    /// </summary>
    /// <param name="name">The name/path of the file in the archive</param>
    /// <returns>A stream representing the file at the path in the archive, or null if there isn't one</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the fileEntry contains a bad <see cref="CompressionMethod"/> value
    /// </exception>
    public Stream? GetFile(string name) {
        return _entries.TryGetValue(name, out var entry) ? GetFile(entry) : null;
    }
}