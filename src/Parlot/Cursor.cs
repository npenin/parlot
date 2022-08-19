using System;
using System.Runtime.CompilerServices;

namespace Parlot
{
    public class Cursor<TChar>
    where TChar : IEquatable<TChar>, IConvertible
    {
        public static readonly TChar NullChar = default(TChar);

        private readonly int _textLength;
        private TChar _current;
        private int _offset;
        private int _line;
        private int _column;
        private readonly BufferSpan<TChar> _buffer;

        public Cursor(BufferSpan<TChar> buffer, in TextPosition position)
        {
            _buffer = buffer;
            _textLength = buffer.Length;
            Eof = _textLength == 0;
            _current = _textLength == 0 ? NullChar : buffer[position.Offset];
            _offset = 0;
            _line = 1;
            _column = 1;
            this.IsChar = typeof(TChar) == typeof(char);
        }

        public readonly bool IsChar;

        public Cursor(BufferSpan<TChar> buffer) : this(buffer, TextPosition.Start)
        {
        }

        public TextPosition Position => new(_offset, _line, _column);

        /// <summary>
        /// Advances the cursor by one character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance()
        {
            _offset++;

            if (_offset >= _textLength)
            {
                Eof = true;
                _column++;
                _current = NullChar;
                return;
            }

            var next = _buffer[_offset];
            if (IsChar)
            {
                var currentAsChar = _current.ToChar(null);
                if (_current.ToChar(null) == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (next.ToChar(null) != '\r')
                {
                    _column++;
                }
            }
            // if c == '\r', don't increase the column count

            _current = next;

        }

        /// <summary>
        /// Advances the cursor.
        /// </summary>
        public void Advance(int count)
        {
            if (Eof)
            {
                return;
            }

            var maxOffset = _offset + count;

            // Detect if the cursor will be over Eof
            if (maxOffset > _textLength - 1)
            {
                Eof = true;
                maxOffset = _textLength - 1;
            }

            while (_offset < maxOffset)
            {
                _offset++;

                var next = _buffer[_offset];

                if (IsChar)
                {
                    if (_current.ToChar(null) == '\n')
                    {
                        _line++;
                        _column = 1;
                    }
                    // if c == '\r', don't increase the column count
                    else if (next.ToChar(null) != '\r')
                    {
                        _column++;
                    }
                }

                _current = next;
            }

            if (Eof)
            {
                _current = NullChar;
                _offset = _textLength;
                _column += 1;
            }

        }

        /// <summary>
        /// Advances the cursor with the knowledge there are no new lines.
        /// </summary>
        public bool AdvanceNoNewLines(int offset)
        {
            var newOffset = _offset + offset;

            // Detect if the cursor will be over Eof
            if (newOffset > _textLength - 1)
            {
                Eof = true;
                _offset = _textLength;
                _current = NullChar;
                return false;
            }

            _current = _buffer[newOffset];
            _offset = newOffset;

            return true;
        }

        /// <summary>
        /// Moves the cursor to the specific position
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetPosition(in TextPosition position)
        {
            if (position.Offset != _offset)
            {
                ResetPositionNotInlined(position);
            }
        }

        private void ResetPositionNotInlined(in TextPosition position)
        {
            _offset = position.Offset;
            _line = position.Line;
            _column = position.Column;

            // Eof might have been recorded
            if (_offset >= _textLength)
            {
                _current = NullChar;
                Eof = true;
            }
            else
            {
                _current = _buffer[_offset];
                Eof = false;
            }
        }

        /// <summary>
        /// Evaluates the char at the current position.
        /// </summary>
        public TChar Current => _current;

        /// <summary>
        /// Returns the cursor's position in the _buffer.
        /// </summary>
        public int Offset => _offset;

        /// <summary>
        /// Evaluates a char forward in the _buffer.
        /// </summary>
        public TChar PeekNext(int index = 1)
        {
            var nextIndex = _offset + index;

            if (nextIndex >= _textLength || nextIndex < 0)
            {
                return NullChar;
            }

            return _buffer[nextIndex];
        }

        public bool Eof { get; private set; }

        public BufferSpan<TChar> Buffer => _buffer;

        /// <summary>
        /// Whether a char is at the current position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Match(TChar c)
        {
            // Ordinal comparison
            return _current.Equals(c);
        }

        /// <summary>
        /// Whether any char of the string is at the current position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchAnyOf(ReadOnlySpan<TChar> s)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(s));
            }

            if (Eof)
            {
                return false;
            }

            var length = s.Length;

            if (length == 0)
            {
                return true;
            }

            for (var i = 0; i < length; i++)
            {
                if (s[i].Equals(_current))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether any char of an array is at the current position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchAny(params TChar[] chars)
        {
            if (chars == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(chars));
            }

            if (Eof)
            {
                return false;
            }

            var length = chars.Length;

            if (length == 0)
            {
                return true;
            }

            for (var i = 0; i < length; i++)
            {
                if (chars[i].Equals(_current))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a string is at the current position.
        /// </summary>
        public bool Match(ReadOnlySpan<TChar> s)
        {
            if (s.Length == 0)
            {
                return true;
            }

            if (Eof)
            {
                return false;
            }

            if (!s[0].Equals(_current))
            {
                return false;
            }

            var length = s.Length;

            if (_offset + length - 1 >= _textLength)
            {
                return false;
            }

            if (length > 1 && !PeekNext(1).Equals(s[1]))
            {
                return false;
            }

            for (var i = 2; i < length; i++)
            {
                if (!s[i].Equals(PeekNext(i)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
