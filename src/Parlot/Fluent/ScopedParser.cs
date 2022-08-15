using System;
using System.Text;

namespace Parlot.Fluent
{
    /// <summary>
    /// Returns a new <see cref="Parser{T,TParseContext}" /> converting the input parser of 
    /// type T to a scoped parsed.
    /// </summary>
    /// <typeparam name="T">The input parser type.</typeparam>
    /// <typeparam name="TParseContext">The parser context type.</typeparam>
    /// <typeparam name="TChar">The char type.</typeparam>
    public sealed class ScopedParser<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>
    where TParseContext : ScopeParseContext<TChar, TParseContext>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly Action<TParseContext> _action;
        private readonly Parser<T, TParseContext, TChar> _parser;

        public override bool Serializable => _parser.Serializable;
        public override bool SerializableWithoutValue => _parser.SerializableWithoutValue;

        public ScopedParser(Action<TParseContext> action, Parser<T, TParseContext, TChar> parser)
        {
            _action = action;
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public ScopedParser(Parser<T, TParseContext, TChar> parser)
        : this(null, parser)
        {
        }

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);
            context = context.Scope();
            if (_action != null)
                _action(context);
            return _parser.Parse(context, ref result);
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            return _parser.Serialize(sb, value);
        }
    }
}
