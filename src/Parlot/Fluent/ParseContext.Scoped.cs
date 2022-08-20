using System;
using System.Collections.Generic;

namespace Parlot.Fluent
{
    public abstract class ScopeParseContext<TChar, TParseContext> : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    where TParseContext : ParseContextWithScanner<TChar>
    {
        protected TParseContext parent;

        public ScopeParseContext(TParseContext context, Scanner<TChar> newScanner = null)
        : this(newScanner ?? context.Scanner)
        {
            OnEnterParser = context.OnEnterParser;
            parent = context;
        }

        public ScopeParseContext(Scanner<TChar> scanner, bool useNewLines = false)
        : base(scanner)
        {
        }

        public abstract TParseContext Scope(BufferSpan<TChar> subBuffer = default);
    }
}
