using Parlot.Compilation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    public sealed class ZeroOrMany<T, TParseContext, TChar> : Parser<List<T>, TParseContext, TChar>, ICompilable<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => _parser.Serializable;
        public override bool SerializableWithoutValue => true;

        public ZeroOrMany(Parser<T, TParseContext, TChar> parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<List<T>> result)
        {
            context.EnterParser(this);

            var results = new List<T>();

            var start = 0;
            var end = 0;

            var first = true;
            var parsed = new ParseResult<T>();

            // TODO: it's not restoring an intermediate failed text position
            // is the inner parser supposed to be clean?

            while (_parser.Parse(context, ref parsed))
            {
                if (first)
                {
                    first = false;
                    start = parsed.Start;
                }

                end = parsed.End;
                results.Add(parsed.Value);
            }

            result = new ParseResult<List<T>>(start, end, results);
            return true;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            var _ = context.DeclareSuccessVariable(result, true);
            var value = context.DeclareValueVariable(result, Expression.New(typeof(List<T>)));

            // value = new List<T>();
            // success = true;
            //
            // while (true)
            // {
            //
            //   parse1 instructions
            // 
            //   if (parser1.Success)
            //   {
            //      results.Add(parse1.Value);
            //   }
            //   else
            //   {
            //      break;
            //   }
            //
            //   if (context.Scanner.Cursor.Eof)
            //   {
            //      break;
            //   }
            // }

            var parserCompileResult = _parser.Build(context);

            var breakLabel = Expression.Label("break");

            var block =
                Expression.Loop(
                    Expression.Block(
                        parserCompileResult.Variables,
                        Expression.Block(parserCompileResult.Body),
                        Expression.IfThenElse(
                            parserCompileResult.Success,
                            context.DiscardResult
                            ? Expression.Empty()
                            : Expression.Call(value, typeof(List<T>).GetMethod("Add"), parserCompileResult.Value),
                            Expression.Break(breakLabel)
                            ),
                        Expression.IfThen(
                            context.Eof<TParseContext, TChar>(),
                            Expression.Break(breakLabel)
                            )),
                    breakLabel
                    );

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, List<T> value)
        {
            if (value != null)
                foreach (var v in value)
                    _parser.Serialize(sb, v);
            return true;
        }
    }
}
