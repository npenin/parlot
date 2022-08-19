﻿using Parlot.Compilation;
using Parlot.Rewriting;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Parlot.Fluent.Char
{
    public sealed class TextLiteral<TParseContext> : Parser<string, TParseContext, char>, ICompilable<TParseContext, char>, ISeekable<char>
    where TParseContext : ParseContextWithScanner<char>
    {
        private readonly StringComparison _comparisonType;
        private readonly bool _hasNewLines;

        public TextLiteral(string text, StringComparison comparisonType)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            _comparisonType = comparisonType;
            _hasNewLines = text.Any(x => Character.IsNewLine(x));
        }

        public string Text { get; }

        public override bool Serializable => true;
        public override bool SerializableWithoutValue => true;

        public bool CanSeek => Text.Length > 0;

        public char[] ExpectedChars => new[] { Text[0] };

        public bool SkipWhitespace => false;

        public override bool Parse(TParseContext context, ref ParseResult<string> result)
        {
            context.EnterParser(this);

            var cursor = context.Scanner.Cursor;

            if (cursor.Match(Text, _comparisonType))
            {
                var start = cursor.Offset;

                if (_hasNewLines)
                {
                    cursor.Advance(Text.Length);
                }
                else
                {
                    cursor.AdvanceNoNewLines(Text.Length);
                }

                result.Set(start, cursor.Offset, Text);
                return true;
            }

            return false;
        }

        public CompilationResult Compile(CompilationContext<TParseContext, char> context)
        {
            var result = new CompilationResult();

            var success = context.DeclareSuccessVariable(result, false);
            var value = context.DeclareValueVariable(result, Expression.Default(typeof(string)));

            // if (context.Scanner.ReadText(Text, _comparer, null))
            // {
            //      success = true;
            //      value = Text;
            // }
            //
            // [if skipWhiteSpace]
            // if (!success)
            // {
            //      resetPosition(beginning);
            // }

            var ifReadText = Expression.IfThen(
                Expression.Call(
null,
                    ExpressionHelper.Scanner_ReadText_NoResult,
                    context.Scanner(),
                    Expression.Constant(Text, typeof(string)),
                    Expression.Constant(_comparisonType, typeof(StringComparison))
                    ),
                Expression.Block(
                    Expression.Assign(success, Expression.Constant(true, typeof(bool))),
                    context.DiscardResult
                    ? Expression.Empty()
                    : Expression.Assign(value, Expression.Constant(Text, typeof(string)))
                    )
                );

            result.Body.Add(ifReadText);

            return result;
        }

        public override bool Serialize(BufferSpanBuilder<char> sb, string value)
        {
            if (value == null || value.Equals(Text, _comparisonType))
            {
#if SUPPORTS_READONLYSPAN
                sb.Append(Text.AsSpan());
#else
                sb.Append(Text.ToCharArray());
#endif
                return true;
            }
            return false;
        }
    }
}
