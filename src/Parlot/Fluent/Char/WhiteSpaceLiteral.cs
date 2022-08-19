﻿using Parlot.Compilation;
using System.Linq.Expressions;

namespace Parlot.Fluent.Char
{
    public sealed class WhiteSpaceLiteral<TParseContext> : Parser<BufferSpan<char>, TParseContext, char>, ICompilable<TParseContext, char>
    where TParseContext : ParseContextWithScanner<char>
    {
        private readonly bool _includeNewLines;

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => true;

        public WhiteSpaceLiteral(bool includeNewLines)
        {
            _includeNewLines = includeNewLines;
        }

        public override bool Parse(TParseContext context, ref ParseResult<BufferSpan<char>> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Offset;

            if (_includeNewLines)
            {
                context.Scanner.SkipWhiteSpaceOrNewLine();
            }
            else
            {
                context.Scanner.SkipWhiteSpace();
            }

            var end = context.Scanner.Cursor.Offset;

            if (start == end)
            {
                return false;
            }

            result.Set(start, context.Scanner.Cursor.Offset, context.Scanner.Buffer.SubBuffer(start, end - start));

            return true;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, char> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(BufferSpan<char>)));

            var start = context.DeclareOffsetVariable(result);

            result.Body.Add(
                _includeNewLines
                    ? context.SkipWhiteSpaceOrNewLine()
                    : context.SkipWhiteSpace()
                );

            var end = context.DeclareOffsetVariable(result);

            result.Body.Add(
                Expression.Block(
                    Expression.IfThen(
                        Expression.NotEqual(start, end),
                        Expression.Assign(success, Expression.Constant(true, typeof(bool)))
                        ),
                    context.DiscardResult ? Expression.Empty() : Expression.Assign(value, context.SubBufferSpan(start, Expression.Subtract(end, start)))
                    )
                );

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<char> sb, BufferSpan<char> value)
        {
            if (value.Equals(null))
                sb.Append(value);
            else if (_includeNewLines)
                sb.Append(System.Environment.NewLine.ToCharArray());
            else
                sb.Append(' ');
            return true;
        }
    }
}