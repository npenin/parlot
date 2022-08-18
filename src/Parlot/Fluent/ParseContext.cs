using System;

namespace Parlot.Fluent
{
    public partial class ParseContext
    {

        /// <summary>
        /// Delegate that is executed whenever a parser is invoked.
        /// </summary>
        public Action<object, ParseContext> OnEnterParser { get; set; }

        /// <summary>
        /// Called whenever a parser is invoked. Will be used to detect invalid states and infinite loops.
        /// </summary>
        public void EnterParser<T, TParseContext>(Parser<T, TParseContext> parser)
        where TParseContext : ParseContext
        {
            OnEnterParser?.Invoke(parser, this);
        }
    }
    public partial class ParseContextWithScanner<TChar> : ParseContext
    where TChar : IEquatable<TChar>, IConvertible
    {
        /// <summary>
        /// The scanner used for the parsing session.
        /// </summary>
        public readonly Scanner<TChar> Scanner;

        public ParseContextWithScanner(Scanner<TChar> scanner)
        {
            Scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        }
    }
}
