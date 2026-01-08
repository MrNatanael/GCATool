using System.Reflection;
using LibGCA;

if (args.Length < 3)
{
    string exePath = Assembly.GetExecutingAssembly().Location;
    string exeName = Path.GetFileNameWithoutExtension(exePath);

    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine($"  {exeName} --unpack <gca_file> <output_directory>");
    Console.Error.WriteLine($"  {exeName} --pack <input_directory> <gca_file>");
    return -1;
}

if (args[0].ToLowerInvariant() is "--unpack" or "-u")
{
    if (!File.Exists(args[1]))
    {
        Console.Error.WriteLine("GCA file does not exist.");
        return -2;
    }

    try
    {
        DirectoryInfo root = new(args[2]);
        Directory.CreateDirectory(root.FullName);
        
        using var gca = GCAFile.FromFile(args[1]);

        Console.Error.WriteLine($"Extracting {gca.FileCount} files...\n");
        foreach (var e in gca)
        {
            Console.Error.WriteLine($"  Extracting \"{e.Path}\" ({e.DecompressedLength} bytes)...");
            e.ExtractToDirectory(root);
        }
        
        Console.Error.WriteLine("\nFinished.");
    }
    catch (FormatException fmt)
    {
        Console.Error.WriteLine(fmt.Message);
        return -3;
    }
}

if (args[0].ToLowerInvariant() is "--pack" or "-p")
{
    if (!Directory.Exists(args[1]))
    {
        Console.Error.WriteLine("Directory does not exist.");
        return -2;
    }
    
    using var gca = GCAFile.FromDirectory(new DirectoryInfo(args[1]), false);
    Console.Error.WriteLine($"Packing {gca.FileCount} files...");
    gca.Save(args[2]);
}

return 0;