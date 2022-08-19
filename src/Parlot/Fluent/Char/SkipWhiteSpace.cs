using Parlot.Compilation;
using Parlot.Rewriting;
using System.Linq.Expressions;

namespace Parlot.Fluent.Char
{
    public sealed class SkipWhiteSpace<T, TParseContext> : Parser<T, TParseContext, char>, ICompilable<TParseContext, char>, ISeekable<char>
    where TParseContext : ParseContextWithScanner<char>
    {

        private readonly Parser<T, TParseContext, char> _parser;

        internal static readonly bool canUseNewLines = typeof(StringParseContext).IsAssignableFrom(typeof(TParseContext));

        public SkipWhiteSpace(Parser<T, TParseContext, char> parser)
        {
            _parser = parser;
        }
        public override bool Serializable => _parser.Serializable;
        public override bool SerializableWithoutValue => _parser.SerializableWithoutValue;

        public bool CanSeek => _parser is ISeekable<char> seekable && seekable.CanSeek;

        public char[] ExpectedChars => _parser is ISeekable<char> seekable ? seekable.ExpectedChars : default;

        public bool SkipWhitespace => true;

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Position;

            // Use the scanner's logic to ignore whitespaces since it knows about multi-line grammars
            if (!canUseNewLines || ((StringParseContext)(object)context).UseNewLines)
                context.Scanner.SkipWhiteSpace();
            else
                context.Scanner.SkipWhiteSpaceOrNewLine();

            if (_parser.Parse(context, ref result))
            {
                return true;
            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, char> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

            var start = context.DeclarePositionVariable(result);

            var parserCompileResult = _parser.Build(context);

            result.Body.Add(
                Expression.Block(
                    parserCompileResult.Variables,
                    canUseNewLines ? Expression.IfThenElse(context.UseNewLines(), context.SkipWhiteSpace(), context.SkipWhiteSpaceOrNewLine()) : context.SkipWhiteSpace(),
                    Expression.Block(parserCompileResult.Body),
                    Expression.IfThenElse(
                        parserCompileResult.Success,
                        Expression.Block(
                            context.DiscardResult ? Expression.Empty() : Expression.Assign(value, parserCompileResult.Value),
                            Expression.Assign(success, Expression.Constant(true, typeof(bool)))
                            ),
                        context.ResetPosition(start)
                        )
                    )
                );

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<char> sb, T value)
        {
            if (sb.Length > 0)
                sb.Append(' ');

            return _parser.Serialize(sb, value);

        }
    }
}
