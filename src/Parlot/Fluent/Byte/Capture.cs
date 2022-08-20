using Parlot.Compilation;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent.Byte
{
    public sealed class CaptureWithPrefixedLength<TParseContext, TChar> : Parser<BufferSpan<TChar>, TParseContext, TChar>
    // , ICompilable<TParseContext, byte>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IConvertible, IEquatable<TChar>
    {
        public override bool Serializable => lengthParser.Serializable;
        public override bool SerializableWithoutValue => true;

        private Parser<ulong, TParseContext, TChar> lengthParser;

        public CaptureWithPrefixedLength(Parser<ulong, TParseContext, TChar> lengthParser)
        {
            this.lengthParser = lengthParser;
        }

        public override bool Parse(TParseContext context, ref ParseResult<BufferSpan<TChar>> result)
        {
            context.EnterParser(this);

            var reset = context.Scanner.Cursor.Position;

            ParseResult<ulong> lengthResult = new();
            if (lengthParser.Parse(context, ref lengthResult))
            {
                if (context.Scanner.ReadN((int)lengthResult.Value, out var buffer))
                {
                    result.Set(lengthResult.Start, context.Scanner.Cursor.Position.Offset, buffer.GetBuffer());
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

        public override bool Serialize(BufferSpanBuilder<TChar> sb, BufferSpan<TChar> value)
        {
            if (value.Equals(null))
                return false;
            if (!lengthParser.Serialize(sb, unchecked((ulong)value.Length)))
                return false;
            sb.Append(value);
            return true;
        }
    }
}
