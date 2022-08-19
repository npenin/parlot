using System;
using System.Text;

namespace Parlot.Fluent
{
    public abstract partial class Parser<T, TParseContext>
    where TParseContext : ParseContext
    {
        public abstract bool Serializable { get; }
        public abstract bool SerializableWithoutValue { get; }

        public abstract bool Parse(TParseContext context, ref ParseResult<T> result);

        // /// <summary>
        // /// Builds a parser that converts the previous result when it succeeds.
        // /// </summary>
        // public Parser<U, TParseContext> Then<U>(Func<T, U> conversion, Func<U, T> conversionBack = null) => new Then<T, U, TParseContext>(this, conversion, conversionBack);

        // /// <summary>
        // /// Builds a parser that converts the previous result when it succeeds.
        // /// </summary>
        // public Parser<U, TParseContext> Then<U>(Func<T, U> conversion, Func<StringBuilder, U, Parser<T, TParseContext>, bool> serializer) => new Then<T, U, TParseContext>(this, conversion, serializer);

        // /// <summary>
        // /// Builds a parser that converts the previous result, and can alter the current <see cref="ParseContext"/>.
        // /// </summary>
        // public Parser<U, TParseContext> Then<U>(Func<TParseContext, T, U> conversion, Func<U, T> conversionBack = null) => new Then<T, U, TParseContext>(this, conversion, conversionBack);

        // /// <summary>
        // /// Builds a parser that converts the previous result, and can alter the current <see cref="ParseContext"/>.
        // /// </summary>
        // public Parser<U, TParseContext> Then<U>(Func<TParseContext, T, U> conversion, Func<StringBuilder, U, Parser<T, TParseContext>, bool> serializer) => new Then<T, U, TParseContext>(this, conversion, serializer);

        // /// <summary>
        // /// Builds a parser that converts the previous result when it succeeds.
        // /// </summary>
        // public Parser<T, TParseContext,TChar> Then(Action<T> action) => new Then<T, TParseContext,TChar>(this, action);

        // /// <summary>
        // /// Builds a parser that converts the previous result, and can alter the current <see cref="ParseContext"/>.
        // /// </summary>
        // public Parser<T, TParseContext,TChar> Then(Action<TParseContext, T> action) => new Then<T, TParseContext>(this, action);

        // /// <summary>
        // /// Builds a parser what returns another one based on the previous result.
        // /// </summary>
        // public Parser<U, TParseContext> Switch<U>(Func<TParseContext, T, Parser<U, TParseContext>> action, Func<U, T> reverseAction = null) => new Switch<T, U, TParseContext>(this, action, reverseAction);

        // /// <summary>
        // /// Builds a parser that discards the previous result and replaces it by the specified type or value.
        // /// </summary>
        // public Parser<U, TParseContext> Discard<U>() => new Discard<T, U, TParseContext>(this);

        // /// <summary>
        // /// Builds a parser that discards the previous result and replaces it by the specified type or value.
        // /// </summary>
        // public Parser<U, TParseContext> Discard<U>(U value) => new Discard<T, U, TParseContext>(this, value);
    }


    public abstract partial class Parser<T, TParseContext, TChar> : Parser<T, TParseContext>
    where TParseContext : ParseContextWithScanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        public abstract bool Serialize(BufferSpanBuilder<TChar> sb, T value);

        /// <summary>
        /// Builds a parser that converts the previous result when it succeeds.
        /// </summary>
        public Parser<U, TParseContext, TChar> Then<U>(Func<T, U> conversion, Func<U, T> conversionBack = null) => new Then<T, U, TParseContext, TChar>(this, conversion, conversionBack);

        /// <summary>
        /// Builds a parser that converts the previous result when it succeeds.
        /// </summary>
        public Parser<U, TParseContext, TChar> Then<U>(Func<T, U> conversion, Func<BufferSpanBuilder<TChar>, U, Parser<T, TParseContext, TChar>, bool> serializer) => new Then<T, U, TParseContext, TChar>(this, conversion, serializer);

        /// <summary>
        /// Builds a parser that converts the previous result, and can alter the current <see cref="ParseContext"/>.
        /// </summary>
        public Parser<U, TParseContext, TChar> Then<U>(Func<TParseContext, T, U> conversion, Func<BufferSpanBuilder<TChar>, U, Parser<T, TParseContext, TChar>, bool> serializer = null) => new Then<T, U, TParseContext, TChar>(this, conversion, serializer);

        /// <summary>
        /// Builds a parser that converts the previous result when it succeeds.
        /// </summary>
        public Parser<T, TParseContext, TChar> Then(Action<T> action) => new Then<T, TParseContext, TChar>(this, action);

        /// <summary>
        /// Builds a parser that converts the previous result, and can alter the current <see cref="ParseContext"/>.
        /// </summary>
        public Parser<T, TParseContext, TChar> Then(Action<TParseContext, T> action) => new Then<T, TParseContext, TChar>(this, action);


        /// <summary>
        /// Builds a parser that emits an error when the previous parser failed.
        /// </summary>
        public Parser<T, TParseContext, TChar> ElseError(string message) => new ElseError<T, TParseContext, TChar>(this, message);

        /// <summary>
        /// Builds a parser that emits an error.
        /// </summary>
        public Parser<T, TParseContext, TChar> Error(string message) => new Error<T, TParseContext, TChar>(this, message);

        /// <summary>
        /// Builds a parser that emits an error.
        /// </summary>
        public Parser<U, TParseContext, TChar> Error<U>(string message) => new Error<T, U, TParseContext, TChar>(this, message);


        /// <summary>
        /// Builds a parser that ensure the cursor is tat the end of the input.
        /// </summary>
        public Parser<T, TParseContext, TChar> Eof() => new Eof<T, TParseContext, TChar>(this);

        /// <summary>
        /// Builds a parser that verifies the previous parser result matches a predicate.
        /// </summary>
        public Parser<T, TParseContext, TChar> When(Func<T, bool> predicate, Func<T> defaultValue = null) => new When<T, TParseContext, TChar>(this, predicate, defaultValue);

        /// <summary>
        /// Builds a parser that returns another one based on the previous result.
        /// </summary>
        public Parser<U, TParseContext, TChar> Switch<U>(Func<TParseContext, T, Parser<U, TParseContext, TChar>> action, Func<U, T> reverseAction = null) => new Switch<T, U, TParseContext, TChar>(this, action, reverseAction);

        /// <summary>
        /// Builds a parser that discards the previous result and replaces it by the specified type or value.
        /// </summary>
        public Parser<U, TParseContext, TChar> Discard<U>() => new Discard<T, U, TParseContext, TChar>(this);

        /// <summary>
        /// Builds a parser that discards the previous result and replaces it by the specified type or value.
        /// </summary>
        public Parser<U, TParseContext, TChar> Discard<U>(U value) => new Discard<T, U, TParseContext, TChar>(this, value);
    }

}
