namespace Parlot.Protobuf;

using Parlot;
using Parlot.Fluent;

public class VarUInt<TParseContext> : Parlot.Fluent.Parser<ulong, TParseContext, byte>
    where TParseContext : ParseContextWithScanner<byte>
{
    private const byte overbyteMask = 0x80;
    private const byte mask = 0x7f;

    public override bool Serializable => true;

    public override bool SerializableWithoutValue => true;

    public override bool Parse(TParseContext context, ref ParseResult<ulong> result)
    {

        context.EnterParser(this);

        var reset = context.Scanner.Cursor.Position;
        var start = reset.Offset;

        if (!context.Scanner.TryReadSingle(out var b))
        {
            // System.Console.WriteLine("eof reached");
            return false;
        }
        // System.Console.WriteLine("read {0:X2}", b);
        ulong bytes = (ulong)(b & mask);
        for (var n = 1; (b & overbyteMask) == overbyteMask && n < 8; n++)
        {
            if (!context.Scanner.TryReadSingle(out b))
                break;
            // System.Console.WriteLine("read {0:X2}", b);
            bytes |= ((ulong)((byte)(b & mask)) << 7 * n);
        }
        // System.Console.WriteLine("final value: {0}", bytes);
        result.Set(start, context.Scanner.Cursor.Position.Offset, bytes);
        return true;

        // context.Scanner.Cursor.ResetPosition(reset);

        // return false;
    }

    public override bool Serialize(BufferSpanBuilder<byte> sb, ulong value)
    {
        // System.Console.WriteLine("serializing " + value);
        while (value > (value & mask))
        {
            sb.Append((byte)(value & mask | overbyteMask));
            value >>= 7;
            // System.Console.WriteLine("remaining " + value);
        }

        sb.Append((byte)(value & mask));
        return true;
    }
}