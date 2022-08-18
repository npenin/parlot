namespace Parlot.Protobuf;

using Parlot;
using Parlot.Fluent;

public class VarUInt<TParseContext> : Parlot.Fluent.Parser<ulong, TParseContext, byte>
    where TParseContext : ParseContextWithScanner<byte>
{
    private const byte mask = 0x70;

    public override bool Serializable => true;

    public override bool SerializableWithoutValue => true;

    public override bool Parse(TParseContext context, ref ParseResult<ulong> result)
    {

        context.EnterParser(this);

        var reset = context.Scanner.Cursor.Position;
        var start = reset.Offset;

        ulong bytes = 0;
        var b = context.Scanner.ReadSingle();
        for (var n = 0; (b & mask) == mask && n < 8; n++)
        {
            if (n > 0)
            {
                bytes <<= 7;
                bytes |= b;
            }
            else
                bytes = b;
        }

        result.Set(start, context.Scanner.Cursor.Position.Offset, bytes);
        return true;

        // context.Scanner.Cursor.ResetPosition(reset);

        // return false;

    }

    public override bool Serialize(BufferSpanBuilder<byte> sb, ulong value)
    {
        throw new System.NotImplementedException();
    }
}