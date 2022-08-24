using System;
using System.Collections;
using System.Collections.Generic;

namespace Parlot
{
    public readonly struct BufferSpan<T> : IEquatable<T[]>, IEquatable<BufferSpan<T>>, IEnumerable<T>
    where T : IEquatable<T>
    {
        public BufferSpan(T[] buffer, int offset = 0, int count = -1)
        {
            Buffer = buffer;
            Offset = offset;
            Length = count == -1 ? buffer?.Length ?? 0 : count;
        }

        public BufferSpan(Span<T> buffer, int offset = 0, int count = -1)
        : this(buffer.ToArray(), offset, count == -1 ? buffer.Length : count)
        {
        }

        public BufferSpan(ReadOnlySpan<T> buffer, int offset = 0, int count = -1)
        : this(buffer.ToArray(), offset, count == -1 ? buffer.Length : count)
        {
        }

        public T this[int i]
        {
            get { return Buffer[Offset + i]; }
        }

        public BufferSpan<T> SubBuffer(int start, int length)
        {
            return new(Buffer, start + Offset, length);
        }
        public T[] ToArray()
        {
            T[] result = new T[Length];
            Array.Copy(Buffer, Offset, result, 0, Length);
            return result;
        }

        public readonly int Length;
        public readonly int Offset;
        public readonly T[] Buffer;

        public ReadOnlySpan<T> Span => Buffer == null ? ReadOnlySpan<T>.Empty : Buffer.AsSpan(Offset, Length);

        public ReadOnlySpan<T> AsSpan(int offset, int length) => Buffer == null ? ReadOnlySpan<T>.Empty : Buffer.AsSpan(Offset + offset, length);
        public ReadOnlySpan<T> AsSpan(int offset) => AsSpan(offset, Length - offset);

        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                if (Buffer == null)
                    return null;
                return new string((char[])(object)Buffer, Offset, Length);
            }
            if (typeof(T) == typeof(byte))
            {
                if (Buffer == null)
                    return null;
                StringBuilder sb = new StringBuilder();
                var offsetLength = Offset + Length;
                var isFirstLine = true;
                for (var i = Offset - (Offset & 0xF); i < offsetLength; i++)
                {
                    if (i > 0 && (i & 0xF) == 0)
                        if (isFirstLine)
                            isFirstLine = false;
                        else
                            sb.Append(Environment.NewLine);
                    else
                        isFirstLine = false;
                    if (i < Offset)
                        sb.Append("  ");
                    else
                        sb.Append(Convert.ToByte(Buffer[i]).ToString("X2"));
                    sb.Append(' ');
                }
                return sb.ToString();
            }
            return base.ToString();
        }

        public bool Equals(T[] other)
        {
            if (other == null)
            {
                return Buffer == null;
            }

            return Span.SequenceEqual(other);
        }

        public bool Equals(BufferSpan<T> other)
        {
            return Span.SequenceEqual(other.Span);
        }

        public static implicit operator BufferSpan<T>(Span<T> s)
        {
            return new BufferSpan<T>(s, 0, s.Length);
        }

        public static implicit operator BufferSpan<T>(ReadOnlySpan<T> s)
        {
            return new BufferSpan<T>(s.ToArray());
        }

        public static implicit operator BufferSpan<T>(T[] s)
        {
            return new BufferSpan<T>(s, 0, s.Length);
        }

        public int IndexOf(T lookup, int startOffset = 0, int end = -1)
        {
            // #if NETSTANDARD2_0
            if (end == -1 || end > Length)
                end = Length;
            for (var i = startOffset + Offset; i < end; i++)
            {
                if (Buffer[i].Equals(lookup))
                    return i - startOffset;
            }

            return -1;
            // #else
            //             return Span.IndexOf(startChar, startOffset, end);
            // #endif
        }

        public int IndexOfAny(params T[] startChar)
        {
            return IndexOfAny(0, startChar);
        }
        public int IndexOfAny(int offset, params T[] startChar)
        {
            // #if NETSTANDARD2_0
            for (var i = Offset + offset; i < Length; i++)
            {
                if (Array.IndexOf(startChar, Buffer[i]) > -1)
                    return i - offset;
            }

            return -1;
            // #else
            //             return Span.IndexOf(startChar, startOffset, end);
            // #endif
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = Offset; i < Length; i++)
            {
                yield return Buffer[i];
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
