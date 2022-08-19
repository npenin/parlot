using Parlot.Compilation;
using Parlot.Rewriting;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    public sealed class ZeroOrOne<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISeekable<TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => _parser.Serializable;
        public override bool SerializableWithoutValue => true;

        public ZeroOrOne(Parser<T, TParseContext, TChar> parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public bool CanSeek => _parser is ISeekable<TChar> seekable && seekable.CanSeek;

        public TChar[] ExpectedChars => _parser is ISeekable<TChar> seekable ? seekable.ExpectedChars : default;

        public bool SkipWhitespace => _parser is ISeekable<TChar> seekable && seekable.SkipWhitespace;

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            _parser.Parse(context, ref result);

            return true;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, true);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

            // T value;
            //
            // parse1 instructions
            // 
            // if (parser1.Success)
            // {
            //    value parse1.Value;
            // }
            // 

            var parserCompileResult = _parser.Build(context);

            var block = Expression.Block(
                parserCompileResult.Variables,
                    Expression.Block(
                        Expression.Block(parserCompileResult.Body),
                        Expression.IfThen(
                            parserCompileResult.Success,
                            context.DiscardResult
                            ? Expression.Empty()
                            : Expression.Assign(value, parserCompileResult.Value)
                            )
                        )
                    );

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            if (value != null && !value.Equals(null))
                _parser.Serialize(sb, value);
            return true;
        }
    }
}
