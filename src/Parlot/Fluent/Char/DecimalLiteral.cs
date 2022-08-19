using Parlot.Compilation;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace Parlot.Fluent.Char
{
    public sealed class DecimalLiteral<TParseContext> : Parser<decimal, TParseContext, char>, ICompilable<TParseContext, char>
    where TParseContext : ParseContextWithScanner<char>
    {
        private readonly NumberStyles _numberOptions;
        private readonly CultureInfo _culture;

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => false;

        public DecimalLiteral(NumberStyles numberOptions = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo culture = null)
        {
            _numberOptions = numberOptions;
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        public override bool Parse(TParseContext context, ref ParseResult<decimal> result)
        {
            context.EnterParser(this);

            var reset = context.Scanner.Cursor.Position;
            var start = reset.Offset;

            if (context.Scanner.ReadDecimal(_numberOptions, _culture.NumberFormat))
            {
                var end = context.Scanner.Cursor.Offset;
#if !SUPPORTS_SPAN_PARSE
                var sourceToParse = context.Scanner.Buffer.SubBuffer(start, end - start).ToString();
#else
                var sourceToParse = context.Scanner.Buffer.SubBuffer(start, end - start).Span;
#endif

                if (decimal.TryParse(sourceToParse, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
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
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(decimal)));

            // var start = context.Scanner.Cursor.Offset;
            // var reset = context.Scanner.Cursor.Position;

            var start = context.DeclareOffsetVariable(result);
            var reset = context.DeclarePositionVariable(result);

            if (_numberOptions.HasFlag(NumberStyles.AllowLeadingSign))
            {
                // if (!context.Scanner.ReadChar('-'))
                // {
                //     context.Scanner.ReadChar('+');
                // }

                result.Body.Add(
                    Expression.IfThen(
                        Expression.Not(context.ReadChar('-')),
                        context.ReadChar('+')
                        )
                    );
            }

            // if (context.Scanner.ReadDecimal())
            // {
            //    var end = context.Scanner.Cursor.Offset;
            //    NETSTANDARD2_0 var sourceToParse = context.Scanner.Buffer.Substring(start, end - start);
            //    NETSTANDARD2_1 var sourceToParse = context.Scanner.Buffer.AsSpan(start, end - start);
            //    success = decimal.TryParse(sourceToParse, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            // }
            //
            // if (!success)
            // {
            //    context.Scanner.Cursor.ResetPosition(begin);
            // }
            //

            var end = Expression.Variable(typeof(int), $"end{context.NextNumber}");

            var sourceToParse = Expression.Variable(typeof(string), $"sourceToParse{context.NextNumber}");
            var sliceExpression = Expression.Assign(sourceToParse, Expression.Call(Expression.Call(context.Buffer(), typeof(BufferSpan<char>).GetMethod("SubBuffer", new[] { typeof(int), typeof(int) }), start, Expression.Subtract(end, start)), typeof(BufferSpan<char>).GetMethod(nameof(BufferSpan<char>.ToString))));
            var tryParseMethodInfo = typeof(decimal).GetMethod(nameof(decimal.TryParse), new[] { typeof(string), typeof(NumberStyles), typeof(IFormatProvider), typeof(decimal).MakeByRefType() });

            // TODO: NETSTANDARD2_1 code path
            var block =
                Expression.IfThen(
                    context.ReadDecimal(_numberOptions, _culture),
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

        public override bool Serialize(BufferSpanBuilder<char> sb, decimal value)
        {
            sb.Append(value.ToString().ToCharArray());
            return true;
        }
    }
}
