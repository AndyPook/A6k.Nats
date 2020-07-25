using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Bedrock.Framework.Protocols;

namespace A6k.Nats.Protocol
{
    public delegate void NatsWriterDelegate<TItem>(TItem item, ref NatsWriter writer);

    public ref partial struct NatsWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt(int num)
        {
            WriteString(num.ToString());
        }

        /// <summary>
        /// Represents a sequence of characters. First the length N is given as an INT16.
        /// Then N bytes follow which are the UTF-8 encoding of the character sequence.
        /// Length must not be negative.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="text"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var textLength = Encoding.UTF8.GetByteCount(text);
            var textSpan = GetSpan(textLength);
            Encoding.UTF8.GetBytes(text, textSpan);
            Advance(textLength);
        }
        public void WriteJson<T>(T data)
        {
            using var json = new Utf8JsonWriter(output);
            JsonSerializer.Serialize<T>(json, data);
            json.Flush();
            Advance((int)json.BytesCommitted);
        }
    }

    /// <summary>
    /// A fast access struct that wraps <see cref="IBufferWriter{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of element to be written.</typeparam>
    public ref partial struct NatsWriter
    {
        /// <summary>
        /// The underlying <see cref="IBufferWriter{T}"/>.
        /// </summary>
        private readonly IBufferWriter<byte> output;

        /// <summary>
        /// The result of the last call to <see cref="IBufferWriter{T}.GetSpan(int)"/>, less any bytes already "consumed" with <see cref="Advance(int)"/>.
        /// Backing field for the <see cref="Span"/> property.
        /// </summary>
        private Span<byte> span;

        /// <summary>
        /// The number of uncommitted bytes (all the calls to <see cref="Advance(int)"/> since the last call to <see cref="Commit"/>).
        /// </summary>
        private int buffered;

        /// <summary>
        /// The total number of bytes written with this writer.
        /// Backing field for the <see cref="BytesCommitted"/> property.
        /// </summary>
        private long bytesCommitted;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferWriter{T}"/> struct.
        /// </summary>
        /// <param name="output">The <see cref="IBufferWriter{T}"/> to be wrapped.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NatsWriter(IBufferWriter<byte> output)
        {
            buffered = 0;
            bytesCommitted = 0;
            this.output = output;
            span = output.GetSpan();
        }

        /// <summary>
        /// Calls <see cref="IBufferWriter{T}.Advance(int)"/> on the underlying writer
        /// with the number of uncommitted bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            var buffered = this.buffered;
            if (buffered > 0)
            {
                bytesCommitted += buffered;
                this.buffered = 0;
                output.Advance(buffered);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int count)
        {
            Ensure(count);
            return span.Slice(0, count);
        }

        /// <summary>
        /// Used to indicate that part of the buffer has been written to.
        /// </summary>
        /// <param name="count">The number of bytes written to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            buffered += count;
            span = span.Slice(count);
        }

        /// <summary>
        /// Copies the caller's buffer into this writer and calls <see cref="Advance(int)"/> with the length of the source buffer.
        /// </summary>
        /// <param name="source">The buffer to copy in.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(in ReadOnlySpan<byte> source)
        {
            if (span.Length >= source.Length)
            {
                source.CopyTo(span);
                Advance(source.Length);
            }
            else
                WriteMultiBuffer(source);
        }
        public void Write(in ReadOnlySequence<byte> source)
        {
            foreach (var span in source)
                Write(span.Span);
        }

        /// <summary>
        /// Acquires a new buffer if necessary to ensure that some given number of bytes can be written to a single buffer.
        /// </summary>
        /// <param name="count">The number of bytes that must be allocated in a single buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ensure(int count = 1)
        {
            if (span.Length < count)
                EnsureMore(count);
        }

        /// <summary>
        /// Gets a fresh span to write to, with an optional minimum size.
        /// </summary>
        /// <param name="count">The minimum size for the next requested buffer.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureMore(int count = 0)
        {
            if (buffered > 0)
                Commit();

            span = output.GetSpan(count);
        }

        /// <summary>
        /// Copies the caller's buffer into this writer, potentially across multiple buffers from the underlying writer.
        /// </summary>
        /// <param name="source">The buffer to copy into this writer.</param>
        private void WriteMultiBuffer(ReadOnlySpan<byte> source)
        {
            while (source.Length > 0)
            {
                if (span.Length == 0)
                    EnsureMore();

                var writable = Math.Min(source.Length, span.Length);
                source.Slice(0, writable).CopyTo(span);
                source = source.Slice(writable);
                Advance(writable);
            }
        }
    }
}
