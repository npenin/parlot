﻿using System;

namespace Parlot.Fluent
{
    public static partial class Parsers
    {

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively. The last parser's result is then ignored.
        /// </summary>
        public static Parser<T2, TParseContext> SkipAnd<T1, T2, TParseContext>(this IParser<T1, TParseContext> parser, IParser<T2, TParseContext> and) where TParseContext : ParseContext => new SequenceSkipAnd<T1, T2, TParseContext>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively. The last parser's result is then ignored.
        /// </summary>
        public static Parser<ValueTuple<T1, T3>, TParseContext> SkipAnd<T1, T2, T3, TParseContext>(this IParser<ValueTuple<T1, T2>, TParseContext> parser, IParser<T3, TParseContext> and) where TParseContext : ParseContext => new SequenceSkipAnd<T1, T2, T3, TParseContext>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively. The last parser's result is then ignored.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T4>, TParseContext> SkipAnd<T1, T2, T3, T4, TParseContext>(this IParser<ValueTuple<T1, T2, T3>, TParseContext> parser, IParser<T4, TParseContext> and) where TParseContext : ParseContext => new SequenceSkipAnd<T1, T2, T3, T4, TParseContext>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively. The last parser's result is then ignored.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T5>, TParseContext> SkipAnd<T1, T2, T3, T4, T5, TParseContext>(this IParser<ValueTuple<T1, T2, T3, T4>, TParseContext> parser, IParser<T5, TParseContext> and) where TParseContext : ParseContext => new SequenceSkipAnd<T1, T2, T3, T4, T5, TParseContext>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively. The last parser's result is then ignored.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T4, T6>, TParseContext> SkipAnd<T1, T2, T3, T4, T5, T6, TParseContext>(this IParser<ValueTuple<T1, T2, T3, T4, T5>, TParseContext> parser, IParser<T6, TParseContext> and) where TParseContext : ParseContext => new SequenceSkipAnd<T1, T2, T3, T4, T5, T6, TParseContext>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively. The last parser's result is then ignored.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T4, T5, T7>, TParseContext> SkipAnd<T1, T2, T3, T4, T5, T6, T7, TParseContext>(this IParser<ValueTuple<T1, T2, T3, T4, T5, T6>, TParseContext> parser, IParser<T7, TParseContext> and) where TParseContext : ParseContext => new SequenceSkipAnd<T1, T2, T3, T4, T5, T6, T7, TParseContext>(parser, and);

        /// <summary>
        /// Builds a parser that ensure the specified parsers match consecutively. The last parser's result is then ignored.
        /// </summary>
        public static Parser<ValueTuple<T1, T2, T3, T4, T5, T6, T8>, TParseContext> SkipAnd<T1, T2, T3, T4, T5, T6, T7, T8, TParseContext>(this IParser<ValueTuple<T1, T2, T3, T4, T5, T6, T7>, TParseContext> parser, IParser<T8, TParseContext> and) where TParseContext : ParseContext => new SequenceSkipAnd<T1, T2, T3, T4, T5, T6, T7, T8, TParseContext>(parser, and);
    }
}