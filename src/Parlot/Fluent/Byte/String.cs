﻿using Parlot.Compilation;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent.Byte
{
    public sealed class String<TParseContext> : Parser<string, TParseContext, byte>
    // , ICompilable<TParseContext, byte>
    where TParseContext : ParseContextWithScanner<byte>
    {
        public override bool Serializable => captureParser.Serializable;
        public override bool SerializableWithoutValue => captureParser.SerializableWithoutValue;

        private Encoding encoding;
        private Parser<BufferSpan<byte>, TParseContext, byte> captureParser;

        public String(Parser<BufferSpan<byte>, TParseContext, byte> captureParser, Encoding encoding)
        {
            this.encoding = encoding;
            this.captureParser = captureParser;
        }

        public override bool Parse(TParseContext context, ref ParseResult<string> result)
        {
            context.EnterParser(this);

            var reset = context.Scanner.Cursor.Position;

            ParseResult<BufferSpan<byte>> buffer = new();

            if (captureParser.Parse(context, ref buffer))
            {
                result.Set(buffer.Start, buffer.End, encoding.GetString(buffer.Value.ToArray()));
                return true;
            }

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
            return !captureParser.Serialize(sb, encoding.GetBytes(value));
        }
    }
}
