

```
Header {
    u8[7] signature   (Expected value: 0xB1444154415243, ±DATARC)
    u8  version       (Expected value: 0x02, 2)
    u64 tableOffset
}
```

```
CMethod: enum (u8) {
    NONE    value = 0
    ZLIB    value = 1
    BROTLI  value = 2
}
```

```
Flags: bitmap (u8) {
    unused      [7]
    unused      [6]
    unused      [5]
    unused      [4]
    unused      [3]
    unused      [2]
    unused      [1]
    encrypted   [0]
}
```

```
TableEntry {
    u16         nameLength
    u8          name[]              (Encoded in utf-8)
    CMethod     compressionMethod
    Flags       fileFlags
    u8[4]       crc32               (After compression)
    u64         originalSize        (Original size of the data, before compression, always set)
    u64         dataStart
    u64         dataEnd
}
```

```
File {
    Header          head
    u8[][]          data
    TableEntry[]    fileTable
}
```

# Description
The File is split into 3 parts:

## The Header
The header contains:
* signature: A signature to identify the filetype
* version: The version of the file standard
* tableOffset: The offset from the beginning of the file at which The File Table begins

## The Data:
The data section contains all the files that are in the archive, stored sequentially with no padding.
The boundaries and metadata of each file is stored in The File Table.

## The File Table:
The Data Table is a list of Table Entries, each of which represent a file stored in the data section.
Each Table entry contains:
* nameLength: The length of the name of the file
* name[]: The name of the file as utf-8 characters.
* compressionMethod: An enum representing how this file has been compressed, set to NONE (0) for no compression.
* fileFlags: Extra flags that may apply to the file
* crc32: A CRC32 checksum for the file. This will always be the CRC32 of the file before compression.
* originalSize: The original size of the file before compression, this is set regardless of whether the file is 
  compressed or not.
* dataStart: The offset from the beginning of the archive file at which the file begins
* dataEnd: The offset from the beginning of the archive file of the byte immediately following the final byte of the 
  file.

The Data Table must be in the same order as the files in the data section.

### Notes
* Due to the name having a variable length, each table entry is not a fixed size and thus cannot
  be looked up randomly. Therefore, decoding the data table must occur sequentially.
* Flags are read from right to left, where the rightmost bit is bit 0, encrypted
* In the current version of the spec, the only compression methods that are required is ZLIB and Brotli, this may change
  in the future.
* In the current version of the spec, the only file flag is ENCRYPTION (bit 0), this may change in the future.
* Files can exist in the body, but not be mentioned in the File Table, meaning it will not be accessible.