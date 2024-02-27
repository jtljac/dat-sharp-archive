using System.IO.Compression;
using System.Text;
using dat_sharp_archive.Util;

namespace dat_sharp_archive;

/// <summary>
/// A builder for writing new Dat Archive files
/// </summary>
public class DatArchiveWriter {
    /// <summary>
    /// A dictionary storing the entries that are being added to the file
    /// <para/>
    /// This maps the name of the archive to the archive entry. This is needed as file names must be unique.
    /// <para/>
    /// Note: This means files can be overwritten in the file table but left in the data
    /// </summary>
    private readonly Dictionary<string, DatArchiveEntry> _entries = new();

    /// <summary>The filestream to write to</summary>
    private readonly BinaryWriter _destFileStream;

    /// <summary>
    /// Create a archive file on the disk
    /// </summary>
    /// <param name="destFilePath">The path to create the archive file at</param>
    /// <param name="overwrite">If true, overwrite any existing files at the <paramref name="destFilePath"/></param>
    /// <exception cref="IOException">Thrown when overwrite is false and a file already exists at the <paramref name="destFilePath"/></exception>
    /// <exception cref="T:System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
    /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurred while creating the file.</exception>
    /// <exception cref="T:System.NotSupportedException"/>
    public DatArchiveWriter(string destFilePath, bool overwrite = false) {
        if (File.Exists(destFilePath) && !overwrite) throw new IOException("A file already exists at that path");
        _destFileStream = new BinaryWriter(File.Create(destFilePath));

        _destFileStream.Write(DatArchiveCommon.Signature);
        _destFileStream.Write(DatArchiveCommon.Version);
        _destFileStream.Write(0L);
    }

    ~DatArchiveWriter() {
        _destFileStream.Dispose();
    }

    /// <summary>
    /// Add a file to the the archive
    /// <para/>
    /// Note: You are responsible for disposing of the filestream passed to this method
    /// </summary>
    /// <param name="fileStream">The stream containing the file</param>
    /// <param name="fileEntry">An entry for the newly added file</param>
    /// <returns>The <see cref="DatArchiveWriter"/> instance</returns>
    public DatArchiveWriter AddFileEntry(Stream fileStream, DatArchiveEntry fileEntry) {
        fileEntry.dataStart = (ulong) _destFileStream.BaseStream.Position;
        var crcBuilder = new Crc32();
        // 4 KiB buffer
        var buffer = new byte[4096];
        int read;

        // Select stream to write with for compression
        var stream = fileEntry.compressionMethod switch {
            CompressionMethod.None => _destFileStream.BaseStream,
            CompressionMethod.ZLib => new ZLibStream(_destFileStream.BaseStream, CompressionMode.Compress, true),
            CompressionMethod.Brotli => new BrotliStream(_destFileStream.BaseStream, CompressionMode.Compress, true),
            _ => throw new IndexOutOfRangeException(nameof(fileEntry.compressionMethod))
        };

        while ((read = fileStream.Read(buffer, 0, buffer.Length)) != 0) {
            fileEntry.size += (ulong) read;
            crcBuilder.TransformBlock(buffer, 0, read, null, 0);
            stream.Write(buffer, 0, read);
        }

        // Only dispose if it's not the default stream
        if (fileEntry.compressionMethod != CompressionMethod.None) stream.Dispose();

        crcBuilder.TransformFinalBlock(buffer, 0, 0);
        fileEntry.crc32 = crcBuilder.Hash ?? [0, 0, 0, 0];

        fileEntry.dataEnd = (ulong) _destFileStream.BaseStream.Position;
        _entries[fileEntry.name] = fileEntry;

        return this;
    }

    /// <summary>
    /// Add a file to the archive
    /// </summary>
    /// <param name="filePath">A path to a file on the disk to add to the archive</param>
    /// <param name="fileEntry">An entry for the newly added file</param>
    /// <returns>The <see cref="DatArchiveWriter"/> instance</returns>
    public DatArchiveWriter AddFileEntry(string filePath, DatArchiveEntry fileEntry) {
        using var inputFile = File.OpenRead(filePath);
        return AddFileEntry(inputFile, fileEntry);
    }

    /// <summary>
    /// Add a file to the archive
    /// </summary>
    /// <param name="file">A buffer containing the file to add to the archive</param>
    /// <param name="fileEntry">An entry for the newly added file</param>
    /// <returns>The <see cref="DatArchiveWriter"/> instance</returns>
    public DatArchiveWriter AddFileEntry(byte[] file, DatArchiveEntry fileEntry) {
        using var inputFile = new MemoryStream(file, false);
        return AddFileEntry(inputFile, fileEntry);
    }

    /// <summary>
    /// Write the File Table to the archive file
    /// </summary>
    private void WriteFileTable() {
        foreach (var entry in _entries.Values) {
            // Name
            var nameBytes = Encoding.UTF8.GetBytes(entry.name);
            _destFileStream.Write((ushort) nameBytes.Length);
            _destFileStream.Write(nameBytes);

            _destFileStream.Write((byte) entry.compressionMethod);
            _destFileStream.Write((byte) entry.flags);

            _destFileStream.Write(entry.crc32);
            _destFileStream.Write(entry.size);
            _destFileStream.Write(entry.dataStart);
            _destFileStream.Write(entry.dataEnd);
        }
    }

    /// <summary>
    /// Finalise the archive file
    /// </summary>
    public void BuildArchive() {
        var fileTablePosition = _destFileStream.BaseStream.Position;

        WriteFileTable();

        _destFileStream.Seek(8, SeekOrigin.Begin);
        _destFileStream.Write(fileTablePosition);

        _destFileStream.Dispose();
    }
}