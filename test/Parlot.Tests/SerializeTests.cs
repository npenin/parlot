using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using static Parlot.Fluent.Char.Parsers<Parlot.Fluent.Char.ParseContext>;

namespace Parlot.Tests
{
    public class SerializeTests
    {
        [Fact]
        public void ShouldSerializeLiterals()
        {
            var parser = Terms.Text("hello");
            Assert.True(parser.Serializable);
            Assert.True(parser.Serializable);

            var sb = new StringBuilder();
            parser.Serialize(sb, " hello world");
            var result = sb.ToString();

            Assert.Equal(0, result.Length);

            sb = new StringBuilder();
            parser.Serialize(sb, null);
            result = sb.ToString();

            Assert.Equal("hello", result);
        }

        [Fact]
        public void ShouldSerializeStringLiterals()
        {
            var parser = Terms.String();
            Assert.True(parser.Serializable);

            var sb = new StringBuilder();
            parser.Serialize(sb, "hello".ToCharArray());
            var result = sb.ToString();

            Assert.NotNull(result);
            Assert.Equal("'hello'", result);

        }

        [Fact]
        public void ShouldSerializeCharLiterals()
        {
            var parser = Literals.Char('h');
            Assert.True(parser.Serializable);

            var sb = new StringBuilder();
            parser.Serialize(sb, 'w');
            var result = sb.ToString();

            Assert.Equal(0, sb.Length);

            sb = new StringBuilder();
            parser.Serialize(sb, 'h');
            result = sb.ToString();

            Assert.Equal(1, sb.Length);
            Assert.Equal('h', result[0]);

            sb = new StringBuilder();
            parser.Serialize(sb, default);
            result = sb.ToString();

            Assert.Equal(1, sb.Length);
            Assert.Equal('h', result[0]);
        }

        [Fact]
        public void ShouldSerializeRangeLiterals()
        {
            var parser = Terms.Pattern(static c => Character.IsInRange(c, 'a', 'z'));
            Assert.True(parser.Serializable);

            var sb = new StringBuilder();
            parser.Serialize(sb, "helloWorld".ToCharArray());
            var result = sb.ToString();

            Assert.Equal("helloWorld", result.ToString());
        }

        [Fact]
        public void ShouldSerializeDecimalLiterals()
        {
            var parser = Terms.Decimal();
            Assert.True(parser.Serializable);

            var sb = new StringBuilder();
            parser.Serialize(sb, 123);
            var result = sb.ToString();

            Assert.Equal("123", result);

            parser = Literals.Decimal();
            Assert.True(parser.Serializable);

            sb = new StringBuilder();
            parser.Serialize(sb, 123);
            result = sb.ToString();

            Assert.Equal("123", result);
        }

        [Fact]
        public void ShouldSerializeOrs()
        {
            var parser = Terms.Text("hello").Or(Terms.Text("world"));
            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);


            var sb = new StringBuilder();
            parser.Serialize(sb, null);
            var result = sb.ToString();

            Assert.NotNull(result);
            Assert.Equal("hello", result);

            parser.Serialize(sb = new(), "world");
            result = sb.ToString();

            Assert.NotNull(result);
            Assert.Equal("world", result);
        }

        [Fact]
        public void ShouldSerializeAnds()
        {
            var parser = Terms.Text("hello").And(Terms.Text("world"));
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, new(null, null));
            var result = sb.ToString();

