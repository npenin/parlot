using System;

namespace Parlot
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This class is used to return tokens extracted from the input buffer.
    /// <typeparamref name="TChar"/>
    /// </summary>
    public class Scanner<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        public readonly BufferSpan<TChar> Buffer;
        public readonly Cursor<TChar> Cursor;

        /// <summary>
        /// Scans some text.
        /// </summary>
        /// <param name="buffer">The string containing the text to scan.</param>
        public Scanner(BufferSpan<TChar> buffer)
        {
            Buffer = buffer.Buffer is null ? throw new ArgumentNullException(nameof(buffer)) : buffer;
            Cursor = new Cursor<TChar>(Buffer, TextPosition.Start);
        }

        /// <summary>
        /// Scans some text.
        /// </summary>
        /// <param name="buffer">The string containing the text to scan.</param>
        public Scanner(ReadOnlySpan<TChar> buffer)
        : this(new BufferSpan<TChar>(buffer.ToArray()))
        {
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadFirstThenOthers(Func<TChar, bool> first, Func<TChar, bool> other)
            => ReadFirstThenOthers(first, other, out _);

        public bool ReadFirstThenOthers(Func<TChar, bool> first, Func<TChar, bool> other, out TokenResult<TChar> result)
        {
            if (!first(Cursor.Current))
            {
                result = TokenResult.Fail<TChar>();
                return false;
            }

            var start = Cursor.Offset;

            // At this point we have an identifier, read while it's an identifier part.

            Cursor.Advance();

            ReadWhile(other, out _);

            result = TokenResult.Succeed(Buffer, start, Cursor.Offset);

            return true;
        }
        public bool ReadN(int length, out TokenResult<TChar> result)
        {
            if (Cursor.Eof)
            {
                result = TokenResult.Fail<TChar>();
                return false;
            }

            var start = Cursor.Offset;

            if (!Cursor.AdvanceNoNewLines(length))
            {
                result = TokenResult.Fail<TChar>();
                return false;
            }

            result = TokenResult.Succeed(Buffer, start, Cursor.Offset);
            return true;
        }


        /// <summary>
        /// Reads a token while the specific predicate is valid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadWhile(Func<TChar, bool> predicate) => ReadWhile(predicate, out _);

        /// <summary>
        /// Reads a token while the specific predicate is valid.
        /// </summary>
        public bool ReadWhile(Func<TChar, bool> predicate, out TokenResult<TChar> result)
        {
            if (Cursor.Eof || !predicate(Cursor.Current))
            {
                result = TokenResult.Fail<TChar>();
                return false;
            }

            var start = Cursor.Offset;

            Cursor.Advance();

            while (!Cursor.Eof && predicate(Cursor.Current))
            {
                Cursor.Advance();
            }

            result = TokenResult.Succeed(Buffer, start, Cursor.Offset);

            return true;
        }

        /// <summary>
        /// Reads 1 <typeparamref name="TChar"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSingle(out TChar item)
        {
            if (Cursor.Eof)
            {
                item = default;
                return false;
            }
            var current = Cursor.Current;
            Cursor.Advance();
            item = current;
            return true;
        }


        /// <summary>
        /// Reads the specified text.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadChar(TChar c)
        {
            if (!Cursor.Match(c))
            {
                return false;
            }

            Cursor.Advance();
            return true;
        }

        /// <summary>
        /// Reads the specified text.
        /// </summary>
        public bool ReadChar(TChar c, out TokenResult<TChar> result)
        {
            if (!Cursor.Match(c))
            {
                result = TokenResult.Fail<TChar>();
                return false;
            }

            var start = Cursor.Offset;
            Cursor.Advance();

            result = TokenResult.Succeed(Buffer, start, Cursor.Offset);
            return true;
        }
        /// <summary>
        /// Reads a sequence token enclosed in arbritrary start and end characters.
        /// </summary>
        /// <remarks>
        /// This method doesn't escape the string, but only validates its content is syntactically correct.
        /// The resulting Span contains the original quotes.
        /// </remarks>
        public bool ReadNonEscapableSequence(TChar startSequenceChar, TChar endSequenceChar, out TokenResult<TChar> result)
        {
            var startChar = Cursor.Current;

            if (!startChar.Equals(startSequenceChar))
            {
                result = TokenResult.Fail<TChar>();
                return false;
            }

            // Fast path if there aren't any escape char until next quote
            var startOffset = Cursor.Offset + 1;
            var lastQuote = startOffset;

            int nextQuote;
            do
            {
                nextQuote = Cursor.Buffer.IndexOf(endSequenceChar, lastQuote + 1);

                if (nextQuote == -1)
                {
                    if (startOffset == lastQuote)
                    {
                        // There is no end sequence character, not a valid escapable sequence
                        result = TokenResult.Fail<TChar>();
                        return false;
                    }
                    nextQuote = lastQuote - 1;
                    break;
                }

                lastQuote += nextQuote + 2;
            }
            while (Cursor.Buffer.Length > lastQuote && endSequenceChar.Equals(Cursor.Buffer[lastQuote]));

            var start = Cursor.Position;

            // If the next escape if not before the next quote, we can return the string as-is
            Cursor.Advance(lastQuote);

            result = TokenResult.Succeed(Buffer, start.Offset, Cursor.Offset);
            return true;
        }
    }
}