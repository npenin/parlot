using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Parlot
{
    public abstract class BufferSpanBuilder<T>
    where T : IEquatable<T>
    {
        public BufferSpanBuilder(CultureInfo culture)
        {
            this.Culture = culture;
        }
        private readonly List<T[]> blocks = new List<T[]>();
        public CultureInfo Culture;

        public int Length { get; private set; }

        public BufferSpanBuilder()
        {
        }

#if SUPPORTS_READONLYSPAN
        public void Append(Span<T> s)
        {
            Append((BufferSpan<T>)s);
        }

        public void Append(ReadOnlySpan<T> s)
        {
            Append(s.ToArray());
        }
#endif

        public void Append(BufferSpan<T> s)
        {
            Append(s.Buffer);
        }

        public void Append(T s)
        {
            Append(new[] { s });
        }

        public void Append(T[] s)
        {
            blocks.Add(s);
            Length += s.Length;
        }

        public abstract void Append(int s);

        public abstract void Append(object s);


        public T[] FlattenBlocks()
        {
            var flatten = new T[Length];
            var offset = 0;
            foreach (var block in blocks)
            {
                Array.Copy(block, 0, flatten, offset, block.Length);
                offset += block.Length;
            }
            return flatten;
        }
    }

    public class StringBuilder : BufferSpanBuilder<char>
    {
        public StringBuilder(CultureInfo culture = null)
        : base(culture ?? CultureInfo.InvariantCulture)
        { }

        public void Append(string s)
        {
#if NETSTANDARD2_0
            base.Append(s.ToCharArray());
#else
            base.Append(s.AsSpan());
#endif
        }

        public override void Append(int s)
        {
            Append(s.ToString());
        }

        public override void Append(object s)
        {
            Append(s.ToString());
        }

        public override string ToString()
        {
            if (Length == 0)
                return string.Empty;
            return new string(FlattenBlocks());
        }
    }

    public class BytesBuilder : BufferSpanBuilder<byte>
    {
        private Encoding defaultEncoding;

        public BytesBuilder(CultureInfo culture = null, Encoding defaultEncoding = null)
        : base(culture)
        {
            this.defaultEncoding = defaultEncoding ?? Encoding.UTF8;
        }

        public void Append(string s, Encoding encoding = null)
        {
            base.Append((encoding ?? defaultEncoding).GetBytes(s));
        }

        public override void Append(int s)
        {
            Append(BitConverter.GetBytes(s));
        }

        public override void Append(object s)
        {
            Append(s.ToString());
        }


    }
}
