using Parlot.Compilation;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent.Byte
{
    public sealed class StringWithPrefixedLength<TParseContext> : Parser<string, TParseContext, byte>
    // , ICompilable<TParseContext, byte>
    where TParseContext : ParseContextWithScanner<byte>
    {
        public override bool Serializable => lengthParser.Serializable;
        public override bool SerializableWithoutValue => true;

        private Encoding encoding;
        private Parser<ulong, TParseContext, byte> lengthParser;

        public StringWithPrefixedLength(Parser<ulong, TParseContext, byte> lengthParser, Encoding encoding)
        {
            this.encoding = encoding;
            this.lengthParser = lengthParser;
        }

        public override bool Parse(TParseContext context, ref ParseResult<string> result)
        {
            context.EnterParser(this);

            var reset = context.Scanner.Cursor.Position;

            ParseResult<ulong> lengthResult = new();
            if (lengthParser.Parse(context, ref lengthResult))
            {
                if (context.Scanner.ReadN(lengthResult.Value, out var buffer))
                {
                    result.Set(lengthResult.Start, context.Scanner.Cursor.Position.Offset, encoding.GetString(buffer.ToArray()));
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(reset);

            return false;
        }

        // public CompilationResult Compile(CompilationContext<TParseContext, byte> context)
        // {
        //     var result = new CompilationResult();

        //     var success = context.DeclareSuccessVariable(result, false);
        //     var value = context.DeclareValueVariable<T, TParseContext>(result);

        //     var reset = context.DeclarePositionVariable(result);
        //     var start = context.DeclareOffsetVariable(result);

        //     // if (success=context.Scanner.ReadN(size, out var buffer))
        //     // {
        //     //     value=convert(buffer);
        //     //     return true;
        //     // }

        //     // if (!success)
        //     // {
        //     //    context.Scanner.Cursor.ResetPosition(begin);
        //     // }
        //     //
        //     var sourceToParse = Expression.Variable(typeof(BufferSpan<byte>), $"sourceToParse{context.NextNumber}");

        //     var block =
        //         Expression.IfThen(
        //             Expression.Assign(success, context.ReadN(size, sourceToParse)),
        //             Expression.Block(
        //                 new[] { sourceToParse },
        //                 Expression.Assign(value,
        //                     Expression.Call(
        //                         convert.Method,
        //                         sourceToParse
        //                     )
        //                 )
        //             )
        //         );

        //     result.Body.Add(block);

        //     result.Body.Add(
        //         Expression.IfThen(
        //             Expression.Not(success),
        //             context.ResetPosition(reset)
        //             )
        //         );

        //     return result;
        // }

        public override bool Serialize(BufferSpanBuilder<byte> sb, string value)
        {
            if (value == null)
                value = string.Empty;
            if (!lengthParser.Serialize(sb, unchecked((ulong)value.Length)))
                return false;
            sb.Append(encoding.GetBytes(value));
            return true;
        }
    }
}
