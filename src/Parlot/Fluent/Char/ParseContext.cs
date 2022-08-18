namespace Parlot.Fluent
{
    public class StringParseContext : ParseContextWithScanner<char>
    {
        /// <summary>
        /// Whether new lines are treated as normal chars or white spaces.
        /// </summary>
        /// <remarks>
        /// When <c>false</c>, new lines will be skipped like any other white space.
        /// Otherwise white spaces need to be read explicitely by a rule.
        /// </remarks>
        public bool UseNewLines { get; private set; }

        public StringParseContext(Scanner<char> scanner, bool useNewLines = false)
        : base(scanner)
        {
            UseNewLines = useNewLines;
        }

        public StringParseContext(string text, bool useNewLines = false)
        : this(new Scanner<char>(text.ToCharArray()), useNewLines)
        {
        }

    }
}
