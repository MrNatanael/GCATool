using System;
using System.IO;

namespace LibGCA;

public static class Utils
{
    public static DateTime FileTimeToDateTime(long ftime) => DateTime.FromFileTimeUtc(ftime).ToLocalTime();
    public static long DateTimeToFileTime(DateTime time) => time.ToUniversalTime().ToFileTimeUtc();
    public static uint Crc32(byte[] buffer)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in buffer)
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }
    public static uint Crc32(FileInfo file) => Crc32(File.ReadAllBytes(file.FullName));

    public static string GetRelativePath(string root, string path)
    {
        Uri fromUri = new Uri(AppendDirectorySeparatorChar(root));
        Uri toUri   = new Uri(path);

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }
    static string AppendDirectorySeparatorChar(string path)
    {
        if (!Path.HasExtension(path) && !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            return path + Path.DirectorySeparatorChar;
        return path;
    }

    static Utils()
    {
        const uint poly = 0xEDB88320;
        Crc32Table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
            Crc32Table[i] = crc;
        }
    }

    private static readonly uint[] Crc32Table;
}