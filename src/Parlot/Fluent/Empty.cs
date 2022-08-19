using Parlot.Compilation;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    /// <summary>
    /// Doesn't parse anything and return the default value.
    /// </summary>
    public sealed class Empty<T, TParseContext, TChar> : Parser<T, TParseContext, TChar>, ICompilable<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly T _value;

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => _value == null && typeof(T) == typeof(object);

        public Empty()
        {
            _value = default;
        }

        public Empty(T value)
        {
            _value = value;
        }

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            result.Set(context.Scanner.Cursor.Offset, context.Scanner.Cursor.Offset, _value);

            return true;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            _ = context.DeclareSuccessVariable(result, true);
            _ = context.DeclareValueVariable(result, Expression.Constant(_value));

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, T value)
        {
            if (_value is IConvertible)
                sb.Append(value);
            return true;
        }
    }
}
