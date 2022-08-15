using Parlot.Compilation;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    public sealed class CharLiteral<TChar, TParseContext> : Parser<TChar, TParseContext, TChar>, ICompilable<TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        public CharLiteral(TChar c)
        {
            Char = c;
        }

        public TChar Char { get; }

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => true;

        public override bool Parse(TParseContext context, ref ParseResult<TChar> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Offset;

            if (context.Scanner.ReadChar(Char))
            {
                result.Set(start, context.Scanner.Cursor.Offset, Char);
                return true;
            }

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(char)));

            // if (context.Scanner.ReadChar(Char))
            // {
            //     success = true;
            //     value = Char;
            // }

            result.Body.Add(
                Expression.IfThen(
                    context.ReadChar(Char),
                    Expression.Block(
                        Expression.Assign(success, Expression.Constant(true, typeof(bool))),
                        context.DiscardResult
                        ? Expression.Empty()
                        : Expression.Assign(value, Expression.Constant(Char, typeof(char)))
                        )
                    )
            );

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, TChar value)
        {
            if (Char.Equals(value) || default(TChar).Equals(value))
            {
                sb.Append(Char);
                return true;
            }
            return false;
        }
    }
}
