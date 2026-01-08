using System.Collections.Generic;
using System.IO;

namespace LibGCA.IO;

public class GCAWriter(IList<GCAEntry> entries)
{
    public void Write(GCAStream stream)
    {
        using var dataTable = new MemoryStream();
        using var metaTable = new GCAStream();
        using var nameTable = new GCAStream();
        
        foreach (var e in entries)
        {
            e.CopyTo(dataTable);
            
            metaTable.WriteU32(e.Crc32);
            metaTable.WriteI32((int)e.Attributes);
            metaTable.WriteI64(Utils.DateTimeToFileTime(e.FileTime));
            metaTable.WriteI64(e.Length);
            metaTable.WriteI64(e.DecompressedLength);
            
            nameTable.WritePath(e.Path);
        }
        
        dataTable.Position = 0;
        metaTable.Position = 0;
        nameTable.Position = 0;
        
        // Write data table
        stream.WriteU64(GCAFile.GCAX);
        stream.WriteI64(dataTable.Length + 16);

        dataTable.CopyTo(stream);
        
        // Write meta table
        stream.WriteU64(GCAFile.GCA3);
        stream.WriteI64(metaTable.Length + nameTable.Length + 32);
        stream.WriteU32(0x00); // TODO: Version?
        stream.WriteU32(0x00); // TODO: Flags or CRC32
        stream.WriteI64(entries.Count);
        
        metaTable.CopyTo(stream);
        nameTable.CopyTo(stream);
    }
}