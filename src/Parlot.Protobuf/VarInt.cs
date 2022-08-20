namespace Parlot.Protobuf;

using Parlot;
using Parlot.Fluent;

public class VarInt<TParseContext> : Parlot.Fluent.Parser<long, TParseContext, byte>
    where TParseContext : ParseContextWithScanner<byte>
{
    private VarUInt<TParseContext> uintParser = new();
    public override bool Serializable => true;

    public override bool SerializableWithoutValue => true;

    public override bool Parse(TParseContext context, ref ParseResult<long> result)
    {

        context.EnterParser(this);
        var unsignedResult = new ParseResult<ulong>();

        if (uintParser.Parse(context, ref unsignedResult))
        {
            var size = unsignedResult.End - unsignedResult.Start;
            long longValue = (long)unsignedResult.Value;
            System.Console.WriteLine($"{unsignedResult.Start},{unsignedResult.End}");
            result.Set(unsignedResult.Start, unsignedResult.End, (longValue >> 1) ^ (-(longValue & 1)));
        }

        return true;

        // context.Scanner.Cursor.ResetPosition(reset);

        // return false;

    }

    public override bool Serialize(BufferSpanBuilder<byte> sb, long value)
    {
        if (value < byte.MaxValue)
            return uintParser.Serialize(sb, unchecked((ulong)(value << 1 ^ value >> 7)));
        if (value < ushort.MaxValue)
            return uintParser.Serialize(sb, unchecked((ulong)(value << 1 ^ value >> 15)));
        if (value < uint.MaxValue)
            return uintParser.Serialize(sb, unchecked((ulong)(value << 1 ^ value >> 31)));
        if (value < long.MaxValue)
            return uintParser.Serialize(sb, unchecked((ulong)(value << 1 ^ value >> 63)));
        throw new System.ArithmeticException();
    }
}