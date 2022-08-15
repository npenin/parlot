using Parlot.Compilation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    public sealed class Separated<U, T, TParseContext, TChar> : Parser<List<T>, TParseContext, TChar>, ICompilable<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<U, TParseContext, TChar> _separator;
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => _parser.Serializable && _separator.Serializable;
        public override bool SerializableWithoutValue => true;

        public Separated(Parser<U, TParseContext, TChar> separator, Parser<T, TParseContext, TChar> parser)
        {
            _separator = separator ?? throw new ArgumentNullException(nameof(separator));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<List<T>> result)
        {
            context.EnterParser(this);

            List<T> results = null;

            var start = 0;
            var end = 0;

            var first = true;
            var parsed = new ParseResult<T>();
            var separatorResult = new ParseResult<U>();

            while (true)
            {
                if (!_parser.Parse(context, ref parsed))
                {
                    if (!first)
                    {
                        break;
                    }

                    // A parser that returns false is reponsible for resetting the position.
                    // Nothing to do here since the inner parser is already failing and resetting it.
                    return false;
                }

                if (first)
                {
                    start = parsed.Start;
                }

                end = parsed.End;
                results ??= new List<T>();
                results.Add(parsed.Value);

                if (!_separator.Parse(context, ref separatorResult))
                {
                    break;
                }
            }

            result = new ParseResult<List<T>>(start, end, results);
            return true;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.New(typeof(List<T>)));

            // value = new List<T>();
            //
            // while (true)
            // {
            //   parse1 instructions
            // 
            //   if (parser1.Success)
            //   {
            //      success = true;
            //      value.Add(parse1.Value);
            //   }
            //   else
            //   {
            //      break;
            //   }
            //   
            //   parseSeparatorExpression with conditional break
            //
            //   if (context.Scanner.Cursor.Eof)
            //   {
            //      break;
            //   }
            // }
            // 

            var parserCompileResult = _parser.Build(context);
            var breakLabel = Expression.Label("break");

            var separatorCompileResult = _separator.Build(context);

            var block = Expression.Block(
                parserCompileResult.Variables,
                Expression.Loop(
                    Expression.Block(
                        Expression.Block(parserCompileResult.Body),
                        Expression.IfThenElse(
                            parserCompileResult.Success,
                            Expression.Block(
                                context.DiscardResult
                                ? Expression.Empty()
                                : Expression.Call(value, typeof(List<T>).GetMethod("Add"), parserCompileResult.Value),
                                Expression.Assign(success, Expression.Constant(true))
                                ),
                            Expression.Break(breakLabel)
                            ),
                        Expression.Block(
                            separatorCompileResult.Variables,
                            Expression.Block(separatorCompileResult.Body),
                            Expression.IfThen(
                                Expression.Not(separatorCompileResult.Success),
                                Expression.Break(breakLabel)
                                )
                            ),
                        Expression.IfThen(
                            context.Eof(),
                            Expression.Break(breakLabel)
                            )
                        ),
                    breakLabel)
                );

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, List<T> value)
        {
            var isFirst = true;
            if (value != null)
                foreach (var item in value)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        _separator.Serialize(sb, default);

                    _parser.Serialize(sb, item);
                }
            return value != null;
        }
    }
}
