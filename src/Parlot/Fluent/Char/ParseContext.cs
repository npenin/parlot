namespace Parlot.Fluent.Char
{
    using System;

    public class ParseContext : ParseContextWithScanner<char>
    {
        /// <summary>
        /// Whether new lines are treated as normal chars or white spaces.
        /// </summary>
        /// <remarks>
        /// When <c>false</c>, new lines will be skipped like any other white space.
        /// Otherwise white spaces need to be read explicitely by a rule.
        /// </remarks>
        public bool UseNewLines { get; private set; }

        public ParseContext(Scanner<char> scanner, bool useNewLines = false)
        : base(scanner)
        {
            UseNewLines = useNewLines;
        }

        public ParseContext(string text, bool useNewLines = false)
        : this(new Scanner<char>(text.AsSpan()), useNewLines)
        {
        }


    }
}
