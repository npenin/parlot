
namespace Parlot.Fluent
{
    public partial class Parsers
    {
        /// <summary>
        /// Compiles the current parser.
        /// </summary>
        /// <returns>A compiled parser.</returns>
        public static Parser<T, StringParseContext, char> Compile<T>(this Parser<T, StringParseContext, char> self)
        {
            return self.Compile<T, StringParseContext, char>();
        }
        /// <summary>
        /// Compiles the current parser.
        /// </summary>
        /// <returns>A compiled parser.</returns>
        public static Parser<T, StringParseContext> Compile<T>(this Parser<T, StringParseContext> self)
        {
            return self.Compile<T, StringParseContext, char>();
        }
    }
}