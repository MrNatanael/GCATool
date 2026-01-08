using System;
using System.Buffers.Binary;
using System.Data;
using System.IO;
using System.Text;

namespace LibGCA.IO;

public class GCAStream(Stream s, bool leaveOpen) : Stream
{
    public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);
    public byte[] ReadExactly(int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int br = Read(buffer, offset, count - offset);
            if (br == 0) throw new DataException("Could not fully read");
            
            offset += br;
        }

        return buffer;
    }

    public MemoryStream ReadExactly(long count)
    {
        const int CHUNK_SIZE = 10 * 2048;
        
        MemoryStream ms = new();
        byte[] buffer = new byte[Math.Min((int)count, CHUNK_SIZE)];
        while (count > 0)
        {
            int r = Math.Min((int)count, CHUNK_SIZE);
            int rb = Read(buffer, 0, r);

            count -= rb;
            ms.Write(buffer, 0, rb);
        }
        
        return ms;
    }
    public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);

    public ushort ReadU16() =>  BinaryPrimitives.ReadUInt16LittleEndian(this.ReadExactly(sizeof(ushort)));
    public void WriteU16(ushort u16) => this.WriteBinary(BinaryPrimitives.WriteUInt16LittleEndian, u16, sizeof(ushort)); 
    
    public uint ReadU32() => BinaryPrimitives.ReadUInt32LittleEndian(this.ReadExactly(sizeof(uint)));
    public void WriteU32(uint u32) => this.WriteBinary(BinaryPrimitives.WriteUInt32LittleEndian, u32, sizeof(uint));
    
    public int ReadI32() => BinaryPrimitives.ReadInt32LittleEndian(this.ReadExactly(sizeof(int)));
    public void WriteI32(int i32) => this.WriteBinary(BinaryPrimitives.WriteInt32LittleEndian, i32, sizeof(int));
    
    public ulong ReadU64() => BinaryPrimitives.ReadUInt64LittleEndian(this.ReadExactly(sizeof(ulong)));
    public void WriteU64(ulong u64) => this.WriteBinary(BinaryPrimitives.WriteUInt64LittleEndian, u64, sizeof(ulong));
    
    public long ReadI64() => BinaryPrimitives.ReadInt64LittleEndian(this.ReadExactly(sizeof(long)));
    public void WriteI64(long i64) => this.WriteBinary(BinaryPrimitives.WriteInt64LittleEndian, i64, sizeof(long));

    public string ReadPath()
    {
        ushort len = ReadU16();
        return Encoding.UTF8.GetString(ReadExactly(len));
    }
    public void WritePath(string path)
    {
        byte[] data = Encoding.UTF8.GetBytes(path);
        WriteU16((ushort)data.Length);
        Write(data, 0, data.Length);
    }
    
    public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);
    public override void SetLength(long value) => BaseStream.SetLength(value);
    public override void Flush() => BaseStream.Flush();
    
    protected override void Dispose(bool disposing)
    {
        if(disposing && !LeaveOpen) BaseStream.Dispose();
        base.Dispose(disposing);
    }

    delegate void Writer<in T>(Span<byte> buffer, T value);

    void WriteBinary<T>(Writer<T> writer, T value, int size)
    {
        byte[] buffer = new byte[size];
        writer(buffer, value);
        
        this.Write(buffer, 0, size);
    }

    public GCAStream() : this(new MemoryStream(), false) {}
    
    public Stream BaseStream { get; } = s;
    public bool LeaveOpen { get; } = leaveOpen;

    public override bool CanRead => BaseStream.CanRead;
    public override bool CanSeek => BaseStream.CanSeek;
    public override bool CanWrite => BaseStream.CanWrite;
    public override long Length => BaseStream.Length;
    public override long Position
    {
        get => BaseStream.Position; set => BaseStream.Position = value;
    }
}