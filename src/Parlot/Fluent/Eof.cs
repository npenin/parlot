using Parlot.Compilation;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    /// <summary>
    /// Successful when the cursor is at the end of the string.
    /// </summary>
    public sealed class Eof<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompilable<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => true;

        public Eof(Parser<T, TParseContext, TChar> parser)
        {
            _parser = parser;
        }

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            if (_parser.Parse(context, ref result) && context.Scanner.Cursor.Eof)
            {
                return true;
            }

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

            // parse1 instructions
            // 
            // if (parser1.Success && context.Scanner.Cursor.Eof)
            // {
            //    value = parse1.Value;
            //    success = true;
            // }

            var parserCompileResult = _parser.Build(context);

            result.Body.Add(
                Expression.Block(
                    parserCompileResult.Variables,
                    Expression.Block(parserCompileResult.Body),
                    Expression.IfThen(
                        Expression.AndAlso(parserCompileResult.Success, context.Eof()),
                        Expression.Block(
                            context.DiscardResult
                                ? Expression.Empty()
                                : Expression.Assign(value, parserCompileResult.Value),
                            Expression.Assign(success, Expression.Constant(true, typeof(bool)))
                            )
                        )
                    )
                );

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            if (_parser != null)
                return _parser.Serialize(sb, value);
            return true;
        }
    }
}
