using Parlot.Compilation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    /// <summary>
    /// Ensure the given parser is valid based on a condition, and backtracks if not.
    /// </summary>
    /// <typeparam name="T">The output parser type.</typeparam>
    /// <typeparam name="TParseContext">The parse context type.</typeparam>
    public sealed class Sub<T, TParseContext> : Parser<T, TParseContext, byte>//, ICompilable<TParseContext, byte>
    where TParseContext : ScopeParseContext<byte, TParseContext>
    {
        private readonly Parser<ulong, TParseContext, byte> _lengthParser;
        private readonly Parser<T, TParseContext, byte> _contentParser;

        public override bool Serializable => _lengthParser.Serializable && _contentParser.Serializable;
        public override bool SerializableWithoutValue => _contentParser.SerializableWithoutValue;

        public Sub(Parser<ulong, TParseContext, byte> lengthParser, Parser<T, TParseContext, byte> contentParser)
        {
            _lengthParser = lengthParser ?? throw new ArgumentNullException(nameof(lengthParser));
            _contentParser = contentParser ?? throw new ArgumentNullException(nameof(contentParser));
        }

        public override bool Parse(TParseContext context, ref ParseResult<T> result)
        {
            context.EnterParser(this);

            var start = context.Scanner.Cursor.Position;

            ParseResult<ulong> lengthResult = new();
            if (_lengthParser.Parse(context, ref lengthResult))
            {
                var subBuffer = context.Scanner.Buffer.SubBuffer(context.Scanner.Cursor.Position.Offset, (int)lengthResult.Value);
                if (_contentParser.Parse(context.Scope(subBuffer), ref result))
                {
                    context.Scanner.Cursor.Advance((int)lengthResult.Value);
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(start);
            return false;
        }

        // public CompilationResult Compile(CompilationContext<TParseContext, byte> context)
        // {
        //     var result = new CompilationResult();

        //     var success = context.DeclareSuccessVariable(result, false);
        //     var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

        //     var parserCompileResult = _parser.Build(context, requireResult: true);

        //     // success = false;
        //     // value = default;
        //     // start = context.Scanner.Cursor.Position;
        //     // parser instructions
        //     // 
        //     // if (parser.Success && _action(value))
        //     // {
        //     //   success = true;
        //     //   value = parser.Value;
        //     // }
        //     // else
        //     // {
        //     //    context.Scanner.Cursor.ResetPosition(start);
        //     // }
        //     //

        //     var start = context.DeclarePositionVariable(result);

        //     var block = Expression.Block(
        //             parserCompileResult.Variables,
        //             parserCompileResult.Body
        //             .Append(
        //                 Expression.IfThenElse(
        //                     Expression.AndAlso(
        //                         parserCompileResult.Success,
        //                         Expression.Invoke(Expression.Constant(_action), new[] { parserCompileResult.Value })
        //                         ),
        //                     Expression.Block(
        //                         Expression.Assign(success, Expression.Constant(true, typeof(bool))),
        //                         context.DiscardResult
        //                             ? Expression.Empty()
        //                             : Expression.Assign(value, parserCompileResult.Value)
        //                         ),
        //                     context.ResetPosition(start)
        //                     )
        //                 )
        //             );


        //     result.Body.Add(block);

        //     return result;
        // }

        public override bool Serialize(BufferSpanBuilder<byte> sb, T value)
        {
            var subBuilder = new BytesBuilder(sb.Culture);
            if (_contentParser.Serialize(subBuilder, value))
            {
                if (_lengthParser.Serialize(sb, (ulong)subBuilder.Length))
                {
                    sb.Append(subBuilder);
                    return true;
                }
            }
            return false;
        }
    }
}