            Assert.Equal("hello world", result);
        }

        [Fact]
        public void ShouldSerializeThens()
        {
            var parser = Terms.Text("hello").And(Terms.Text("world")).Then(x => x.Item1.ToUpper(), x =>
            {
                if (x == null)
                    return new(null, null);
                var split = x.ToLower().Split(' ');
                return new(split[0], split[1]);
            });
            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, null);
            var result = sb.ToString();

            Assert.Equal("hello world", result);
        }

        [Fact]
        public void ShouldSerializeDeferreds()
        {
            var deferred = Deferred<string>();

            deferred.Parser = Terms.Text("hello");

            var parser = deferred.And(deferred);

            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, new(null, null));
            var result = sb.ToString();

            Assert.Equal("hello hello", result);
        }

        [Fact]
        public void ShouldSerializeCyclicDeferreds()
        {
            var openParen = Terms.Char('(');
            var closeParen = Terms.Char(')');
            var expression = Deferred<decimal>();

            var groupExpression = Between(openParen, expression, closeParen);
            expression.Parser = Terms.Decimal().Or(groupExpression);

            var parser = ZeroOrMany(expression);

            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, new List<decimal> { 1, 2, 3 });
            var result = sb.ToString();

            Assert.Equal("1 2 3", result);
        }

        [Fact]
        public void ShouldNotSerializeMultipleDeferred()
        {
            var deferred1 = Deferred<decimal>();
            var deferred2 = Deferred<decimal>();

            deferred1.Parser = Terms.Decimal();
            deferred2.Parser = Terms.Decimal();

            var parser = deferred1.And(deferred2).Then(x => x.Item1 + x.Item2);
            Assert.False(parser.Serializable);
        }

        [Fact]
        public void ShouldSerializeRecursive()
        {
            var number = Terms.Decimal();
            var minus = Terms.Char('-');

            var unary = Recursive<decimal>((u) =>
                minus.And(u)
                    .Then<decimal>(static x => 0 - x.Item2, serializer: static (sb, x, parser) =>
                    {
                        if (x < 0)
                            return parser.Serialize(sb, new('-', 0 - x));
                        return false;
                    })
                .Or(number)
                );

            var parser = unary;

            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, 1);
            var result = sb.ToString();

            Assert.Equal("1", result);
        }

        [Fact]
        public void ShouldSerializeZeroOrMany()
        {
            var parser = ZeroOrMany(Terms.Text("hello").Or(Terms.Text("world")));

            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, new() { "hello", "world", "hello" });
            var result = sb.ToString();

            Assert.Equal("hello  world hello", result);
        }

        [Fact]
        public void ShouldSerializeZeroOrOne()
        {
            var parser = ZeroOrOne(Terms.Text("hello"));
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, null);
            var result = sb.ToString();

            Assert.Equal("", result);

            sb = new StringBuilder();
            parser.Serialize(sb, "hello");
            result = sb.ToString();

            Assert.Equal("hello", result);
        }

        [Fact]
        public void ShouldSerializeBetweens()
        {
            var parser = Between(Terms.Text("hello"), Terms.Text("world"), Terms.Text("hello"));
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, null);
            var result = sb.ToString();

            Assert.Equal("hello world hello", result);
        }

        [Fact]
        public void ShouldSerializeSeparatedChar()
        {
            var parser = Separated(Literals.Char(','), Terms.Decimal());
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, null);
            var result = sb.ToString();

            Assert.Equal("", result);

            sb = new StringBuilder();
            parser.Serialize(sb, new() { 1 });
            result = sb.ToString();

            Assert.Equal("1", result);

            sb = new StringBuilder();
            parser.Serialize(sb, new() { 1, 2, 3 });
            result = sb.ToString();

            Assert.Equal("1, 2, 3", result);
        }

        [Fact]
        public void ShouldSerializeExpressionParser()
        {
            var parser = Calc.FluentParser.Expression;

            var parseResult = parser.Parse("(2 + 1) * 3");
            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, parseResult);
            var result = sb.ToString();

            Assert.Equal("( 2 + 1 )  * 3", result);
        }

        [Fact]
        public void ShouldSerializeCapture()
        {
            Parser<char, Fluent.Char.ParseContext, char> Dot = Literals.Char('.');
            Parser<char, Fluent.Char.ParseContext, char> Plus = Literals.Char('+');
            Parser<char, Fluent.Char.ParseContext, char> Minus = Literals.Char('-');
            Parser<char, Fluent.Char.ParseContext, char> At = Literals.Char('@');
            Parser<BufferSpan<char>, Fluent.Char.ParseContext, char> WordChar = Terms.Pattern(char.IsLetterOrDigit);
            Parser<List<char>, Fluent.Char.ParseContext, char> WordDotPlusMinus = OneOrMany(OneOf(WordChar.Then(x => 'w'), Dot, Plus, Minus));
            Parser<List<char>, Fluent.Char.ParseContext, char> WordDotMinus = OneOrMany(OneOf(WordChar.Then(x => 'w'), Dot, Minus));
            Parser<List<char>, Fluent.Char.ParseContext, char> WordMinus = OneOrMany(OneOf(WordChar.Then(x => 'w'), Minus));
            Parser<BufferSpan<char>, Fluent.Char.ParseContext, char> Email = Capture(WordDotPlusMinus.And(At).And(WordMinus).And(Dot).And(WordDotMinus));

            string _email = "sebastien.ros@gmail.com";

            var parser = Email;
            var result = parser.Parse(_email);
            var sb = new StringBuilder();
            parser.Serialize(sb, result);

            Assert.Equal(sb.ToString(), result.ToString());
        }

        private sealed class NonCompilableCharLiteral : Parser<char, Fluent.Char.ParseContext, char>
        {
            public NonCompilableCharLiteral(char c, bool skipWhiteSpace = true)
            {
                Char = c;
                SkipWhiteSpace = skipWhiteSpace;
            }

            public char Char { get; }

            public bool SkipWhiteSpace { get; }

            public override bool Serializable => true;
            public override bool SerializableWithoutValue => true;

            public override bool Parse(Fluent.Char.ParseContext context, ref ParseResult<char> result)
            {
                context.EnterParser(this);

                if (SkipWhiteSpace)
                {
                    context.Scanner.SkipWhiteSpace();
                }

                var start = context.Scanner.Cursor.Offset;

                if (context.Scanner.ReadChar(Char))
                {
                    result.Set(start, context.Scanner.Cursor.Offset, Char);
                    return true;
                }

                return false;
            }

            public override bool Serialize(BufferSpanBuilder<char> sb, char value)
            {
                if (value != default && value != Char)
                    return false;
                sb.Append(Char);
                return true;
            }
        }

        [Fact]
        public void ShouldSerializeNonCompilableCharLiterals()
        {
            var parser = new NonCompilableCharLiteral('h');

            var result = parser.Parse(" hello world");
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            parser.Serialize(sb, result);
            var resultString = sb.ToString();

            Assert.Equal("h", resultString);
        }

        [Fact]
        public void ShouldSerializeOneOfABT()
        {
            var a = Literals.Char('a');
            var b = Literals.Decimal();

            var o2 = a.Or<char, decimal, object, Fluent.Char.ParseContext, char>(b);

            Assert.True(o2.Serializable);
            Assert.False(o2.SerializableWithoutValue);

            var sb = new StringBuilder();
            o2.Serialize(sb, 'a');
            var result = sb.ToString();

            Assert.Equal("a", result);

            sb = new StringBuilder();
            o2.Serialize(sb, (decimal)1);
            result = sb.ToString();

            Assert.Equal("1", result);
        }

        [Fact]
        public void ShouldSerializeAndSkip()
        {
            var code = Terms.Text("hello").AndSkip(Terms.Integer());


            Assert.True(code.Serializable);
            Assert.False(code.SerializableWithoutValue);

            var sb = new StringBuilder();
            Assert.True(code.Serialize(sb, "hello"));
            var result = sb.ToString();
            Assert.Equal("hello 0", result);
        }

        [Fact]
        public void ShouldSerializeSkipAnd()
        {
            var code = Terms.Text("hello").SkipAnd(Terms.Integer());

            Assert.True(code.Serializable);
            Assert.False(code.SerializableWithoutValue);

            var sb = new StringBuilder();
            Assert.True(code.Serialize(sb, 123));
            var result = sb.ToString();
            Assert.Equal("hello 123", result);
        }

        [Fact]
        public void ShouldSerializeEmpty()
        {
            var parser = Empty();

            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            Assert.True(parser.Serialize(sb, 123));
            var result = sb.ToString();
            Assert.Equal(string.Empty, result);

            var parser2 = Empty(1);

            Assert.True(parser2.Serializable);
            Assert.False(parser2.SerializableWithoutValue);

            sb = new StringBuilder();
            Assert.True(parser2.Serialize(sb, 123));
            result = sb.ToString();
            Assert.Equal("123", result);
        }

        [Fact]
        public void ShouldSerializeEof()
        {
            var parser = Empty().Eof();
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            var sb = new StringBuilder();
            Assert.True(parser.Serialize(sb, 123));
            var result = sb.ToString();
            Assert.Equal(string.Empty, result);

            var parser2 = Terms.Decimal().Eof();
            sb = new StringBuilder();
            Assert.True(parser2.Serialize(sb, 123));
            result = sb.ToString();
            Assert.Equal("123", result);
        }

        [Fact]
        public void ShouldSerializeNot()
        {
            Assert.Equal(string.Empty, Not(Terms.Decimal()).Serialize(123));
        }

        [Fact]
        public void ShouldSerializeDiscard()
        {
            Assert.Equal("0", Terms.Decimal().Discard<bool>().Serialize(false));
            Assert.Equal("0", Terms.Decimal().Discard<bool>(true).Serialize(false));
        }

        [Fact]
        public void WhenShouldFailSerializerWhenFalse()
        {
            var parser = Literals.Integer().When(x => x % 2 == 0);
            Assert.False(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            parser = Literals.Integer().When(x => x % 2 == 0, () => -1);
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            Assert.Equal("1234", parser.Serialize(1234));
            Assert.Equal("-1", parser.Serialize(1235));
        }

        [Fact]
        public void ShouldSerializeSwitch()
        {
            var d = Literals.Text("d:");
            var i = Literals.Text("i:");
            var s = Literals.Text("s:");

            var parser = d.Or(i).Or(s).Switch((context, result) =>
            {
                switch (result)
                {
                    case "d:": return Literals.Decimal().Then<object>(x => x);
                    case "i:": return Literals.Integer().Then<object>(x => x);
                    case "s:": return Literals.String().Then<object>(x => x);
                }
                return null;
            });
            Assert.False(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            parser = d.Or(i).Or(s).Switch((context, result) =>
           {
               switch (result)
               {
                   case "d:": return Literals.Decimal().Then<object>(x => x, (sb, x, parser) => x is decimal d ? parser.Serialize(sb, d) : false);
                   case "i:": return Literals.Integer().Then<object>(x => x, (sb, x, parser) => x is long l ? parser.Serialize(sb, l) : false);
                   case "s:": return Literals.String().Then<object>(x => x, (sb, x, parser) => x is char[] s ? parser.Serialize(sb, s) : false);
               }
               return null;
           }, x =>
           {
               if (x is decimal d)
                   return "d:";
               else if (x is long i)
                   return "i:";
               else if (x is char[] s)
                   return "s:";

               throw new NotSupportedException();
           });
            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            Assert.Equal("i:1234", parser.Serialize((long)1234));
            Assert.Equal("d:1234", parser.Serialize((decimal)1234));
            Assert.Equal("s:'toto'", parser.Serialize("toto".ToCharArray()));
        }

        [Fact]
        public void ShouldSerializeTextBefore()
        {
            var parser = AnyCharBefore(Literals.Char('a'));
            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            Assert.Equal("hell", parser.Serialize("hell"));


            parser = AnyCharBefore(Literals.Char('a'), true);
            Assert.True(parser.Serializable);
            Assert.True(parser.SerializableWithoutValue);

            Assert.Equal("hell", parser.Serialize("hell"));

            parser = AnyCharBefore(Literals.Char('a'), false, false, true);
            Assert.True(parser.Serializable);
            Assert.False(parser.SerializableWithoutValue);

            Assert.Equal("hella", parser.Serialize("hell"));
        }

        [Fact]
        public void ShouldSerializeAndSkipWithAnd()
        {
            var parser = Terms.Char('a').And(Terms.Char('b')).AndSkip(Terms.Char('c')).And(Terms.Char('d'));

            Assert.Equal("a b c d", parser.Serialize(new('a', 'b', 'd')));
        }

        [Fact]
        public void ShouldSerializeSkipAndWithAnd()
        {
            var parser = Terms.Char('a').And(Terms.Char('b')).SkipAnd(Terms.Char('c')).And(Terms.Char('d'));

            Assert.Equal("a b c d", parser.Serialize(new('a', 'c', 'd')));
        }
    }
}
