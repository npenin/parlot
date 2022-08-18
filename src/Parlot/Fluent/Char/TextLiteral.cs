using Parlot.Compilation;
using System;
using System.Linq.Expressions;

namespace Parlot.Fluent.Char
{
    public sealed class TextLiteral<TParseContext> : Parser<string, TParseContext, char>, ICompilable<TParseContext, char>
    where TParseContext : ParseContextWithScanner<char>
    {
        private readonly StringComparer _comparer;

        public TextLiteral(string text, StringComparer comparer = null)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            _comparer = comparer;
        }

        public string Text { get; }

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => true;

        public override bool Parse(TParseContext context, ref ParseResult<string> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Offset;

            if (context.Scanner.ReadText(Text, _comparer))
            {
                result.Set(start, context.Scanner.Cursor.Offset, Text);
                return true;
            }

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, char> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(string)));

            // if (context.Scanner.ReadText(Text, _comparer, null))
            // {
            //      success = true;
            //      value = Text;
            // }
            //
            // [if skipWhiteSpace]
            // if (!success)
            // {
            //      resetPosition(beginning);
            // }

            var ifReadText = Expression.IfThen(
                Expression.Call(
null,
                    ExpressionHelper<TParseContext>.Scanner_ReadText_NoResult,
                    context.Scanner(),
                    Expression.Constant(Text, typeof(string)),
                    Expression.Constant(_comparer, typeof(StringComparer))
                    ),
                Expression.Block(
                    Expression.Assign(success, Expression.Constant(true, typeof(bool))),
                    context.DiscardResult
                    ? Expression.Empty()
                    : Expression.Assign(value, Expression.Constant(Text, typeof(string)))
                    )
                );

            result.Body.Add(ifReadText);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<char> sb, string value)
        {
            if (value == null || (_comparer != null && _comparer.Equals(value, Text)) || (_comparer == null && value == Text))
            {
#if SUPPORTS_READONLYSPAN
                sb.Append((ReadOnlySpan<char>)Text);
#else
                sb.Append(Text.ToCharArray());
#endif
                return true;
            }
            return false;
        }
    }
}
