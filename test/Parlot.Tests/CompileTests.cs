using System.Linq.Expressions;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using Xunit;
using static Parlot.Fluent.Parsers;

namespace Parlot.Tests
{
    public class CompileTests
    {
        public static Func<ParseContext, T> Compile<T>(Parser<T> parser)
        {
            var parseContext = Expression.Parameter(typeof(ParseContext));
            var compilationContext = new CompilationContext(parseContext)
            {
                //OnParse = (context, parser) => { Console.WriteLine($"'{context.Scanner.Cursor.Current}' {context.Scanner.Cursor.Position}"); }
            };

            var compileResult = parser.Compile(compilationContext);

            var returnLabelTarget = Expression.Label(typeof(T));
            var returnLabelExpression = Expression.Label(returnLabelTarget, compileResult.Value);

            compileResult.Body.Add(returnLabelExpression);

            var allVariables = new List<ParameterExpression>();
            allVariables.AddRange(compilationContext.GlobalVariables);
            allVariables.AddRange(compileResult.Variables);

            var allExpressions = new List<Expression>();
            allExpressions.AddRange(compilationContext.GlobalExpressions);
            allExpressions.AddRange(compileResult.Body);

            var body = Expression.Block(
                typeof(T),
                allVariables,
                allExpressions
                );

            var result = Expression.Lambda<Func<ParseContext, T>>(body, parseContext);

            return result.Compile();
        }

        [Fact]
        public void ShouldCompileTextLiterals()
        {
            var parse = Compile(Terms.Text("hello"));

            var scanner = new Scanner(" hello world");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.NotNull(result);
            Assert.Equal("hello", result);
        }

        [Fact]
        public void ShouldCompileStringLiterals()
        {
            var parse = Compile(Terms.String());

            var scanner = new Scanner("'hello'");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal("hello", result);
        }

        [Fact]
        public void ShouldCompileCharLiterals()
        {
            var parse = Compile(Terms.Char('h'));

            var scanner = new Scanner(" hello world");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal('h', result);
        }

        [Fact]
        public void ShouldCompileRangeLiterals()
        {
            var parse = Compile(Terms.Pattern(static c => Character.IsInRange(c, 'a', 'z')));

            var scanner = new Scanner("helloWorld");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal("hello", result);
        }

        [Fact]
        public void ShouldCompileDecimalLiterals()
        {
            var parse = Compile(Terms.Decimal());

            var scanner = new Scanner(" 123");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(123, result);
        }

        [Fact]
        public void ShouldCompileLiteralsWithoutSkipWhiteSpace()
        {
            var parse = Compile(Literals.Text("hello"));

            var scanner = new Scanner(" hello world");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Null(result);
        }

        [Fact]
        public void ShouldCompileOrs()
        {
            var parse = Compile(Terms.Text("hello").Or(Terms.Text("world")));

            var scanner = new Scanner(" hello world");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.NotNull(result);
            Assert.Equal("hello", result);

            result = parse(context);

            Assert.NotNull(result);
            Assert.Equal("world", result);
        }

        [Fact]
        public void ShouldCompileAnds()
        {
            var parse = Compile(Terms.Text("hello").And(Terms.Text("world")));

            var scanner = new Scanner(" hello world");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(("hello", "world"), result);
        }

        [Fact]
        public void ShouldCompileThens()
        {
            var parse = Compile(Terms.Text("hello").And(Terms.Text("world")).Then(x => x.Item1.ToUpper()));

            var scanner = new Scanner(" hello world");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void ShouldCompileDeferreds()
        {
            var deferred = Deferred<string>();

            deferred.Parser = Terms.Text("hello");

            var parse = Compile(deferred.And(deferred));

            var scanner = new Scanner(" hello hello hello");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(("hello", "hello"), result);
        }

        [Fact]
        public void ShouldCompileCyclicDeferreds()
        {
            var openParen = Terms.Char('(');
            var closeParen = Terms.Char(')');
            var expression = Deferred<decimal>();

            var groupExpression = Between(openParen, expression, closeParen);
            expression.Parser = Terms.Decimal().Or(groupExpression);

            var parse = Compile(ZeroOrMany(expression));

            var scanner = new Scanner("1 (2) 3");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(new decimal[] {1, 2, 3 }, result);
        }

        [Fact]
        public void ShouldCompileMultipleDeferred()
        {
            var deferred1 = Deferred<decimal>();
            var deferred2 = Deferred<decimal>();

            deferred1.Parser = Terms.Decimal();
            deferred2.Parser = Terms.Decimal();

            var parser = deferred1.And(deferred2).Then(x => x.Item1 + x.Item2);

            var parse = Compile(parser);

            var scanner = new Scanner("1 2");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(3, result);
        }

        [Fact]
        public void ShouldCompileRecursive()
        {
            var number = Terms.Decimal();
            var minus = Terms.Char('-');

            var unary = Recursive<decimal>((u) =>
                minus.And(u)
                    .Then(static x => 0 - x.Item2)
                .Or(number)
                );

            var parse = Compile(unary);

            var scanner = new Scanner("--1");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldCompileZeroOrManys()
        {
            var parse = Compile(ZeroOrMany(Terms.Text("hello").Or(Terms.Text("world"))));

            var scanner = new Scanner(" hello world hello");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(new[] { "hello", "world", "hello" }, result);
        }


        [Fact]
        public void ShouldCompileBetweens()
        {
            var parse = Compile(Between(Terms.Text("hello"), Terms.Text("world"), Terms.Text("hello")));

            var scanner = new Scanner(" hello world hello");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal("world", result);
        }

        [Fact]
        public void ShouldCompileSeparated()
        {
            var parse = Compile(Separated(Terms.Char(','), Terms.Decimal()));

            var scanner = new Scanner("1, 2,3");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ShouldCompileExpressionParser()
        {
            var parse = Compile(Calc.FluentParser.Expression);

            var scanner = new Scanner("(2 + 1) * 3");
            var context = new ParseContext(scanner);
            var result = parse(context);

            Assert.Equal(9, result.Evaluate());
        }

        [Fact]
        public void ShouldCompileCapture()
        {
            Parser<char> Dot = Literals.Char('.');
            Parser<char> Plus = Literals.Char('+');
            Parser<char> Minus = Literals.Char('-');
            Parser<char> At = Literals.Char('@');
            Parser<TextSpan> WordChar = Literals.Pattern(char.IsLetterOrDigit);
            Parser<List<char>> WordDotPlusMinus = OneOrMany(OneOf(WordChar.Then(x => 'w'), Dot, Plus, Minus));
            Parser<List<char>> WordDotMinus = OneOrMany(OneOf(WordChar.Then(x => 'w'), Dot, Minus));
            Parser<List<char>> WordMinus = OneOrMany(OneOf(WordChar.Then(x => 'w'), Minus));
            Parser<TextSpan> Email = Capture(WordDotPlusMinus.And(At).And(WordMinus).And(Dot).And(WordDotMinus));

            string _email = "sebastien.ros@gmail.com";

            var scanner = new Scanner(_email);
            var context = new ParseContext(scanner);
            var parse = Compile(Email);
            var result = parse(context);

            Assert.Equal(_email, result.ToString());
        }
    }
}
