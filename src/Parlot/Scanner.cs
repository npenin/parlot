﻿using System;

namespace Parlot
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// This class is used to return tokens extracted from the input buffer.
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
            Buffer = buffer.Buffer ?? throw new ArgumentNullException(nameof(buffer));
            Cursor = new Cursor<TChar>(Buffer, TextPosition.Start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadFirstThenOthers(Func<TChar, bool> first, Func<TChar, bool> other)
            => ReadFirstThenOthers(first, other, out _);

        public bool ReadFirstThenOthers(Func<TChar, bool> first, Func<TChar, bool> other, out BufferSpan<TChar> result)
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
        public bool ReadN(ulong length, out BufferSpan<TChar> result)
        {
            if (!Cursor.Eof)
            {
                result = TokenResult.Fail<TChar>();
                return false;
            }

            var start = Cursor.Offset;

            // At this point we have an identifier, read while it's an identifier part.
            for (ulong i = 0; i < length; i++)
            {
                if (!Cursor.AdvanceOnce())
                {
                    result = TokenResult.Fail<TChar>();
                    return false;
                }
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
        public bool ReadWhile(Func<TChar, bool> predicate, out BufferSpan<TChar> result)
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
        /// Reads the specified text.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TChar ReadSingle()
        {
            var current = Cursor.Current;
            Cursor.Advance();
            return current;
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
        public bool ReadChar(TChar c, out BufferSpan<TChar> result)
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
        public bool ReadNonEscapableSequence(TChar startSequenceChar, TChar endSequenceChar, out BufferSpan<TChar> result)
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

                lastQuote = nextQuote + 1;
            }
            while (Cursor.Buffer.Length > lastQuote && endSequenceChar.Equals(Cursor.Buffer[lastQuote]));

            var start = Cursor.Position;

            // If the next escape if not before the next quote, we can return the string as-is
            Cursor.Advance(nextQuote + 2 - startOffset);

            result = TokenResult.Succeed(Buffer, start.Offset, Cursor.Offset);
            return true;
        }
    }

    public static class CharScannerExtensions
    {

        /// <summary>
        /// Reads any whitespace without generating a token.
        /// </summary>
        /// <returns>Whether some white space was read.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipWhiteSpaceOrNewLine(this Scanner<char> scanner)
        {
            if (!Character.IsWhiteSpaceOrNewLine(scanner.Cursor.Current))
            {
                return false;
            }

            return SkipWhiteSpaceOrNewLineUnlikely(scanner);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SkipWhiteSpaceOrNewLineUnlikely(this Scanner<char> scanner)
        {
            scanner.Cursor.Advance();

            while (Character.IsWhiteSpaceOrNewLine(scanner.Cursor.Current))
            {
                scanner.Cursor.Advance();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipWhiteSpace(this Scanner<char> scanner)
        {
            bool found = false;
            while (Character.IsWhiteSpace(scanner.Cursor.Current))
            {
                scanner.Cursor.AdvanceOnce();
                found = true;
            }

            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadIdentifier(this Scanner<char> scanner) => ReadIdentifier(scanner, out _);

        public static bool ReadIdentifier(this Scanner<char> scanner, out BufferSpan<char> result)
        {
            // perf: using Character.IsIdentifierStart instead of x => Character.IsIdentifierStart(x) induces some allocations

            return scanner.ReadFirstThenOthers(static x => Character.IsIdentifierStart(x), static x => Character.IsIdentifierPart(x), out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadDecimal(this Scanner<char> scanner, System.Globalization.NumberStyles options, System.Globalization.NumberFormatInfo culture) => ReadDecimal(scanner, options, culture, out _);

        public static bool ReadDecimal(this Scanner<char> scanner, System.Globalization.NumberStyles options, System.Globalization.NumberFormatInfo culture, out BufferSpan<char> result)
        {
            // perf: fast path to prevent a copy of the position

            if (options.HasFlag(System.Globalization.NumberStyles.AllowLeadingSign))
            {
                if (!scanner.ReadChar('-'))
                {
                    // If there is no '-' try to read a '+' but don't read both.
                    scanner.ReadChar('+');
                }
            }

            var start = scanner.Cursor.Position;
            if (options.HasFlag(System.Globalization.NumberStyles.AllowHexSpecifier))
            {

                if (!Character.IsHexDigit(scanner.Cursor.Current))
                {
                    result = TokenResult.Fail<char>();
                    return false;
                }
                do
                {
                    scanner.Cursor.Advance();

                } while (!scanner.Cursor.Eof && (Character.IsHexDigit(scanner.Cursor.Current)));
            }
            else
            {

                if (!Character.IsDecimalDigit(scanner.Cursor.Current))
                {
                    result = TokenResult.Fail<char>();
                    return false;
                }
                do
                {
                    scanner.Cursor.Advance();

                } while (!scanner.Cursor.Eof && (Character.IsDecimalDigit(scanner.Cursor.Current)));

                if (scanner.Cursor.Match(culture.CurrencyDecimalSeparator))
                {
                    scanner.Cursor.Advance();

                    if (!Character.IsDecimalDigit(scanner.Cursor.Current))
                    {
                        result = TokenResult.Fail<char>();
                        scanner.Cursor.ResetPosition(start);
                        return false;
                    }

                    do
                    {
                        scanner.Cursor.Advance();

                    } while (!scanner.Cursor.Eof && Character.IsDecimalDigit(scanner.Cursor.Current));
                }

                if (options.HasFlag(System.Globalization.NumberStyles.AllowExponent) && scanner.Cursor.Match('E'))
                {
                    scanner.Cursor.Advance();

                    if (!Character.IsDecimalDigit(scanner.Cursor.Current))
                    {
                        result = TokenResult.Fail<char>();
                        scanner.Cursor.ResetPosition(start);
                        return false;
                    }

                    do
                    {
                        scanner.Cursor.Advance();

                    } while (!scanner.Cursor.Eof && Character.IsDecimalDigit(scanner.Cursor.Current));
                }
            }
            result = TokenResult.Succeed(scanner.Buffer, start.Offset, scanner.Cursor.Offset);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadNonWhiteSpace(this Scanner<char> scanner) => ReadNonWhiteSpace(scanner, out _);

        public static bool ReadNonWhiteSpace(this Scanner<char> scanner, out BufferSpan<char> result)
        {
            return scanner.ReadWhile(static x => !Character.IsWhiteSpace(x), out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadNonWhiteSpaceOrNewLine(this Scanner<char> scanner) => ReadNonWhiteSpaceOrNewLine(scanner, out _);

        public static bool ReadNonWhiteSpaceOrNewLine(this Scanner<char> scanner, out BufferSpan<char> result)
        {
            return scanner.ReadWhile(static x => !Character.IsWhiteSpaceOrNewLine(x), out result);
        }
        /// <summary>
        /// Reads the specific expected text.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadText(this Scanner<char> scanner, string text, StringComparer comparer) => ReadText(scanner, text, comparer, out _);

        /// <summary>
        /// Reads the specific expected text.
        /// </summary>
        public static bool ReadText(this Scanner<char> scanner, string text, StringComparer comparer, out BufferSpan<char> result)
        {
            var match = comparer is null
                ? scanner.Cursor.Match(text)
                : scanner.Cursor.Match(text, comparer);

            if (!match)
            {
                result = TokenResult.Fail<char>();
                return false;
            }

            int start = scanner.Cursor.Offset;
            scanner.Cursor.Advance(text.Length);
            result = TokenResult.Succeed(scanner.Buffer, start, scanner.Cursor.Offset);

            return true;
        }

        /// <summary>
        /// Reads the specific expected text.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadText(this Scanner<char> scanner, string text) => ReadText(scanner, text, out _);

        /// <summary>
        /// Reads the specific expected text.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadText(this Scanner<char> scanner, string text, out BufferSpan<char> result) => ReadText(scanner, text, comparer: null, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadSingleQuotedString(this Scanner<char> scanner) => ReadSingleQuotedString(scanner, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadSingleQuotedString(this Scanner<char> scanner, out BufferSpan<char> result)
        {
            return ReadQuotedString(scanner, '\'', out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadDoubleQuotedString(this Scanner<char> scanner) => ReadDoubleQuotedString(scanner, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadDoubleQuotedString(this Scanner<char> scanner, out BufferSpan<char> result)
        {
            return ReadQuotedString(scanner, '\"', out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadQuotedString(this Scanner<char> scanner) => ReadQuotedString(scanner, out _);

        public static bool ReadQuotedString(this Scanner<char> scanner, out BufferSpan<char> result)
        {
            var startChar = scanner.Cursor.Current;

            if (startChar != '\'' && startChar != '\"')
            {
                result = TokenResult.Fail<char>();
                return false;
            }

            return ReadQuotedString(scanner, startChar, out result);
        }

        /// <summary>
        /// Reads a string token enclosed in single or double quotes.
        /// </summary>
        /// <remarks>
        /// This method doesn't escape the string, but only validates its content is syntactically correct.
        /// The resulting Span contains the original quotes.
        /// </remarks>
        private static bool ReadQuotedString(this Scanner<char> scanner, char quoteChar, out BufferSpan<char> result)
        {
            var startChar = scanner.Cursor.Current;

            if (startChar != quoteChar)
            {
                result = TokenResult.Fail<char>();
                return false;
            }

            // Fast path if there aren't any escape char until next quote
            var start = scanner.Cursor.Position;

            var nextQuote = scanner.Cursor.Buffer.IndexOf(startChar, start.Offset + 1);

            if (nextQuote == -1)
            {
                // There is no end quote, not a string
                result = TokenResult.Fail<char>();
                return false;
            }

            scanner.Cursor.Advance();

            var nextEscape = scanner.Cursor.Buffer.IndexOf('\\', start.Offset, nextQuote - start.Offset);

            // If the next escape if not before the next quote, we can return the string as-is
            if (nextEscape == -1 || nextEscape > nextQuote)
            {
                scanner.Cursor.Advance(nextQuote - start.Offset);

                result = TokenResult.Succeed(scanner.Buffer, start.Offset, scanner.Cursor.Offset);
                return true;
            }

            while (!scanner.Cursor.Match(startChar))
            {
                // We can read Eof if there is an escaped quote sequence and no actual end quote, e.g. "'abc\'def"
                if (scanner.Cursor.Eof)
                {
                    result = TokenResult.Fail<char>();
                    return false;
                }

                if (scanner.Cursor.Match('\\'))
                {
                    scanner.Cursor.Advance();

                    switch (scanner.Cursor.Current)
                    {
                        case '0':
                        case '\\':
                        case 'b':
                        case 'f':
                        case 'n':
                        case 'r':
                        case 't':
                        case 'v':
                        case '\'':
                        case '"':
                            break;

                        case 'u':

                            // https://stackoverflow.com/a/32175520/142772
                            // exactly 4 digits

                            var isValidUnicode = false;

                            scanner.Cursor.Advance();

                            if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.Current))
                            {
                                scanner.Cursor.Advance();
                                if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.Current))
                                {
                                    scanner.Cursor.Advance();
                                    if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.Current))
                                    {
                                        scanner.Cursor.Advance();
                                        if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.Current))
                                        {
                                            isValidUnicode = true;
                                        }
                                    }
                                }
                            }

                            if (!isValidUnicode)
                            {
                                scanner.Cursor.ResetPosition(start);

                                result = TokenResult.Fail<char>();
                                return false;
                            }

                            break;
                        case 'x':

                            // https://stackoverflow.com/a/32175520/142772
                            // exactly 4 digits

                            bool isValidHex = false;

                            scanner.Cursor.Advance();

                            if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.Current))
                            {
                                isValidHex = true;

                                if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.PeekNext()))
                                {
                                    scanner.Cursor.Advance();

                                    if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.PeekNext()))
                                    {
                                        scanner.Cursor.Advance();

                                        if (!scanner.Cursor.Eof && Character.IsHexDigit(scanner.Cursor.PeekNext()))
                                        {
                                            scanner.Cursor.Advance();
                                        }
                                    }
                                }
                            }

                            if (!isValidHex)
                            {
                                scanner.Cursor.ResetPosition(start);

                                result = TokenResult.Fail<char>();
                                return false;
                            }

                            break;
                        default:
                            scanner.Cursor.ResetPosition(start);

                            result = TokenResult.Fail<char>();
                            return false;
                    }
                }

                scanner.Cursor.Advance();
            }

            scanner.Cursor.Advance();

            result = TokenResult.Succeed(scanner.Buffer, start.Offset, scanner.Cursor.Offset);

            return true;
        }
    }
}