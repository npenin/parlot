namespace Parlot.Rewriting
{
    using Parlot.Fluent;
    using System;

    /// <summary>
    /// A Parser implementing this interface can be rewritten in a more optimized way.
    /// The result will replace the instance.
    /// </summary>
    public interface IRewritable<T, TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        /// <summary>
        /// Returns the parser to substitute.
        /// </summary>
        Parser<T, TParseContext, TChar> Rewrite();
    }
}
