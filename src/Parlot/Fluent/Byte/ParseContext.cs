using System;
using System.Collections.Generic;

namespace Parlot.Fluent.Byte
{
    public partial class ParseContext : ScopeParseContext<byte, ParseContext>
    {

        protected ParseContext(ParseContext context, Scanner<byte> newScanner = null)
        : base(context, newScanner)
        {
        }

        public ParseContext(Scanner<byte> scanner, bool useNewLines = false)
        : base(scanner, useNewLines)
        {
        }

        public override ParseContext Scope(BufferSpan<byte> subBuffer = default)
        {
            if (subBuffer.Equals(null))
                return new ParseContext(this);
            return new ParseContext(this, new Scanner<byte>(subBuffer));
        }

        public static ParseContext Scan(Scanner<byte> scanner, bool useNewLines = false)
        {
            return new ParseContext(scanner, useNewLines);
        }
    }
}
