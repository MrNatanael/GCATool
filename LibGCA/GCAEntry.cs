using System;
using System.IO;
using LibGCA.IO;

namespace LibGCA;

public abstract class GCAEntry(uint crc, FileAttributes attributes, DateTime fileTime, long length, long decompressedLength, string path)
{
    public abstract byte[] GetBuffer();
    public abstract void CopyTo(Stream stream);

    public void ExtractToDirectory(DirectoryInfo rootDir)
    {
        string path = System.IO.Path.Combine(rootDir.FullName, Path).Replace("\\", System.IO.Path.DirectorySeparatorChar.ToString());
        FileInfo file = new(path);
        
        Directory.CreateDirectory(file.Directory!.FullName);

        File.WriteAllBytes(path, GetBuffer());
        File.SetAttributes(path, Attributes);
        File.SetCreationTimeUtc(path, FileTime.ToUniversalTime());
    }

    public uint Crc32 { get; } = crc;
    public FileAttributes Attributes { get; } = attributes;
    public DateTime FileTime { get; } = fileTime;
    public long Length { get; } = length;
    public long DecompressedLength { get; } = decompressedLength;
    public string Path { get; } = path;
}

internal class StreamGCAEntry(GCAStream s, long offset, uint crc, FileAttributes attributes, DateTime fileTime, long length, long decompressedLength, string path) : GCAEntry(crc, attributes, fileTime, length, decompressedLength, path)
{
    public override byte[] GetBuffer()
    {
        s.Seek(offset, SeekOrigin.Begin);
        using var ms = s.ReadExactly(DecompressedLength);
        return ms.ToArray();
    }

    public override void CopyTo(Stream stream)
    {
        s.Seek(offset, SeekOrigin.Begin);
        using var ms = s.ReadExactly(DecompressedLength);
        ms.CopyTo(stream);
    }
}

internal class FSGCAEntry(FileInfo file, string path) : GCAEntry(Utils.Crc32(file), file.Attributes,
    file.CreationTime.ToLocalTime(), file.Length, file.Length, path)
{
    public override byte[] GetBuffer() => File.ReadAllBytes(file.FullName);
    public override void CopyTo(Stream stream)
    {
        using var s = file.OpenRead();
        s.CopyTo(stream);
    }
}

internal class BufferGCAEntry(byte[] buffer, string path, DateTime fileTime, FileAttributes attributes = FileAttributes.Archive) : GCAEntry(Utils.Crc32(buffer), attributes, fileTime,
    buffer.LongLength, buffer.LongLength, path)
{
    public override byte[] GetBuffer() => buffer;
    public override void CopyTo(Stream stream)
    {
        using var ms = new MemoryStream(buffer);
        ms.CopyTo(stream);
    }
}