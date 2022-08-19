using Parlot.Compilation;
using Parlot.Rewriting;
using System;
using System.Linq.Expressions;

namespace Parlot.Fluent
{
    public sealed class Between<A, T, B, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISeekable<TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<T, TParseContext, TChar> _parser;
        private readonly Parser<A, TParseContext, TChar> _before;
        private readonly Parser<B, TParseContext, TChar> _after;

        public override bool Serializable => _before.Serializable && _before.SerializableWithoutValue && _parser.Serializable && _after.Serializable && _after.SerializableWithoutValue;
        public override bool SerializableWithoutValue => _before.SerializableWithoutValue && _parser.SerializableWithoutValue && _after.SerializableWithoutValue;

        public Between(Parser<A, TParseContext, TChar> before, Parser<T, TParseContext, TChar> parser, Parser<B, TParseContext, TChar> after)
        {
            _before = before ?? throw new ArgumentNullException(nameof(before));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _after = after ?? throw new ArgumentNullException(nameof(after));
        }

        public bool CanSeek => _before is ISeekable<TChar> seekable && seekable.CanSeek;

        public TChar[] ExpectedChars => _before is ISeekable<TChar> seekable ? seekable.ExpectedChars : default;

        public bool SkipWhitespace => _before is ISeekable<TChar> seekable && seekable.SkipWhitespace;

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var cursor = context.Scanner.Cursor;

            var start = cursor.Position;

            var parsedA = new ParseResult<A>();

            if (!_before.Parse(context, ref parsedA))
            {
                // Don't reset position since _before should do it
                return false;
            }

            if (!_parser.Parse(context, ref result))
            {
                cursor.ResetPosition(start);
                return false;
            }

            var parsedB = new ParseResult<B>();

            if (!_after.Parse(context, ref parsedB))
            {
                cursor.ResetPosition(start);
                return false;
            }

            return true;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

            // start = context.Scanner.Cursor.Position;
            //
            // before instructions
            //
            // if (before.Success)
            // {
            //      parser instructions
            //      
            //      if (parser.Success)
            //      {
            //         after instructions
            //      
            //         if (after.Success)
            //         {
            //            success = true;
            //            value = parser.Value;
            //         }  
            //      }
            //
            //      if (!success)
            //      {  
            //          resetPosition(start);
            //      }
            // }

            var beforeCompileResult = _before.Build(context);
            var parserCompileResult = _parser.Build(context);
            var afterCompileResult = _after.Build(context);

            var start = context.DeclarePositionVariable(result);

            var block = Expression.Block(
                    beforeCompileResult.Variables,
                    Expression.Block(beforeCompileResult.Body),
                    Expression.IfThen(
                        beforeCompileResult.Success,
                        Expression.Block(
                            parserCompileResult.Variables,
                            Expression.Block(parserCompileResult.Body),
                            Expression.IfThen(
                                parserCompileResult.Success,
                                Expression.Block(
                                    afterCompileResult.Variables,
                                    Expression.Block(afterCompileResult.Body),
                                    Expression.IfThen(
                                        afterCompileResult.Success,
                                        Expression.Block(
                                            Expression.Assign(success, Expression.Constant(true, typeof(bool))),
                                            context.DiscardResult
                                            ? Expression.Empty()
                                            : Expression.Assign(value, parserCompileResult.Value)
                                            )
                                        )
                                    )
                                ),
                            Expression.IfThen(
                                Expression.Not(success),
                                context.ResetPosition(start)
                                )
                            )
                        )
                    );

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            _before.Serialize(sb, default);
            _parser.Serialize(sb, value);
            _after.Serialize(sb, default);
            return true;
        }
    }
}
