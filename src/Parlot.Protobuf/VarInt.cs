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
            var size = result.End - result.Start;

            result.Set(unsignedResult.Start, unsignedResult.End, unchecked((long)(unsignedResult.Value << 1)) ^ unchecked((long)((unsignedResult.Value >> (size * 8 - 1)))));
        }

        return true;

        // context.Scanner.Cursor.ResetPosition(reset);

        // return false;

    }

    public override bool Serialize(BufferSpanBuilder<byte> sb, long value)
    {
        throw new System.NotImplementedException();
    }
}