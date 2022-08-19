namespace Parlot
{
    using System;
    using System.Runtime.CompilerServices;

    public static class TokenResult
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenResult<T> Succeed<T>(BufferSpan<T> buffer, int start, int end)
    where T : IEquatable<T>
        => TokenResult<T>.Succeed(buffer, start, end);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenResult<T> Fail<T>()
            where T : IEquatable<T>
        => TokenResult<T>.Fail();
    }
    public readonly struct TokenResult<T>
    where T : IEquatable<T>
    {
        private readonly BufferSpan<T> _buffer;

        public readonly int Start;
        public readonly int Length;

        private TokenResult(BufferSpan<T> buffer, int start, int length)
        {
            _buffer = buffer;
            Start = start;
            Length = length;
        }

        public BufferSpan<T> GetBuffer() => _buffer.SubBuffer(Start, Length);

        public ReadOnlySpan<T> Span => _buffer.AsSpan(Start, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenResult<T> Succeed(BufferSpan<T> buffer, int start, int end)
        {
            return new(buffer, start, end - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenResult<T> Fail() => default;
    }
}