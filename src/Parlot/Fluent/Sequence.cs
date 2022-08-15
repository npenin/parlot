using Parlot.Compilation;
using System;
using System.Linq;
using System.Text;

namespace Parlot.Fluent
{
    public sealed class Sequence<T1, T2, TParseContext, TChar> : Parser<ValueTuple<T1, T2>, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISkippableSequenceParser<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        internal readonly Parser<T1, TParseContext, TChar> _parser1;
        internal readonly Parser<T2, TParseContext, TChar> _parser2;

        public override bool Serializable => _parser1.Serializable && _parser2.Serializable;
        public override bool SerializableWithoutValue => _parser1.SerializableWithoutValue && _parser2.SerializableWithoutValue;

        public Sequence(Parser<T1, TParseContext, TChar> parser1, Parser<T2, TParseContext, TChar> parser2)
        {
            _parser1 = parser1 ?? throw new ArgumentNullException(nameof(parser1));
            _parser2 = parser2 ?? throw new ArgumentNullException(nameof(parser2));
        }

        public override bool Parse(TParseContext context, ref ParseResult<ValueTuple<T1, T2>> result)
        {
            context.EnterParser(this);

            var parseResult1 = new ParseResult<T1>();

            var start = context.Scanner.Cursor.Position;

            if (_parser1.Parse(context, ref parseResult1))
            {
                var parseResult2 = new ParseResult<T2>();

                if (_parser2.Parse(context, ref parseResult2))
                {
                    result.Set(parseResult1.Start, parseResult2.End, new ValueTuple<T1, T2>(parseResult1.Value, parseResult2.Value));
                    return true;
                }

                context.Scanner.Cursor.ResetPosition(start);
            }

            return false;
        }

