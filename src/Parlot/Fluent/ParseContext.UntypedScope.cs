using System;
using System.Collections.Generic;

namespace Parlot.Fluent
{
    public partial class ParseContext
    {
        public class Untyped<TChar> : ScopeParseContext<TChar, Untyped<TChar>>
            where TChar : IConvertible, IEquatable<TChar>
        {
            IDictionary<string, object> scope;

            private bool HasValue(string name)
            {
                return scope != null && scope.TryGetValue(name, out _) || parent != null && parent.HasValue(name);
            }

            public void Set(string name, object value)
            {
                if (parent != null && parent.HasValue(name))
                    parent.Set(name, value);
                else
                {
                    if (scope == null)
                        scope = new Dictionary<string, object>();
                    scope[name] = value;
                }
            }

            public T Get<T>(string name)
            {
                if (scope != null && scope.TryGetValue(name, out var result))
                    return (T)result;
                if (parent == null)
                    return default(T);
                return parent.Get<T>(name);
            }

            protected Untyped(Untyped<TChar> context, Scanner<TChar> newScanner = null)
            : base(context, newScanner)
            {
            }

            public Untyped(Scanner<TChar> scanner, bool useNewLines = false)
            : base(scanner, useNewLines)
            {
            }

            public override Untyped<TChar> Scope(BufferSpan<TChar> subBuffer = default)
            {
                if (subBuffer.Equals(null))
                    return new Untyped<TChar>(this);
                return new Untyped<TChar>(this, new Scanner<TChar>(subBuffer));
            }

            public static Untyped<TChar> Scan(Scanner<TChar> scanner, bool useNewLines = false)
            {
                return new Untyped<TChar>(scanner, useNewLines);
            }
        }
    }
}
