using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LibGCA.IO;

namespace LibGCA;

public class GCAFile : IDisposable, IEnumerable<GCAEntry>
{
    public void Add(FileInfo file, string path) => _entries.Add(new FSGCAEntry(file, path));
    public void Add(byte[] buffer, string path, DateTime fileTime, FileAttributes attributes = FileAttributes.Archive) => _entries.Add(new BufferGCAEntry(buffer, path, fileTime, attributes));
    public GCAEntry GetEntry(int index) => _entries[index];
    public void RemoveEntry(int index) => _entries.RemoveAt(index);

    public void Save(FileInfo outFile)
    {
        using var s = outFile.OpenWrite();
        Save(s);
    }

    public void Save(string path)
    {
        using var s = File.OpenWrite(path);
        Save(s);
    }
    public void Save(Stream s)
    {
        if(s is GCAStream gca) new GCAWriter(_entries).Write(gca);
        else
        {
            using var gca2 = new GCAStream(s, true);
            new GCAWriter(_entries).Write(gca2);
        }
    }

    public IEnumerator<GCAEntry> GetEnumerator() => _entries.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public void Dispose() => _reader?.Dispose();
    
    public static GCAFile FromFile(string path) => FromStream(File.OpenRead(path), false);
    public static GCAFile FromFile(FileInfo file) => FromStream(file.OpenRead(), false);
    public static GCAFile FromStream(Stream stream, bool leaveOpen) => new GCAFile(new GCAStream(stream, leaveOpen));

    public static GCAFile FromDirectory(DirectoryInfo directory, bool includeRootName)
    {
        var gca = new GCAFile();
        foreach (var e in directory.GetFiles("*", SearchOption.AllDirectories))
        {
            string path = Utils.GetRelativePath(directory.FullName, e.FullName);
            if (includeRootName) path = Path.Combine(directory.Name, path);
            
            gca._entries.Add(new FSGCAEntry(e, path));
        }

        return gca;
    }
    
    protected GCAFile() {}
    public GCAFile(GCAStream stream)
    {
        if (stream.ReadU64() != GCAX)
            throw new FormatException("Invalid GCA header");

        long metaOffset = stream.ReadI64();
        long dataOffset = stream.Position;
        
        stream.Seek(metaOffset, SeekOrigin.Begin);
        if(stream.ReadU64() != GCA3)
            throw new  FormatException("Invalid file table header");

        /*
         * Skip
         * Table length (8)
         * Unknown (4)
         * Crc32? (4)
         */
        stream.Seek(16, SeekOrigin.Current);
        long fileCount = stream.ReadI64();

        const byte GCA_ENTRY_LENGTH = 32;
        
        long arrayStart = stream.Position;
        long nameTableStart = stream.Position + fileCount * GCA_ENTRY_LENGTH;
        for (long i = 0; i < fileCount; i++)
        {
            stream.Seek(arrayStart, SeekOrigin.Begin);
            uint crc = stream.ReadU32();
            FileAttributes attr = (FileAttributes)stream.ReadI32();
            long time = stream.ReadI64();
            long length = stream.ReadI64();
            long decompressedLength = stream.ReadI64();
            
            arrayStart = stream.Position;
            
            stream.Seek(nameTableStart, SeekOrigin.Begin);
            string path = stream.ReadPath();
            nameTableStart = stream.Position;
            
            _entries.Add(new StreamGCAEntry(stream, dataOffset, crc, attr, Utils.FileTimeToDateTime(time), length, decompressedLength, path));
            dataOffset += length;
        }

        _reader = stream;
    }
    
    public int FileCount => _entries.Count;

    private readonly GCAStream _reader;
    private readonly List<GCAEntry> _entries = new();

    public const ulong GCAX = 0x58414347;
    public const ulong GCA3 = 0x33414347;
}