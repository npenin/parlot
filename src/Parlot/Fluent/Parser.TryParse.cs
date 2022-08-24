namespace Parlot.Fluent
{
    using System.Threading;

    public abstract partial class Parser<T, TParseContext, TChar>
    {
        private int _invocations = 0;
        private volatile Parser<T, TParseContext, TChar> _compiledParser;

        public T Parse(TParseContext context)
        {
            var localResult = new ParseResult<T>();

            var success = CheckCompiled(context).Parse(context, ref localResult);

            if (success)
            {
                return localResult.Value;
            }

            return default;
        }

        private Parser<T, TParseContext, TChar> CheckCompiled(TParseContext context)
        {
            if (_compiledParser != null || context.CompilationThreshold == 0)
            {
                return _compiledParser ?? this;
            }

            // Only the thread that reaches CompilationThreshold compiles the parser.
            // Any other concurrent call here will return 'this'. This prevents multiple compilations of 
            // the same parser, and a lock.

            if (Interlocked.Increment(ref _invocations) == context.CompilationThreshold)
            {
                return _compiledParser = this.Compile();
            }

            return this;
        }


        public bool TryParse(TParseContext context, out T value, out ParseError error)
        {
            error = null;

            try
            {
                var localResult = new ParseResult<T>();

                var success = CheckCompiled(context).Parse(context, ref localResult);

                if (success)
                {
                    value = localResult.Value;
                    return true;
                }
            }
            catch (ParseException e)
            {
                error = new ParseError
                {
                    Message = e.Message,
                    Position = e.Position
                };
            }

            value = default;
            return false;
        }
    }
}


namespace Parlot.Fluent
{
    using System;

    public static class ParserExtensions
    {
        public static T Parse<T>(this Parser<T, Char.ParseContext, char> parser, string text)
        {
            return parser.Parse(new Char.ParseContext(new Scanner<char>(text.ToCharArray())));
        }

        public static bool TryParse<TResult>(this Parser<TResult, Char.ParseContext, char> parser, string text, out TResult value)
        {
            return parser.TryParse(text, out value, out _);
        }

        public static bool TryParse<TResult>(this Parser<TResult, Char.ParseContext, char> parser, string text, out TResult value, out ParseError error)
        {
            return parser.TryParse(new Char.ParseContext(new Scanner<char>(text.ToCharArray())), out value, out error);
        }

        public static bool TryParse<TResult, TParseContext, TChar>(this Parser<TResult, TParseContext, TChar> parser, TParseContext context, out TResult value)
        where TParseContext : ParseContextWithScanner<TChar>
        where TChar : IEquatable<TChar>, IConvertible
        {
            return parser.TryParse(context, out value, out _);
        }
    }
}