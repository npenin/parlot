using Parlot.Compilation;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace Parlot.Fluent.Byte
{
    public sealed class Numeric<T, TParseContext> : Parser<T, TParseContext, byte>, ICompilable<TParseContext, byte>
    where TParseContext : ParseContextWithScanner<byte>
    where T : IConvertible, IEquatable<T>
    {
        public override bool Serializable => convertBack != null;
        public override bool SerializableWithoutValue => false;

        private ushort size;
        private Func<BufferSpan<byte>, T> convert;
        private Func<T, BufferSpan<byte>> convertBack;

        public Numeric(Func<BufferSpan<byte>, T> convert, Func<T, BufferSpan<byte>> convertBack)
        : this(GetSize(typeof(T)), convert, convertBack)
        {
        }

        private static ushort GetSize(Type type)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return 1;
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return 8;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return 16;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return 32;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return 64;
                case TypeCode.Single:
                    return 32;
                case TypeCode.Double:
                    return 64;
                case TypeCode.Decimal:
                    return 128;
                default:
                    throw new NotSupportedException($"{type.FullName} is not a valid numeric value");
            }
        }

        public Numeric(ushort size, Func<BufferSpan<byte>, T> convert, Func<T, BufferSpan<byte>> convertBack)
        {
            this.size = size;
            this.convert = convert;
            this.convertBack = convertBack;
        }

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var reset = context.Scanner.Cursor.Position;
            var start = reset.Offset;

            if (context.Scanner.ReadN(size, out var buffer))
            {
                result.Set(start, context.Scanner.Cursor.Position.Offset, convert(buffer));
                return true;
            }

            context.Scanner.Cursor.ResetPosition(reset);

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, byte> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable<T, TParseContext>(result);

            var reset = context.DeclarePositionVariable(result);
            var start = context.DeclareOffsetVariable(result);

            // if (success=context.Scanner.ReadN(size, out var buffer))
            // {
            //     value=convert(buffer);
            //     return true;
            // }

            // if (!success)
            // {
            //    context.Scanner.Cursor.ResetPosition(begin);
            // }
            //
            var sourceToParse = Expression.Variable(typeof(BufferSpan<byte>), $"sourceToParse{context.NextNumber}");

            var block =
                Expression.IfThen(
                    Expression.Assign(success, context.ReadN(size, sourceToParse)),
                    Expression.Block(
                        new[] { sourceToParse },
                        Expression.Assign(value,
                            Expression.Call(
                                convert.Method,
                                sourceToParse
                            )
                        )
                    )
                );

            result.Body.Add(block);

            result.Body.Add(
                Expression.IfThen(
                    Expression.Not(success),
                    context.ResetPosition(reset)
                    )
                );

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<byte> sb, T value)
        {
            sb.Append(value);
            return true;
        }
    }
}
