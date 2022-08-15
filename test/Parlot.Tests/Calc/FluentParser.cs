using Parlot.Fluent;
using static Parlot.Fluent.StringParsers<Parlot.Fluent.StringParseContext>;

namespace Parlot.Tests.Calc
{
    public class FluentParser
    {
        public static readonly Parser<Expression, StringParseContext, char> Expression;

        static FluentParser()
        {
            /*
             * Grammar:
             * expression     => factor ( ( "-" | "+" ) factor )* ;
             * factor         => unary ( ( "/" | "*" ) unary )* ;
             * unary          => ( "-" ) unary
             *                 | primary ;
             * primary        => NUMBER
             *                  | "(" expression ")" ;
            */

            // The Deferred helper creates a parser that can be referenced by others before it is defined
            var expression = Deferred<Expression>();

            var number = Terms.Decimal()
                .Then<Expression>(static d => new Number(d), (sb, e, parser) => e is Number n ? parser.Serialize(sb, n.Value) : false)
                ;

            var divided = Terms.Char('/');
            var times = Terms.Char('*');
            var minus = Terms.Char('-');
            var plus = Terms.Char('+');
            var openParen = Terms.Char('(');
            var closeParen = Terms.Char(')');

            // "(" expression ")"
            var groupExpression = Between(openParen, expression, closeParen);

            // primary => NUMBER | "(" expression ")";
            var primary = number.Or(groupExpression);

            // The Recursive helper allows to create parsers that depend on themselves.
            // ( "-" ) unary | primary;
            var unary = Recursive<Expression>((u) =>
                minus.And(u)
                    .Then<Expression>(static x => new NegateExpression(x.Item2), static (sb, x, parser) => x is NegateExpression n ? parser.Serialize(sb, new('-', n.Inner)) : false)
                    .Or(primary));

            // factor => unary ( ( "/" | "*" ) unary )* ;
            var factor = unary.And(ZeroOrMany(divided.Or(times).And(unary)))
                .Then(static x =>
                {
                    // unary
                    var result = x.Item1;

                    // (("/" | "*") unary ) *
                    foreach (var op in x.Item2)
                    {
                        result = op.Item1 switch
                        {
                            '/' => new Division(result, op.Item2),
                            '*' => new Multiplication(result, op.Item2),
                            _ => null
                        };
                    }

                    return result;
                }, static (sb, op, parser) =>
                {
                    var revertBack = new System.Collections.Generic.List<(char, Expression)>();
                    var exp = op;
                    while (exp is Division || exp is Multiplication)
                    {
                        if (exp is Division a)
                        {
                            revertBack.Add(new('/', a.Right));
                            exp = a.Left;
                        }
                        if (exp is Multiplication s)
                        {
                            revertBack.Add(new('*', s.Right));
                            exp = s.Left;
                        }
                    }
                    return parser.Serialize(sb, new(exp, revertBack));
                });

            // expression => factor ( ( "-" | "+" ) factor )* ;
            expression.Parser = factor.And(ZeroOrMany(plus.Or(minus).And(factor)))
                .Then(static x =>
                {
                    // factor
                    var result = x.Item1;

                    // (("-" | "+") factor ) *
                    foreach (var op in x.Item2)
                    {
                        result = op.Item1 switch
                        {
                            '+' => new Addition(result, op.Item2),
                            '-' => new Subtraction(result, op.Item2),
                            _ => null
                        };
                    }

                    return result;
                }, static (sb, op, parser) =>
                {
                    var revertBack = new System.Collections.Generic.List<(char, Expression)>();
                    var exp = op;
                    while (exp is Addition || exp is Subtraction)
                    {
                        if (exp is Addition a)
                        {
                            revertBack.Add(new('+', a.Right));
                            exp = a.Left;
                        }
                        if (exp is Subtraction s)
                        {
                            revertBack.Add(new('-', s.Right));
                            exp = s.Left;
                        }
                    }
                    return parser.Serialize(sb, new(exp, revertBack));
                });

            Expression = expression;
        }
    }
}
