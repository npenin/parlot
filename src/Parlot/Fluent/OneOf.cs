using Parlot.Compilation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    /// <summary>
    /// OneOf the inner choices when all parsers return the same type.
    /// We then return the actual result of each parser.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TParseContext"></typeparam>
    /// <typeparam name="TChar"></typeparam>
    public sealed class OneOf<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompilable<TParseContext>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<T, TParseContext, TChar>[] _parsers;

        public OneOf(Parser<T, TParseContext, TChar>[] parsers)
        {
            _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
        }

        public Parser<T, TParseContext, TChar>[] Parsers => _parsers;

        public override bool Serializable => _parsers.All(p => p.Serializable);
        public override bool SerializableWithoutValue => false;

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            foreach (var parser in _parsers)
            {
                if (parser.Parse(context, ref result))
                {
                    return true;
                }
            }

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

            // parse1 instructions
            // 
            // if (parser1.Success)
            // {
            //    success = true;
            //    value = parse1.Value;
            // }
            // else
            // {
            //   parse2 instructions
            //   
            //   if (parser2.Success)
            //   {
            //      success = true;
            //      value = parse2.Value
            //   }
            //   
            //   ...
            // }


            Expression block = Expression.Empty();

            foreach (var parser in _parsers.Reverse())
            {
                var parserCompileResult = parser.Build(context);

                block = Expression.Block(
                    parserCompileResult.Variables,
                    Expression.Block(parserCompileResult.Body),
                    Expression.IfThenElse(
                        parserCompileResult.Success,
                        Expression.Block(
                            Expression.Assign(success, Expression.Constant(true, typeof(bool))),
                            context.DiscardResult
                            ? Expression.Empty()
                            : Expression.Assign(value, parserCompileResult.Value)
                            ),
                        block
                        )
                    );
            }

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            foreach (var parser in _parsers)
            {
                if (parser.Serialize(sb, value))
                    return true;
            }
            return false;
        }
    }
}
