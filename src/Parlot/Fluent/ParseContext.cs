using System;

namespace Parlot.Fluent
{
    public partial class ParseContext
    {
        public static int DefaultCompilationThreshold = 0;

        /// <summary>
        /// The number of usages of the parser before it is compiled automatically. <c>0</c> to disable automatic compilation. Default is 0.
        /// </summary>
        public int CompilationThreshold { get; set; } = DefaultCompilationThreshold;

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
