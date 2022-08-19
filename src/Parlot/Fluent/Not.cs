using Parlot.Compilation;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    public sealed class Not<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompilable<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => true;

        public Not(Parser<T, TParseContext, TChar> parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Position;

            if (!_parser.Parse(context, ref result))
            {
                return true;
            }

            context.Scanner.Cursor.ResetPosition(start);
            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            _ = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

            // var start = context.Scanner.Cursor.Position;

            var start = context.DeclarePositionVariable(result);

            var parserCompileResult = _parser.Build(context);

            // success = false;
            //
            // parser instructions
            // 
            // if (parser.succcess)
            // {
            //     context.Scanner.Cursor.ResetPosition(start);
            // }
            // else
            // {
            //     success = true;
            // }
            // 

            result.Body.Add(
                Expression.Block(
                    parserCompileResult.Variables,
                    Expression.Block(parserCompileResult.Body),
                    Expression.IfThenElse(
                        parserCompileResult.Success,
                        context.ResetPosition(start),
                        Expression.Assign(success, Expression.Constant(true, typeof(bool)))
                        )
                    )
                );

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            return true;
        }
    }
}
