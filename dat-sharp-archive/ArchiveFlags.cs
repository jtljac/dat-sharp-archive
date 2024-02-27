using System.Collections;

namespace dat_sharp_archive;

[Flags]
public enum ArchiveFlags : byte {
    None      = 0b_0000_0000,
    Encrypted = 0b_0000_0001
}