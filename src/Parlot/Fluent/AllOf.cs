using Parlot.Compilation;
using Parlot.Rewriting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Parlot.Fluent
{
    /// <summary>
    /// AllOf the inner choices when all parsers return the same type.
    /// We then return the actual result of each parser.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TParseContext"></typeparam>
    /// <typeparam name="TChar"></typeparam>
    public sealed class AllOf<T, TParseContext, TChar> : Parser<List<T>, TParseContext, TChar>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        private readonly bool _allowRepeat;
        private readonly Parser<T, TParseContext, TChar>[] _parsers;
        internal readonly Dictionary<TChar, List<Parser<T, TParseContext, TChar>>> _lookupTable;
        internal readonly bool _skipWhiteSpace;

        private static bool canUseNewLines = typeof(Char.ParseContext).IsAssignableFrom(typeof(TParseContext));

        public AllOf(bool allowRepeat, Parser<T, TParseContext, TChar>[] parsers)
        {
            _allowRepeat = allowRepeat;
            _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));

            // All parsers are seekable
            if (_parsers.All(x => x is ISeekable<TChar> seekable && seekable.CanSeek))
            {
                _lookupTable = new Dictionary<TChar, List<Parser<T, TParseContext, TChar>>>();

                foreach (var parser in _parsers)
                {
                    var expectedChars = (parser as ISeekable<TChar>).ExpectedChars;

                    foreach (var c in expectedChars)
                    {
                        if (!_lookupTable.TryGetValue(c, out var list))
                        {
                            list = new List<Parser<T, TParseContext, TChar>>();
                            _lookupTable[c] = list;
                        }

                        list.Add(parser);
                    }
                }

                if (_lookupTable.Count <= 1)
                {
                    // If all parsers have the same first char, no need to use a lookup table

                    _lookupTable = null;
                }
                else if (_parsers.All(x => x is ISeekable<TChar> seekable && seekable.SkipWhitespace))
                {
                    // All parsers can start with white spaces
                    _skipWhiteSpace = true;
                }
                else if (_parsers.Any(x => x is ISeekable<TChar> seekable && seekable.SkipWhitespace))
                {
                    // If not all parsers accept a white space, we can't use a lookup table since the order matters

                    _lookupTable = null;
                }
            }
        }

        public Parser<T, TParseContext, TChar>[] Parsers => _parsers;

        public override bool Serializable => _parsers.All(p => p.Serializable);
        public override bool SerializableWithoutValue => false;

        public override bool Parse(TParseContext context, ref ParseResult<List<T>> result)
        {
            context.EnterParser(this);

            var cursor = context.Scanner.Cursor;

            var processedParsers = new HashSet<Parser<T, TParseContext, TChar>>();
            List<T> results = new();
            var start = context.Scanner.Cursor.Position;
            ParseResult<T> inner = new();

            if (_lookupTable != null)
            {

                if (_skipWhiteSpace)
                {
                    if (context.Scanner.Cursor.IsChar)
                    {
                        if (canUseNewLines)
                        {
                            var stringContext = (Char.ParseContext)(object)context;
                            if (stringContext.UseNewLines)
                                stringContext.Scanner.SkipWhiteSpace();
                            else
                                stringContext.Scanner.SkipWhiteSpaceOrNewLine();
                        }
                        else
                            ((Scanner<char>)(object)context.Scanner).SkipWhiteSpace();
                    }
                }

                while (_allowRepeat && !context.Scanner.Cursor.Eof || !_allowRepeat && processedParsers.Count < _parsers.Length)
                {
                    var found = false;
                    if (_lookupTable.TryGetValue(cursor.Current, out var seekableParsers))
                    {
                        var length = seekableParsers.Count;

                        for (var i = 0; i < length; i++)
                        {
                            if ((_allowRepeat || !processedParsers.Contains(seekableParsers[i])) && seekableParsers[i].Parse(context, ref inner))
                            {
                                processedParsers.Add(seekableParsers[i]);
                                results.Add(inner.Value);
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                        break;
                }

                if (processedParsers.Count == _parsers.Length)
                {
                    result.Set(start.Offset, context.Scanner.Cursor.Offset, results);
                    return true;
                }
            }
            else
            {
                var parsers = _parsers;
                var length = parsers.Length;

                while (_allowRepeat && !context.Scanner.Cursor.Eof || !_allowRepeat && processedParsers.Count < length)
                {
                    var found = false;
                    for (var i = 0; i < length; i++)
                    {
                        if ((_allowRepeat || !processedParsers.Contains(parsers[i])) && parsers[i].Parse(context, ref inner))
                        {
                            if (_allowRepeat && !processedParsers.Contains(parsers[i]))
                                processedParsers.Add(parsers[i]);
                            results.Add(inner.Value);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        break;
                }

                if (processedParsers.Count == length)
                {
                    System.Console.WriteLine("end of allof");
                    result.Set(start.Offset, context.Scanner.Cursor.Offset, results);
                    return true;
                }
            }

            context.Scanner.Cursor.ResetPosition(start);
            return false;
        }

        // public CompilationResult Compile(CompilationContext<TParseContext, TChar> context)
        // {
        //     var result = new CompilationResult();

        //     var success = context.DeclareSuccessVariable(result, false);
        //     var value = context.DeclareValueVariable(result, Expression.Default(typeof(T)));

        //     Expression block = Expression.Empty();

        //     if (_lookupTable != null)
        //     {
        //         // Lookup table is converted to a switch expression

        //         // switch (Cursor.Current)
        //         // {
        //         //   case 'a' :
        //         //     parse1 instructions
        //         //     
        //         //     if (parser1.Success)
        //         //     {
        //         //        success = true;
        //         //        value = parse1.Value;
        //         //     }
        //         // 
        //         //     break; // implicit in SwitchCase expression
        //         //
        //         //   case 'b' :
        //         //   ...
        //         // }

        //         var cases = _lookupTable.Select(kvp =>
        //         {
        //             Expression group = Expression.Empty();

        //             // The list is reversed since the parsers are unwrapped
        //             foreach (var parser in kvp.Value.ToArray().Reverse())
        //             {
        //                 var groupResult = parser.Build(context);

        //                 group = Expression.Block(
        //                     groupResult.Variables,
        //                     Expression.Block(groupResult.Body),
        //                     Expression.IfThenElse(
        //                         groupResult.Success,
        //                         Expression.Block(
        //                             Expression.Assign(success, Expression.Constant(true, typeof(bool))),
        //                             context.DiscardResult
        //                             ? Expression.Empty()
        //                             : Expression.Assign(value, groupResult.Value)
        //                             ),
        //                         group
        //                         )
        //                     );
        //             }

        //             return Expression.SwitchCase(
        //                     group,
        //                     Expression.Constant(kvp.Key)
        //                 );
        //         }).ToArray();

        //         SwitchExpression switchExpr =
        //             Expression.Switch(
        //                 context.Current(),
        //                 Expression.Empty(), // no match => success = false
        //                 cases
        //             );

        //         if (_skipWhiteSpace)
        //         {
        //             var start = context.DeclarePositionVariable(result);

        //             block = Expression.Block(
        //                 context.ForceSkipWhiteSpace(),
        //                 switchExpr,
        //                 Expression.IfThen(
        //                     Expression.IsFalse(success),
        //                     context.ResetPosition(start))
        //                 );
        //         }
        //         else
        //         {
        //             block = Expression.Block(
        //                 switchExpr
        //             );
        //         }
        //     }
        //     else
        //     {
        //         // parse1 instructions
        //         // 
        //         // if (parser1.Success)
        //         // {
        //         //    success = true;
        //         //    value = parse1.Value;
        //         // }
        //         // else
        //         // {
        //         //   parse2 instructions
        //         //   
        //         //   if (parser2.Success)
        //         //   {
        //         //      success = true;
        //         //      value = parse2.Value
        //         //   }
        //         //   
        //         //   ...
        //         // }

        //         foreach (var parser in _parsers.Reverse())
        //         {
        //             var parserCompileResult = parser.Build(context);

        //             block = Expression.Block(
        //                 parserCompileResult.Variables,
        //                 Expression.Block(parserCompileResult.Body),
        //                 Expression.IfThenElse(
        //                     parserCompileResult.Success,
        //                     Expression.Block(
        //                         Expression.Assign(success, Expression.Constant(true, typeof(bool))),
        //                         context.DiscardResult
        //                         ? Expression.Empty()
        //                         : Expression.Assign(value, parserCompileResult.Value)
        //                         ),
        //                     block
        //                     )
        //                 );
        //         }
        //     }

        //     result.Body.Add(block);

        //     return result;
        // }

        public override bool Serialize(BufferSpanBuilder<TChar> sb, List<T> values)
        {
            var processedParsers = new HashSet<Parser<T, TParseContext, TChar>>();
            foreach (var value in values)
            {
                foreach (var parser in _parsers)
                {
                    if (!processedParsers.Contains(parser) && parser.Serialize(sb, value))
                        processedParsers.Add(parser);
                }
            }
            return false;
        }
    }
}
