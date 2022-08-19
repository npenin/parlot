using Parlot.Fluent;
using System;

namespace Parlot.Compilation
{
    /// <summary>
    /// Marker interface to detect a Parser has already been compiled.
    /// </summary>
    public interface ICompiledParser
    {

    }

    /// <summary>
    /// An instance of this class encapsulates the result of a compiled parser
    /// in order to expose is as as standard parser contract.
    /// </summary>
    /// <remarks>
    /// This class is used in <see cref="Parsers.Compile{T, TParseContext,TChar}(Parser{T,TParseContext, TChar})"/>.
    /// </remarks>
    public class CompiledParser<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompiledParser
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Func<TParseContext, ValueTuple<bool, T>> _parse;

        public Parser<T, TParseContext, TChar> Source { get; }

        public CompiledParser(Func<TParseContext, ValueTuple<bool, T>> parse, Parser<T, TParseContext, TChar> source)
        {
            _parse = parse ?? throw new ArgumentNullException(nameof(parse));
            Source = source;
        }

        public override bool Serializable => false;
        public override bool SerializableWithoutValue => false;

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            var cursor = context.Scanner.Cursor;
            var start = cursor.Offset;
            var parsed = _parse(context);

            if (parsed.Item1)
            {
                result.Set(start, cursor.Offset, parsed.Item2);
                return true;
            }

            return false;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            throw new NotSupportedException();
        }
    }
}
