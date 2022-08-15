using System;
using System.Linq;

namespace Parlot.Fluent
{
    // We don't care about the performance of these helpers since they are called only once 
    // during the parser tree creation

    public static partial class Parsers
    {
        /// <summary>
        /// Builds a parser that return either of the first successful of the specified parsers.
        /// </summary>
        public static Parser<T, TParseContext, TChar> Or<T, TParseContext, TChar>(this Parser<T, TParseContext, TChar> parser, Parser<T, TParseContext, TChar> or)
        where TParseContext : ParseContextWithScanner<TChar>
        where TChar : IEquatable<TChar>, IConvertible
        {
            // We don't care about the performance of these helpers since they are called only once 
            // during the parser tree creation

            if (parser is OneOf<T, TParseContext, TChar> oneOf)
            {
                // Return a single OneOf instance with this new one
                return new OneOf<T, TParseContext, TChar>(oneOf.Parsers.Append(or).ToArray());
            }
            else
            {
                return new OneOf<T, TParseContext, TChar>(new[] { parser, or });
            }
        }

        /// <summary>
        /// Builds a parser that return either of the first successful of the specified parsers.
        /// </summary>
        public static Parser<T, TParseContext, TChar> Or<A, B, T, TParseContext, TChar>(this Parser<A, TParseContext, TChar> parser, Parser<B, TParseContext, TChar> or)
            where A : T
            where B : T
            where TParseContext : ParseContextWithScanner<TChar>
            where TChar : IEquatable<TChar>, IConvertible
        {
            return new OneOf<A, B, T, TParseContext, TChar>(parser, or);
        }
    }
}
