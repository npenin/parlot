using Parlot.Compilation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    /// <summary>
    /// Returns a new <see cref="Parser{U,TParseContext}" /> converting the input value of 
    /// type T to the output value of type U using a custom function.
    /// </summary>
    /// <typeparam name="T">The input parser type.</typeparam>
    /// <typeparam name="U">The output parser type.</typeparam>
    /// <typeparam name="TParseContext">The parse context type.</typeparam>
    /// <typeparam name="TChar">The char type.</typeparam>
    public sealed class ThenWithScanner<T, U, TParseContext, TChar> : Parser<U, TParseContext, TChar>, ICompilable<TParseContext>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Func<T, U> _transform1;
        private readonly Func<TParseContext, T, U> _transform2;
        private readonly Func<U, T> _transformBack1;
        private readonly Func<BufferSpanBuilder<TChar>, U, Parser<T, TParseContext, TChar>, bool> _transformBack2;
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => (_transform1 != null && _transformBack1 != null || _transform2 != null && _transformBack2 != null || _transform1 == null && _transform2 == null) && _parser.Serializable;
        public override bool SerializableWithoutValue => _transform1 == null && _transform2 == null && _parser.SerializableWithoutValue;

        public ThenWithScanner(Parser<T, TParseContext, TChar> parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public ThenWithScanner(Parser<T, TParseContext, TChar> parser, Func<T, U> action, Func<U, T> reverseAction)
        {
            _transform1 = action ?? throw new ArgumentNullException(nameof(action));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _transformBack1 = reverseAction;
        }

        public ThenWithScanner(Parser<T, TParseContext, TChar> parser, Func<T, U> action, Func<BufferSpanBuilder<TChar>, U, Parser<T, TParseContext, TChar>, bool> reverseAction)
        {
            _transform1 = action ?? throw new ArgumentNullException(nameof(action));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _transformBack2 = reverseAction;
        }
        public ThenWithScanner(Parser<T, TParseContext, TChar> parser, Func<TParseContext, T, U> action, Func<BufferSpanBuilder<TChar>, U, Parser<T, TParseContext, TChar>, bool> reverseAction)
        {
            _transform2 = action ?? throw new ArgumentNullException(nameof(action));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _transformBack2 = reverseAction;
        }
        public ThenWithScanner(Parser<T, TParseContext, TChar> parser, Func<TParseContext, T, U> action, Func<U, T> reverseAction)
        {
            _transform2 = action ?? throw new ArgumentNullException(nameof(action));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _transformBack1 = reverseAction;
        }

        public override bool Parse(TParseContext context, ref ParseResult<U> result)
        {
            context.EnterParser(this);

            var parsed = new ParseResult<T>();

            if (_parser.Parse(context, ref parsed))
            {
                if (_transform1 != null)
                {
                    result.Set(parsed.Start, parsed.End, _transform1(parsed.Value));
                }
                else if (_transform2 != null)
                {
                    result.Set(parsed.Start, parsed.End, _transform2(context, parsed.Value));
                }

                return true;
            }

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(U)));

            // parse1 instructions
            // 
            // if (parser1.Success)
            // {
            //    success = true;
            //    value = action(parse1.Value);
            // }

            var parserCompileResult = _parser.Build(context, requireResult: true);

            Expression transformation;

            if (_transform1 != null)
            {
                transformation = Expression.Invoke(Expression.Constant(_transform1), new[] { parserCompileResult.Value });
            }
            else if (_transform2 != null)
            {
                transformation = Expression.Invoke(Expression.Constant(_transform2), context.ParseContext, parserCompileResult.Value);
            }
            else
            {
                transformation = Expression.Default(typeof(U));
            }

            var block = Expression.Block(
                    parserCompileResult.Variables,
                    parserCompileResult.Body
                    .Append(
                        Expression.IfThen(
                            parserCompileResult.Success,
                            Expression.Block(
                                Expression.Assign(success, Expression.Constant(true, typeof(bool))),
                                context.DiscardResult
                                ? Expression.Empty()
                                : Expression.Assign(value, transformation)
                                )
                            )
                        )
                    );

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, U value)
        {
            if (_transformBack1 != null)
                return _parser.Serialize(sb, _transformBack1(value));
            else if (_transformBack2 != null)
                return _transformBack2(sb, value, _parser);
            else
                return _parser.Serialize(sb, (T)(object)value);
        }
    }

    /// <summary>
    /// Returns a new <see cref="Parser{U,TParseContext}" /> converting the input value of 
    /// type T to the output value of type U using a custom function.
    /// </summary>
    /// <typeparam name="T">The input parser type.</typeparam>
    /// <typeparam name="TParseContext">The parse context type.</typeparam>
    /// <typeparam name="TChar">The char type.</typeparam>
    public sealed class ThenWithScanner<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompilable<TParseContext>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Action<T> _action1;
        private readonly Action<TParseContext, T> _action2;
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => _parser.Serializable;
        public override bool SerializableWithoutValue => _parser.SerializableWithoutValue;

        public ThenWithScanner(Parser<T, TParseContext, TChar> parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public ThenWithScanner(Parser<T, TParseContext, TChar> parser, Action<T> action)
        {
            _action1 = action ?? throw new ArgumentNullException(nameof(action));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public ThenWithScanner(Parser<T, TParseContext, TChar> parser, Action<TParseContext, T> action)
        {
            _action2 = action ?? throw new ArgumentNullException(nameof(action));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            if (_parser.Parse(context, ref result))
            {
                if (_action1 != null)
                {
                    _action1(result.Value);
                }
                else if (_action2 != null)
                {
                    _action2(context, result.Value);
                }

                return true;
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
            //    value = action(parse1.Value);
            // }

            var parserCompileResult = _parser.Build(context, requireResult: true);

            Expression action;

            if (_action1 != null)
            {
                action = Expression.Invoke(Expression.Constant(_action1), new[] { parserCompileResult.Value });
            }
            else if (_action2 != null)
            {
                action = Expression.Invoke(Expression.Constant(_action2), context.ParseContext, parserCompileResult.Value);
            }
            else
            {
                action = Expression.Default(typeof(T));
            }

            var block = Expression.Block(
                    parserCompileResult.Variables,
                    parserCompileResult.Body
                    .Append(
                        Expression.IfThen(
                            parserCompileResult.Success,
                            Expression.Block(
                                Expression.Assign(success, Expression.Constant(true, typeof(bool))),
                                action,
                                context.DiscardResult
                                ? Expression.Empty()
                                : Expression.Assign(value, parserCompileResult.Value)
                                )
                            )
                        )
                    );

            result.Body.Add(block);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            return _parser.Serialize(sb, value);
        }
    }
}