        public SkippableCompilationResult[] BuildSkippableParsers(CompilationContext<TParseContext, TChar> context)
        {
            return new[]
                {
                    new SkippableCompilationResult(_parser1.Build(context), false),
                    new SkippableCompilationResult(_parser2.Build(context), false)
                };
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            return SequenceCompileHelper.CreateSequenceCompileResult(BuildSkippableParsers(context), context);
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, (T1, T2) value)
        {
            return _parser1.Serialize(sb, value.Item1) &&
            _parser2.Serialize(sb, value.Item2);
        }
    }

    public sealed class Sequence<T1, T2, T3, TParseContext, TChar> : Parser<ValueTuple<T1, T2, T3>, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISkippableSequenceParser<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<ValueTuple<T1, T2>, TParseContext, TChar> _parser;
        internal readonly Parser<T3, TParseContext, TChar> _lastParser;

        public override bool Serializable => _parser.Serializable && _lastParser.Serializable;
        public override bool SerializableWithoutValue => _parser.SerializableWithoutValue && _lastParser.SerializableWithoutValue;

        public Sequence(Parser<ValueTuple<T1, T2>, TParseContext, TChar>
            parser,
            Parser<T3, TParseContext, TChar> lastParser
            )
        {
            _parser = parser;
            _lastParser = lastParser ?? throw new ArgumentNullException(nameof(lastParser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<ValueTuple<T1, T2, T3>> result)
        {
            context.EnterParser(this);

            var tupleResult = new ParseResult<ValueTuple<T1, T2>>();

            var start = context.Scanner.Cursor.Position;

            if (_parser.Parse(context, ref tupleResult))
            {
                var lastResult = new ParseResult<T3>();

                if (_lastParser.Parse(context, ref lastResult))
                {
                    var tuple = new ValueTuple<T1, T2, T3>(
                        tupleResult.Value.Item1,
                        tupleResult.Value.Item2,
                        lastResult.Value
                        );

                    result.Set(tupleResult.Start, lastResult.End, tuple);
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }

        public SkippableCompilationResult[] BuildSkippableParsers(CompilationContext<TParseContext, TChar> context)
        {
            if (_parser is not ISkippableSequenceParser<TParseContext, TChar> sequenceParser)
            {
                throw new InvalidOperationException(SequenceCompileHelper.SequenceRequired);
            }

            return sequenceParser.BuildSkippableParsers(context).Append(new SkippableCompilationResult(_lastParser.Build(context), false)).ToArray();
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            return SequenceCompileHelper.CreateSequenceCompileResult(BuildSkippableParsers(context), context);
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, (T1, T2, T3) value)
        {
            return _parser.Serialize(sb, new(value.Item1, value.Item2)) &&
            _lastParser.Serialize(sb, value.Item3);
        }
    }

    public sealed class Sequence<T1, T2, T3, T4, TParseContext, TChar> : Parser<ValueTuple<T1, T2, T3, T4>, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISkippableSequenceParser<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<ValueTuple<T1, T2, T3>, TParseContext, TChar> _parser;
        internal readonly Parser<T4, TParseContext, TChar> _lastParser;

        public override bool Serializable => _lastParser.Serializable && _parser.Serializable;
        public override bool SerializableWithoutValue => _lastParser.SerializableWithoutValue && _parser.SerializableWithoutValue;

        public Sequence(Parser<ValueTuple<T1, T2, T3>, TParseContext, TChar> parser, Parser<T4, TParseContext, TChar> lastParser)
        {
            _parser = parser;
            _lastParser = lastParser ?? throw new ArgumentNullException(nameof(lastParser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<ValueTuple<T1, T2, T3, T4>> result)
        {
            context.EnterParser(this);

            var tupleResult = new ParseResult<ValueTuple<T1, T2, T3>>();

            var start = context.Scanner.Cursor.Position;

            if (_parser.Parse(context, ref tupleResult))
            {
                var lastResult = new ParseResult<T4>();

                if (_lastParser.Parse(context, ref lastResult))
                {
                    var tuple = new ValueTuple<T1, T2, T3, T4>(
                        tupleResult.Value.Item1,
                        tupleResult.Value.Item2,
                        tupleResult.Value.Item3,
                        lastResult.Value
                        );

                    result.Set(tupleResult.Start, lastResult.End, tuple);
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }

        public SkippableCompilationResult[] BuildSkippableParsers(CompilationContext<TParseContext, TChar> context)
        {
            if (_parser is not ISkippableSequenceParser<TParseContext, TChar> sequenceParser)
            {
                throw new InvalidOperationException(SequenceCompileHelper.SequenceRequired);
            }

            return sequenceParser.BuildSkippableParsers(context).Append(new SkippableCompilationResult(_lastParser.Build(context), false)).ToArray();
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            return SequenceCompileHelper.CreateSequenceCompileResult(BuildSkippableParsers(context), context);
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, (T1, T2, T3, T4) value)
        {
            return _parser.Serialize(sb, new(value.Item1, value.Item2, value.Item3)) &&
             _lastParser.Serialize(sb, value.Item4);
        }
    }

    public sealed class Sequence<T1, T2, T3, T4, T5, TParseContext, TChar> : Parser<ValueTuple<T1, T2, T3, T4, T5>, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISkippableSequenceParser<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<ValueTuple<T1, T2, T3, T4>, TParseContext, TChar> _parser;
        internal readonly Parser<T5, TParseContext, TChar> _lastParser;

        public override bool Serializable => _lastParser.Serializable && _parser.Serializable;
        public override bool SerializableWithoutValue => _lastParser.SerializableWithoutValue && _parser.SerializableWithoutValue;

        public Sequence(Parser<ValueTuple<T1, T2, T3, T4>, TParseContext, TChar> parser, Parser<T5, TParseContext, TChar> lastParser)
        {
            _parser = parser;
            _lastParser = lastParser ?? throw new ArgumentNullException(nameof(lastParser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<ValueTuple<T1, T2, T3, T4, T5>> result)
        {
            context.EnterParser(this);

            var tupleResult = new ParseResult<ValueTuple<T1, T2, T3, T4>>();

            var start = context.Scanner.Cursor.Position;

            if (_parser.Parse(context, ref tupleResult))
            {
                var lastResult = new ParseResult<T5>();

                if (_lastParser.Parse(context, ref lastResult))
                {
                    var tuple = new ValueTuple<T1, T2, T3, T4, T5>(
                        tupleResult.Value.Item1,
                        tupleResult.Value.Item2,
                        tupleResult.Value.Item3,
                        tupleResult.Value.Item4,
                        lastResult.Value
                        );

                    result.Set(tupleResult.Start, lastResult.End, tuple);
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }

        public SkippableCompilationResult[] BuildSkippableParsers(CompilationContext<TParseContext, TChar> context)
        {
            if (_parser is not ISkippableSequenceParser<TParseContext, TChar> sequenceParser)
            {
                throw new InvalidOperationException(SequenceCompileHelper.SequenceRequired);
            }

            return sequenceParser.BuildSkippableParsers(context).Append(new SkippableCompilationResult(_lastParser.Build(context), false)).ToArray();
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            return SequenceCompileHelper.CreateSequenceCompileResult(BuildSkippableParsers(context), context);
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, (T1, T2, T3, T4, T5) value)
        {
            return _parser.Serialize(sb, new(value.Item1, value.Item2, value.Item3, value.Item4)) &&
            _lastParser.Serialize(sb, value.Item5);
        }
    }

    public sealed class Sequence<T1, T2, T3, T4, T5, T6, TParseContext, TChar> : Parser<ValueTuple<T1, T2, T3, T4, T5, T6>, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISkippableSequenceParser<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<ValueTuple<T1, T2, T3, T4, T5>, TParseContext, TChar> _parser;
        internal readonly Parser<T6, TParseContext, TChar> _lastParser;

        public override bool Serializable => _lastParser.Serializable && _parser.Serializable;
        public override bool SerializableWithoutValue => _lastParser.SerializableWithoutValue && _parser.SerializableWithoutValue;

        public Sequence(Parser<ValueTuple<T1, T2, T3, T4, T5>, TParseContext, TChar> parser, Parser<T6, TParseContext, TChar> lastParser)
        {
            _parser = parser;
            _lastParser = lastParser ?? throw new ArgumentNullException(nameof(lastParser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<ValueTuple<T1, T2, T3, T4, T5, T6>> result)
        {
            context.EnterParser(this);

            var tupleResult = new ParseResult<ValueTuple<T1, T2, T3, T4, T5>>();

            var start = context.Scanner.Cursor.Position;

            if (_parser.Parse(context, ref tupleResult))
            {
                var lastResult = new ParseResult<T6>();

                if (_lastParser.Parse(context, ref lastResult))
                {
                    var tuple = new ValueTuple<T1, T2, T3, T4, T5, T6>(
                        tupleResult.Value.Item1,
                        tupleResult.Value.Item2,
                        tupleResult.Value.Item3,
                        tupleResult.Value.Item4,
                        tupleResult.Value.Item5,
                        lastResult.Value
                        );

                    result.Set(tupleResult.Start, lastResult.End, tuple);
                    return true;
                }

            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }

        public SkippableCompilationResult[] BuildSkippableParsers(CompilationContext<TParseContext, TChar> context)
        {
            if (_parser is not ISkippableSequenceParser<TParseContext, TChar> sequenceParser)
            {
                throw new InvalidOperationException(SequenceCompileHelper.SequenceRequired);
            }

            return sequenceParser.BuildSkippableParsers(context).Append(new SkippableCompilationResult(_lastParser.Build(context), false)).ToArray();
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            return SequenceCompileHelper.CreateSequenceCompileResult(BuildSkippableParsers(context), context);
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, (T1, T2, T3, T4, T5, T6) value)
        {
            return _parser.Serialize(sb, new(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5)) &&
            _lastParser.Serialize(sb, value.Item6);

        }
    }

    public sealed class Sequence<T1, T2, T3, T4, T5, T6, T7, TParseContext, TChar> : Parser<ValueTuple<T1, T2, T3, T4, T5, T6, T7>, TParseContext, TChar>, ICompilable<TParseContext, TChar>, ISkippableSequenceParser<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Parser<ValueTuple<T1, T2, T3, T4, T5, T6>, TParseContext, TChar> _parser;
        internal readonly Parser<T7, TParseContext, TChar> _lastParser;

        public override bool Serializable => _lastParser.Serializable && _parser.Serializable;
        public override bool SerializableWithoutValue => _lastParser.SerializableWithoutValue && _parser.SerializableWithoutValue;

        public Sequence(Parser<ValueTuple<T1, T2, T3, T4, T5, T6>, TParseContext, TChar> parser, Parser<T7, TParseContext, TChar> lastParser)
        {
            _parser = parser;
            _lastParser = lastParser ?? throw new ArgumentNullException(nameof(lastParser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> result)
        {
            context.EnterParser(this);

            var tupleResult = new ParseResult<ValueTuple<T1, T2, T3, T4, T5, T6>>();

            var start = context.Scanner.Cursor.Position;

            if (_parser.Parse(context, ref tupleResult))
            {
                var lastResult = new ParseResult<T7>();

                if (_lastParser.Parse(context, ref lastResult))
                {
                    var tuple = new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(
                        tupleResult.Value.Item1,
                        tupleResult.Value.Item2,
                        tupleResult.Value.Item3,
                        tupleResult.Value.Item4,
                        tupleResult.Value.Item5,
                        tupleResult.Value.Item6,
                        lastResult.Value
                        );

                    result.Set(tupleResult.Start, lastResult.End, tuple);
                    return true;
                }

            }

            context.Scanner.Cursor.ResetPosition(start);

            return false;
        }

        public SkippableCompilationResult[] BuildSkippableParsers(CompilationContext<TParseContext, TChar> context)
        {
            if (_parser is not ISkippableSequenceParser<TParseContext, TChar> sequenceParser)
            {
                throw new InvalidOperationException(SequenceCompileHelper.SequenceRequired);
            }

            return sequenceParser.BuildSkippableParsers(context).Append(new SkippableCompilationResult(_lastParser.Build(context), false)).ToArray();
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            return SequenceCompileHelper.CreateSequenceCompileResult(BuildSkippableParsers(context), context);
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, (T1, T2, T3, T4, T5, T6, T7) value)
        {
            return _parser.Serialize(sb, new(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6))
             && _lastParser.Serialize(sb, value.Item7);
        }
    }
}
