using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using A6k.Nats.Operations;
using Bedrock.Framework.Infrastructure;
using Bedrock.Framework.Protocols;

namespace A6k.Nats
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

            if (opId == NatsOperationId.OK)
            {
                // +OK
                reader.AdvancePastAny(AnyDelimiter);
                examined = consumed = reader.Position;
                message = new NatsOperation(opId, Empty, null);
                return true;
            }

            if (!reader.TryReadToAny(out ReadOnlySequence<byte> fieldsValue, CRorLF))
                return false;
            reader.AdvancePastAny(AnyDelimiter);

            object op = opId switch
            {
                NatsOperationId.INFO => ParseInfo(fieldsValue),
                NatsOperationId.PING => new PingOperation(),
                NatsOperationId.PONG => new PongOperation(),
                NatsOperationId.OK => new PongOperation(),
                NatsOperationId.ERR => ParseErr(fieldsValue),
                NatsOperationId.MSG => default,

                _ => throw new InvalidOperationException($"unknown operation {opId}")
            };

            if (opId == NatsOperationId.MSG)
            {
                var msg = ParseMsg(fieldsValue);
                if (!TryReadBytes(ref reader, msg.NumBytes, out var data))
                    return false;
                msg.Data = data.ToArray();
                op = msg;
            }

            reader.AdvancePastAny(AnyDelimiter);

            examined = consumed = reader.Position;
            message = new NatsOperation(opId, fieldsValue.ToMemory(), op);
            return true;
        }

        private ReadOnlySequence<byte> ReadArg(ref SequenceReader<byte> reader)
        {
            if (!reader.TryReadToAny(out ReadOnlySequence<byte> arg, AnyDelimiter, advancePastDelimiter: false))
                throw new InvalidOperationException();
            return arg;
        }
        private ReadOnlySequence<byte> ReadArgFinal(ref SequenceReader<byte> reader)
        {
            return reader.Sequence.Slice(reader.Position, reader.Remaining);
        }

        private byte ConsumeDelimiter(ref SequenceReader<byte> reader)
        {
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

        private string ReadString(ref SequenceReader<byte> reader)
        {
            var arg = ReadArg(ref reader);
            return Encoding.UTF8.GetString(arg.ToSpan());
        }

        private int ReadNumber(ReadOnlySequence<byte> buffer)
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

        private ErrOperation ParseErr(ReadOnlySequence<byte> buffer)
        {
            var msg = Encoding.UTF8.GetString(buffer.ToSpan());
            return new ErrOperation { Message = msg };
        }

        private ServerInfo ParseInfo(ReadOnlySequence<byte> buffer)
        {
            var reader = new Utf8JsonReader(buffer);
            var info = JsonSerializer.Deserialize<ServerInfo>(ref reader);
            return info;
        }

        private MsgOperation ParseMsg(ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);
            var subject = ReadString(ref reader);
            string replyTo = null;
            ConsumeDelimiter(ref reader);
            var arg = ReadArg(ref reader);
            var delimiter = ConsumeDelimiter(ref reader);
            if (delimiter == SP)
            {
                replyTo = Encoding.UTF8.GetString(arg.ToSpan());
                delimiter = ConsumeDelimiter(ref reader);
            }
            if (delimiter == CR)
                arg = ReadArgFinal(ref reader);

            var numBytes = ReadNumber(arg);

            return new MsgOperation { Subject = subject, ReplyTo = replyTo, NumBytes = numBytes };
        }
    }
}
