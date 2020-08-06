using System;
using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Bedrock.Framework.Infrastructure;

namespace A6k.Nats.Protocol
{
    public static class NatsBufferWriterExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> GetSpan<T>(ref this BufferWriter<T> buffer, int count)
             where T : IBufferWriter<byte>
        {
            buffer.Ensure(count);
            return buffer.Span.Slice(0, count);
        }

        /// <summary>
        /// Write integer as a string.
        /// </summary>
        /// <param name="num"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt<T>(ref this BufferWriter<T> buffer, int num)
             where T : IBufferWriter<byte>
        {
            const int MaxLength = 11; // The string "-2147483647" (int.MinValue)

            var textSpan = buffer.GetSpan(MaxLength);
            if (Utf8Formatter.TryFormat(num, textSpan, out int bytesWritten))
                buffer.Advance(bytesWritten);
        }

        /// <summary>
        /// Represents a sequence of characters. First the length N is given as an INT16.
        /// Then N bytes follow which are the UTF-8 encoding of the character sequence.
        /// Length must not be negative.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="text"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString<T>(ref this BufferWriter<T> buffer, string text)
             where T : IBufferWriter<byte>
        {
            if (string.IsNullOrEmpty(text))
                return;

            var textLength = Encoding.UTF8.GetByteCount(text);
            var textSpan = buffer.GetSpan(textLength);
            Encoding.UTF8.GetBytes(text, textSpan);
            buffer.Advance(textLength);
        }

        public static void WriteJson<T, TOut>(ref this BufferWriter<TOut> buffer, T data)
             where TOut : IBufferWriter<byte>
        {
            var jsonBuffer = new ArrayBufferWriter<byte>();
            using var json = new Utf8JsonWriter(jsonBuffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = false });
            JsonSerializer.Serialize(json, data, new JsonSerializerOptions { IgnoreNullValues = true });
            buffer.Write(jsonBuffer.WrittenSpan);
        }
    }
}
