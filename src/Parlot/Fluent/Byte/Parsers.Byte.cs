
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Parlot.Fluent
{
    public static class ByteParsers<TParseContext>
    where TParseContext : ParseContextWithScanner<byte>
    {
        /// <summary>
        /// Builds a parser that return either of the first successful of the specified parsers.
        /// </summary>
        public static Parser<T, TParseContext, byte> OneOf<T>(params Parser<T, TParseContext, byte>[] parsers) => Parsers<TParseContext, byte>.OneOf(parsers);

        /// <summary>
        /// Builds a parser that looks for zero or many times a parser separated by another one.
        /// </summary>
        public static Parser<List<T>, TParseContext, byte> Separated<U, T>(Parser<U, TParseContext, byte> separator, Parser<T, TParseContext, byte> parser) => Parsers<TParseContext, byte>.Separated(separator, parser);

        /// <summary>
        /// Builds a parser that looks for zero or one time the specified parser.
        /// </summary>
        public static Parser<T, TParseContext, byte> ZeroOrOne<T>(Parser<T, TParseContext, byte> parser) => Parsers<TParseContext, byte>.ZeroOrOne(parser);

        /// <summary>
        /// Builds a parser that creates a scope usable in the specified parser.
        /// </summary>
        public static Parser<T, TParseContext2, byte> Scope<T, TParseContext2>(Parser<T, TParseContext2, byte> parser) where TParseContext2 : ScopeParseContext<byte, TParseContext2> => Parsers<TParseContext, byte>.Scope(parser);

        /// <summary>
        /// Builds a parser that looks for zero or many times the specified parser.
        /// </summary>
        public static Parser<List<T>, TParseContext, byte> ZeroOrMany<T>(Parser<T, TParseContext, byte> parser) => Parsers<TParseContext, byte>.ZeroOrMany(parser);

        /// <summary>
        /// Builds a parser that looks for one or many times the specified parser.
        /// </summary>
        public static Parser<List<T>, TParseContext, byte> OneOrMany<T>(Parser<T, TParseContext, byte> parser) => Parsers<TParseContext, byte>.OneOrMany(parser);

        /// <summary>
        /// Builds a parser that succeed when the specified parser fails to match.
        /// </summary>
        public static Parser<T, TParseContext, byte> Not<T>(Parser<T, TParseContext, byte> parser) => Parsers<TParseContext, byte>.Not(parser);

        /// <summary>
        /// Builds a parser that can be defined later one. Use it when a parser need to be declared before its rule can be set.
        /// </summary>
        public static Deferred<T, TParseContext, byte> Deferred<T>() => Parsers<TParseContext, byte>.Deferred<T>();

        /// <summary>
        /// Builds a parser than needs a reference to itself to be declared.
        /// </summary>
        public static Deferred<T, TParseContext, byte> Recursive<T>(Func<Deferred<T, TParseContext, byte>, Parser<T, TParseContext, byte>> parser) => Parsers<TParseContext, byte>.Recursive(parser);

        /// <summary>
        /// Builds a parser that matches the specified parser between two other ones.
        /// </summary>
        public static Parser<T, TParseContext, byte> Between<A, T, B>(Parser<A, TParseContext, byte> before, Parser<T, TParseContext, byte> parser, Parser<B, TParseContext, byte> after) => Parsers<TParseContext, byte>.Between(before, parser, after);

        /// <summary>
        /// Builds a parser that matches any bytes before a specific parser.
        /// </summary>
        public static Parser<BufferSpan<byte>, TParseContext, byte> AnyCharBefore<T>(Parser<T, TParseContext, byte> parser, bool canBeEmpty = false, bool failOnEof = false, bool consumeDelimiter = false) => Parsers<TParseContext, byte>.AnyCharBefore(parser, canBeEmpty, failOnEof, consumeDelimiter);

        /// <summary>
        /// Builds a parser that captures the output of another parser.
        /// </summary>
        public static Parser<BufferSpan<byte>, TParseContext, byte> Capture<T>(Parser<T, TParseContext> parser) => Parsers<TParseContext, byte>.Capture<T>(parser);

        /// <summary>
        /// Builds a parser that always succeeds.
        /// </summary>
        public static Parser<T, TParseContext, byte> Empty<T>() => Parsers<TParseContext, byte>.Empty<T>();

        /// <summary>
        /// Builds a parser that always succeeds.
        /// </summary>
        public static Parser<object, TParseContext, byte> Empty() => Parsers<TParseContext, byte>.Empty();

        /// <summary>
        /// Builds a parser that always succeeds.
        /// </summary>
        public static Parser<T, TParseContext, byte> Empty<T>(T value) => Parsers<TParseContext, byte>.Empty(value);

        /// <summary>
        /// Builds a parser that matches the specified byte.
        /// </summary>
        public static Parser<byte, TParseContext, byte> Char(byte c) => Parsers<TParseContext, byte>.Char(c);

        /// <summary>
        /// Builds a parser that parses double (8 bits).
        /// </summary>
        public static Parser<byte, TParseContext, byte> Byte() => new Byte.Numeric<byte, TParseContext>(1,
           static b => b[0], static v => new[] { v }
        );

        /// <summary>
        /// Builds a parser that parses double (8 bits).
        /// </summary>
        public static Parser<sbyte, TParseContext, byte> SByte() => new Byte.Numeric<sbyte, TParseContext>(1,
            static b => (sbyte)b[0], static v => new[] { (byte)v }
        );

        /// <summary>
        /// Builds a parser that parses double (64 bits).
        /// </summary>
        public static Parser<double, TParseContext, byte> Double() => new Byte.Numeric<double, TParseContext>(8,
            static b => BitConverter.ToDouble(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );

        /// <summary>
        /// Builds a parser that parses float (32 bits).
        /// </summary>
        public static Parser<float, TParseContext, byte> Float() => new Byte.Numeric<float, TParseContext>(4,
            static b => BitConverter.ToSingle(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );

        /// <summary>
        /// Builds a parser that parses short (16 bits).
        /// </summary>
        public static Parser<short, TParseContext, byte> Int16() => new Byte.Numeric<short, TParseContext>(2,
            static b => BitConverter.ToInt16(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );

        /// <summary>
        /// Builds a parser that parses int (32 bits).
        /// </summary>
        public static Parser<int, TParseContext, byte> Int32() => new Byte.Numeric<int, TParseContext>(4,
            static b => BitConverter.ToInt32(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );

        /// <summary>
        /// Builds a parser that parses long (64 bits).
        /// </summary>
        public static Parser<long, TParseContext, byte> Int64() => new Byte.Numeric<long, TParseContext>(8,
            static b => BitConverter.ToInt64(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );


        /// <summary>
        /// Builds a parser that parses unsigned short (16 bits).
        /// </summary>
        public static Parser<ushort, TParseContext, byte> UInt16() => new Byte.Numeric<ushort, TParseContext>(2,
            static b => BitConverter.ToUInt16(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );

        /// <summary>
        /// Builds a parser that parses unsigned int (32 bits).
        /// </summary>
        public static Parser<uint, TParseContext, byte> UInt32() => new Byte.Numeric<uint, TParseContext>(4,
            static b => BitConverter.ToUInt32(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );

        /// <summary>
        /// Builds a parser that parses unsigned long (64 bits).
        /// </summary>
        public static Parser<ulong, TParseContext, byte> UInt64() => new Byte.Numeric<ulong, TParseContext>(8,
            static b => BitConverter.ToUInt64(b.ToArray(), 0), static v => BitConverter.GetBytes(v)
        );

        /// <summary>
        /// Builds a parser that parses unsigned long (64 bits).
        /// </summary>
        public static Parser<string, TParseContext, byte> String(Parser<ulong, TParseContext, byte> length, System.Text.Encoding encoding = null) => new Byte.StringWithPrefixedLength<TParseContext>(length, encoding ?? System.Text.Encoding.UTF8);

        /// <summary>
        /// Builds a parser that parses unsigned long (64 bits).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parser<string, TParseContext, byte> String<T>(Parser<T, TParseContext, byte> length, System.Text.Encoding encoding = null)
            where T : IConvertible
        => String(length.Then(t => t.ToUInt64(null)), encoding);

    }
}