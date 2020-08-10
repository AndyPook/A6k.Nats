using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using A6k.Nats.Operations;
using Bedrock.Framework.Infrastructure;
using Bedrock.Framework.Protocols;

namespace A6k.Nats.Protocol
{
    public class NatsOperationReader : IMessageReader<NatsOperation>
    {
        private const byte SP = (byte)' ';
        private const byte HT = (byte)'\t';
        private const byte CR = (byte)'\r';
        private const byte LF = (byte)'\n';
        private static readonly byte[] Empty = new byte[0];

        private static ReadOnlySpan<byte> AnyDelimiter => new byte[] { SP, HT, CR, LF };
        private static ReadOnlySpan<byte> SPorHT => new byte[] { SP, HT };
        private static ReadOnlySpan<byte> CRorLF => new byte[] { CR, LF };

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out NatsOperation message)
        {
            message = default;
            var reader = new SequenceReader<byte>(input);
            if (!reader.TryReadToAny(out ReadOnlySequence<byte> opBuffer, AnyDelimiter))
                return false;

            var opId = NatsOperation.GetOpId(opBuffer.ToSpan());
            switch (opId)
            {
                case NatsOperationId.OK:
                case NatsOperationId.PING:
                case NatsOperationId.PONG:
                    reader.AdvancePastAny(AnyDelimiter);
                    examined = consumed = reader.Position;
                    message = new NatsOperation(opId);
                    return true;
                case NatsOperationId.ERR:
                    if (!TryParseErr(ref reader, out var err))
                        return false;
                    message = new NatsOperation(opId, err);
                    break;
                case NatsOperationId.INFO:
                    if (!TryParseInfo(ref reader, out var info))
                        return false;
                    message = new NatsOperation(opId, info);
                    break;
                case NatsOperationId.MSG:
                    if (!TryParseMsg(ref reader, out var msg))
                        return false;
                    message = new NatsOperation(opId, msg);
                    break;

                default:
                    throw new InvalidOperationException($"unknown operation {opId}");
            }

            reader.AdvancePastAny(AnyDelimiter);

            examined = consumed = reader.Position;
            return true;
        }

        private static string ReadString(ref SequenceReader<byte> reader)
        {
            var arg = ReadArg(ref reader);
            return Encoding.UTF8.GetString(arg.ToSpan());
        }

        private static int ReadNumber(ReadOnlySequence<byte> buffer)
        {
            var span = buffer.ToSpan();
            int result = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                result <<= 8;
                int n = span[i] - 0x30;
                if (n < 0) break;
                result += n;
            }
            return result;
        }
        private static bool TryReadBytes(ref SequenceReader<byte> reader, int length, out ReadOnlySpan<byte> value)
        {
            if (length <= 0)
            {
                value = Span<byte>.Empty;
                return true;
            }

            var span = reader.UnreadSpan;
            if (span.Length < length)
                return TryReadMultisegmentBytes(ref reader, length, out value);

            value = span.Slice(0, length);
            reader.Advance(length);
            return true;
        }
        private static unsafe bool TryReadMultisegmentBytes(ref SequenceReader<byte> reader, int length, out ReadOnlySpan<byte> value)
        {
            Debug.Assert(reader.UnreadSpan.Length < length);

            // Not enough data in the current segment, try to peek for the data we need.
            // In my use case, these strings cannot be more than 64kb, so stack memory is fine.
            byte* buffer = stackalloc byte[length];
            // Hack because the compiler thinks reader.TryCopyTo could store the span.
            var tempSpan = new Span<byte>(buffer, length);

            if (!reader.TryCopyTo(tempSpan))
            {
                value = default;
                return false;
            }

            value = tempSpan;
            reader.Advance(length);
            return true;
        }


        private static bool TryReadLine(ref SequenceReader<byte> reader, out ReadOnlySequence<byte> line)
        {
            if (!reader.TryReadToAny(out line, CRorLF))
                return false;
            reader.AdvancePastAny(CRorLF);
            return true;
        }

        private static bool TryParseErr(ref SequenceReader<byte> reader, out ErrOperation err)
        {
            err = default;
            if (!TryReadLine(ref reader, out var buffer))
                return false;

            var msg = Encoding.UTF8.GetString(buffer.ToSpan());
            err = new ErrOperation(msg);
            return true;
        }

        private static bool TryParseInfo(ref SequenceReader<byte> reader, out ServerInfo info)
        {
            info = default;
            if (!TryReadLine(ref reader, out var buffer))
                return false;

            var infoReader = new Utf8JsonReader(buffer);
            info = JsonSerializer.Deserialize<ServerInfo>(ref infoReader);
            return true;
        }

        private static bool TryParseMsg(ref SequenceReader<byte> reader, out MsgOperation msg)
        {
            msg = default;
            if (!TryReadLine(ref reader, out var fields))
                return false;

            var fieldReader = new SequenceReader<byte>(fields);
            var subject = ReadString(ref fieldReader);
            ConsumeDelimiter(ref fieldReader);
            var sid = ReadString(ref fieldReader);
            string replyTo = null;
            var delimiter = ConsumeDelimiter(ref fieldReader);
            var arg = ReadArg(ref fieldReader);
            if (delimiter == SP)
            {
                delimiter = ConsumeDelimiter(ref fieldReader);
                if (delimiter == SP)
                {
                    replyTo = Encoding.UTF8.GetString(arg.ToSpan());
                    delimiter = ConsumeDelimiter(ref fieldReader);
                }
                if (delimiter == CR)
                    arg = ReadArgFinal(ref fieldReader);
            }
            var numBytes = ReadNumber(arg);

            if (!TryReadBytes(ref reader, numBytes, out var data))
                return false;

            msg = new MsgOperation(subject, sid, replyTo, numBytes, data.ToArray());
            return true;
        }

        private static ReadOnlySequence<byte> ReadArg(ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadToAny(out ReadOnlySequence<byte> arg, AnyDelimiter, advancePastDelimiter: false))
                return reader.Sequence.Slice(reader.Position);
            return arg;
        }
        private static ReadOnlySequence<byte> ReadArgFinal(ref SequenceReader<byte> reader)
        {
            return reader.Sequence.Slice(reader.Position, reader.Remaining);
        }

        private static byte ConsumeDelimiter(ref SequenceReader<byte> reader)
        {
            if (reader.End)
                return CR;
            byte delimiter = CR;
            while (!reader.End)
            {
                if (!reader.TryPeek(out var c))
                    break;
                if (AnyDelimiter.IndexOf(c) == -1)
                    break;
                delimiter = c;
                reader.Advance(1);
            }
            return delimiter switch
            {
                LF => CR,
                HT => SP,
                _ => delimiter
            };
        }
    }
}
