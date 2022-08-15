using System;
using System.Runtime.CompilerServices;

namespace Parlot.Fluent
{
    public static partial class Parsers
    {

        /// <summary>
        /// Helper to serialize char based parsers to string.
        /// </summary>
        public static string Serialize<T, TParseContext>(this Parser<T, TParseContext, char> parser, T value)
            where TParseContext : ParseContextWithScanner<char>
        {
            var sb = new StringBuilder();
            if (parser.Serialize(sb, value))
                return sb.ToString();
            return null;
        }
        /// <summary>
        /// Helper to serialize char based parsers to string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Serialize<TParseContext>(this Parser<BufferSpan<char>, TParseContext, char> parser, string value)
            where TParseContext : ParseContextWithScanner<char> => parser.Serialize(value.ToCharArray());

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively.
        /// </summary>
        public static Parser<ValueTuple<T1, T2>, TParseContext, TChar> And<T1, T2, TParseContext, TChar>(this Parser<T1, TParseContext, TChar> parser, Parser<T2, TParseContext, TChar> and) where TParseContext : ParseContextWithScanner<TChar> where TChar : IEquatable<TChar>, IConvertible => new Sequence<T1, T2, TParseContext, TChar>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3>, TParseContext, TChar> And<T1, T2, T3, TParseContext, TChar>(this Parser<ValueTuple<T1, T2>, TParseContext, TChar> parser, Parser<T3, TParseContext, TChar> and) where TParseContext : ParseContextWithScanner<TChar> where TChar : IEquatable<TChar>, IConvertible => new Sequence<T1, T2, T3, TParseContext, TChar>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T4>, TParseContext, TChar> And<T1, T2, T3, T4, TParseContext, TChar>(this Parser<ValueTuple<T1, T2, T3>, TParseContext, TChar> parser, Parser<T4, TParseContext, TChar> and) where TParseContext : ParseContextWithScanner<TChar> where TChar : IEquatable<TChar>, IConvertible => new Sequence<T1, T2, T3, T4, TParseContext, TChar>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T4, T5>, TParseContext, TChar> And<T1, T2, T3, T4, T5, TParseContext, TChar>(this Parser<ValueTuple<T1, T2, T3, T4>, TParseContext, TChar> parser, Parser<T5, TParseContext, TChar> and) where TParseContext : ParseContextWithScanner<TChar> where TChar : IEquatable<TChar>, IConvertible => new Sequence<T1, T2, T3, T4, T5, TParseContext, TChar>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T4, T5, T6>, TParseContext, TChar> And<T1, T2, T3, T4, T5, T6, TParseContext, TChar>(this Parser<ValueTuple<T1, T2, T3, T4, T5>, TParseContext, TChar> parser, Parser<T6, TParseContext, TChar> and) where TParseContext : ParseContextWithScanner<TChar> where TChar : IEquatable<TChar>, IConvertible => new Sequence<T1, T2, T3, T4, T5, T6, TParseContext, TChar>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T4, T5, T6, T7>, TParseContext, TChar> And<T1, T2, T3, T4, T5, T6, T7, TParseContext, TChar>(this Parser<ValueTuple<T1, T2, T3, T4, T5, T6>, TParseContext, TChar> parser, Parser<T7, TParseContext, TChar> and) where TParseContext : ParseContextWithScanner<TChar> where TChar : IEquatable<TChar>, IConvertible => new Sequence<T1, T2, T3, T4, T5, T6, T7, TParseContext, TChar>(parser, and);

    }
}
