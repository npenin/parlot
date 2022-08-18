using Parlot.Compilation;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace Parlot.Fluent.Char
{
    public sealed class IntegerLiteral<TParseContext> : Parser<long, TParseContext, char>, ICompilable<TParseContext, char>
    where TParseContext : ParseContextWithScanner<char>
    {
        private readonly NumberStyles _numberOptions;

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => false;

        public IntegerLiteral(NumberStyles numberOptions = NumberStyles.Integer)
        {
            _numberOptions = numberOptions;
        }

        public override bool Parse(TParseContext context, ref ParseResult<long> result)
        {
            context.EnterParser(this);

            var reset = context.Scanner.Cursor.Position;
            var start = reset.Offset;

            if (context.Scanner.ReadDecimal(_numberOptions, CultureInfo.InvariantCulture.NumberFormat))
            {
                var end = context.Scanner.Cursor.Offset;

#if NETSTANDARD2_0
                var sourceToParse = context.Scanner.Buffer.SubBuffer(start, end - start).ToString();
#else
                var sourceToParse = context.Scanner.Buffer.SubBuffer(start, end - start).Span;
#endif

                if (long.TryParse(sourceToParse, _numberOptions, CultureInfo.InvariantCulture, out var value))
                {
                    result.Set(start, end, value);
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(reset);

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, char> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable<long, TParseContext>(result);

            var reset = context.DeclarePositionVariable(result);
            var start = context.DeclareOffsetVariable(result);

            // if (context.Scanner.ReadInteger())
            // {
            //    var end = context.Scanner.Cursor.Offset;
            //    NETSTANDARD2_0 var sourceToParse = context.Scanner.Buffer.Substring(start, end - start);
            //    NETSTANDARD2_1 var sourceToParse = context.Scanner.Buffer.AsSpan(start, end - start);
            //    success = long.TryParse(sourceToParse, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            // }
            //
            // if (!success)
            // {
            //    context.Scanner.Cursor.ResetPosition(begin);
            // }
            //

            var end = Expression.Variable(typeof(int), $"end{context.NextNumber}");
#if NETSTANDARD2_0
            var sourceToParse = Expression.Variable(typeof(string), $"sourceToParse{context.NextNumber}");
            var sliceExpression = Expression.Assign(sourceToParse, Expression.Call(Expression.Call(context.Buffer(), typeof(BufferSpan<char>).GetMethod("SubBuffer", new[] { typeof(int), typeof(int) }), start, Expression.Subtract(end, start)), typeof(BufferSpan<char>).GetMethod(nameof(BufferSpan<char>.ToString))));
            var tryParseMethodInfo = typeof(long).GetMethod(nameof(long.TryParse), new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(long).MakeByRefType() });
#else
            var sourceToParse = Expression.Variable(typeof(ReadOnlySpan<char>), $"sourceToParse{context.NextNumber}");
            var sliceExpression = Expression.Assign(sourceToParse, Expression.Property(Expression.Call(context.Buffer(), typeof(BufferSpan<char>).GetMethod("SubBuffer", new[] { typeof(int), typeof(int) }), start, Expression.Subtract(end, start)), typeof(BufferSpan<char>).GetProperty("Span")));
            var tryParseMethodInfo = typeof(long).GetMethod(nameof(long.TryParse), new[] { typeof(ReadOnlySpan<char>), typeof(NumberStyles), typeof(IFormatProvider), typeof(long).MakeByRefType()});
#endif

            // TODO: NETSTANDARD2_1 code path
            var block =
                Expression.IfThen(
                    context.ReadDecimal(_numberOptions, CultureInfo.InvariantCulture),
                    Expression.Block(
                        new[] { end, sourceToParse },
                        Expression.Assign(end, context.Offset()),
                        sliceExpression,
                        Expression.Assign(success,
                            Expression.Call(
                                tryParseMethodInfo,
                                sourceToParse,
                                Expression.Constant(_numberOptions),
                                Expression.Constant(CultureInfo.InvariantCulture),
                                value)
                            )
                    )
                );

            result.Body.Add(block);

            result.Body.Add(
                Expression.IfThen(
                    Expression.Not(success),
                    context.ResetPosition(reset)
                    )
                );

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<char> sb, long value)
        {
            sb.Append(value);
            return true;
        }
    }
}
