using Parlot.Compilation;
using Parlot.Rewriting;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    public sealed class OneOrMany<T, TParseContext, TChar> : Parser<List<T>, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISeekable<TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => _parser.Serializable;
        public override bool SerializableWithoutValue => false;

        public OneOrMany(Parser<T, TParseContext, TChar> parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public bool CanSeek => _parser is ISeekable<TChar> seekable && seekable.CanSeek;

        public TChar[] ExpectedChars => _parser is ISeekable<TChar> seekable ? seekable.ExpectedChars : default;

        public bool SkipWhitespace => _parser is ISeekable<TChar> seekable && seekable.SkipWhitespace;

        public override bool Parse(TParseContext context, ref ParseResult<List<T>> result)
        {
            context.EnterParser(this);

            var parsed = new ParseResult<T>();

            if (!_parser.Parse(context, ref parsed))
            {
                return false;
            }

            var start = parsed.Start;
            var results = new List<T>();

            int end;

            do
            {
                end = parsed.End;
                results.Add(parsed.Value);

            } while (_parser.Parse(context, ref parsed));

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
            //
            // if (value.Count > 0)
            // {
            //     success = true;
            // }
            // 

            var parserCompileResult = _parser.Build(context);

            var breakLabel = Expression.Label("break");

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
                        Expression.IfThen(
                            context.Eof(),
                            Expression.Break(breakLabel)
                            )),
                    breakLabel)
            );

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, List<T> value)
        {
            foreach (var v in value)
                _parser.Serialize(sb, v);
            return true;
        }
    }
}
