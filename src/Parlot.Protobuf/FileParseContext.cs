namespace Parlot.Protobuf;

using Parlot;
using Parlot.Fluent;
using System.IO;

public class FileParseContext : StringParseContext
{
    public FileParseContext(string text, bool useNewLines = false) : base(text, useNewLines)
    {
        Protocol = new Protocol();
    }

    public FileParseContext(Scanner<char> scanner, bool useNewLines = false) : base(scanner, useNewLines)
    {
        Protocol = new Protocol();
    }

    public Protocol Protocol { get; }

    public string Path { get; private set; }

    public FileParseContext Import(string path)
    {
        if (!System.IO.Path.IsPathRooted(path))
            path = System.IO.Path.Combine(Path, path);
        return OpenFile(path);
    }

    public static FileParseContext OpenFile(string path)
    {
        return new FileParseContext(File.ReadAllText(path)) { Path = path };
    }
}