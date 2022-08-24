using Parlot.Fluent;
using Parlot.Rewriting;

namespace Parlot.Tests.Models
{
    public partial class RewriteTests
    {
        public sealed class FakeSeekable : Parser<string, Fluent.Char.ParseContext, char>, ISeekable<char>
        {
            public FakeSeekable()
            {
            }

            public string Text { get; set; }
            public bool CanSeek { get; set; }
            public char[] ExpectedChars { get; set; }
            public bool SkipWhiteSpace { get; }
            public bool SkipWhitespace { get; set; }
            public bool Success { get; set; }

            public override bool Serializable => false;

            public override bool SerializableWithoutValue => false;

            public override bool Parse(Fluent.Char.ParseContext context, ref ParseResult<string> result)
            {
                context.EnterParser(this);

                if (Success)
                {
                    result.Set(0, 0, Text);
                    return true;
                }

                return false;
            }

            public override bool Serialize(BufferSpanBuilder<char> sb, string value)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
