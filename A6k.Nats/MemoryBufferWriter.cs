using System;
using System.Buffers;
using System.Diagnostics;

#nullable enable

namespace A6k.Nats
{
    // borrowed from https://github.com/yigolden/TiffLibrary

    public sealed class MemoryBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private MemoryPool<byte> memoryPool;
        private BufferSegment? head;
        private BufferSegment? current;

        public int Length { get; private set; }

        public MemoryBufferWriter(MemoryPool<byte>? memoryPool = null)
        {
            this.memoryPool = memoryPool ?? MemoryPool<byte>.Shared;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => GetBufferSegment(sizeHint).AvailableMemory;

        public Span<byte> GetSpan(int sizeHint = 0) => GetBufferSegment(sizeHint).AvailableSpan;

        private BufferSegment GetBufferSegment(int sizeHint)
        {
            BufferSegment? temp = current;
            if (temp is null)
                head = temp = current = new BufferSegment(0, memoryPool.Rent(Math.Max(sizeHint, 16384)));
            if (sizeHint < temp.AvailableLength)
                return temp;

            Debug.Assert(current != null);
            temp = new BufferSegment(Length, memoryPool.Rent(Math.Max(sizeHint, 16384)));
            temp!.SetNext(temp);
            current = temp;

            return temp;
        }

        public void Advance(int count)
        {
            BufferSegment? temp = current;
            if (temp is null)
                throw new InvalidOperationException();
            if (count > temp.AvailableLength)
                throw new ArgumentOutOfRangeException(nameof(count));

            temp.Advance(count);
            Length += count;
        }

        public byte[] ToArray()
        {
            int totalLength = 0;
            BufferSegment? segment = head;
            while (segment != null)
            {
                totalLength += segment.Length;
                segment = segment.NextSegment;
            }

            byte[] destination = new byte[totalLength];
            int offset = 0;
            segment = head;
            while (segment != null)
            {
                segment.CopyTo(destination.AsSpan(offset));
                offset += segment.Length;
                segment = segment.NextSegment;
            }

            return destination;
        }

        public void CopyTo(Span<byte> destination)
        {
            BufferSegment? segment = head;
            while (segment != null)
            {
                segment.CopyTo(destination);
                destination = destination.Slice(segment.Length);
                segment = segment.NextSegment;
            }
        }
        public void CopyTo(IBufferWriter<byte> output)
        {
            // copy buffer to output
            // Try to minimize segments in the target writer by hinting at the total size.
            output.GetSpan(Length);
            foreach (var segment in AsReadOnlySequence)
                output.Write(segment.Span);
        }

        public ReadOnlySequence<byte> AsReadOnlySequence
        {
            get
            {
                if (head is null || current is null)
                    return ReadOnlySequence<byte>.Empty;

                return new ReadOnlySequence<byte>(head, 0, current, current.Length);
            }
        }
        /// <summary>
        /// Expresses this sequence as a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <param name="sequence">The sequence to convert.</param>
        public static implicit operator ReadOnlySequence<byte>(MemoryBufferWriter writer)
        {
            if (writer.head is null || writer.current is null)
                return ReadOnlySequence<byte>.Empty;

            return new ReadOnlySequence<byte>(writer.head, 0, writer.current, writer.current.Length);
        }

        public void Dispose()
        {
            BufferSegment? segment = head;
            while (segment != null)
                segment = segment.ReturnMemory();

            head = current = null;
            Length = 0;
        }

        private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
        {
            private IMemoryOwner<byte>? memoryOwner;
            private Memory<byte> memory;

            public int Length { get; private set; }

            public int AvailableLength => memory.Length - Length;

            public Memory<byte> AvailableMemory => memory.Slice(Length);

            public Span<byte> AvailableSpan => memory.Span.Slice(Length);

            public BufferSegment? NextSegment { get; private set; }

            public BufferSegment(long runningIndex, IMemoryOwner<byte> memoryOwner)
            {
                this.memoryOwner = memoryOwner;
                memory = memoryOwner.Memory;
                Length = 0;

                Memory = default;
                RunningIndex = runningIndex;
            }

            public void Advance(int count)
            {
                Debug.Assert(count <= AvailableLength);

                Length += count;
                Memory = memory.Slice(0, Length);
            }

            public void SetNext(BufferSegment segment)
            {
                NextSegment = segment;
                Next = segment;
            }

            public void CopyTo(Span<byte> destination)
            {
                memory.Span.Slice(0, Length).CopyTo(destination);
            }

            public BufferSegment? ReturnMemory()
            {
                BufferSegment? next = NextSegment;

                memoryOwner?.Dispose();
                memoryOwner = null;
                memory = default;
                NextSegment = null;

                Memory = default;
                Next = default;

                return next;
            }
        }
    }
}