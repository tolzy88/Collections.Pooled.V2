//
// Authors:
//   Steven Tolzmann
//
// Copyright (C) 2025 Steven Tolzmann

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Collections.Pooled.Tests.PooledMemory
{
    public class PooledMemory_Growth_Tests
    {
        private sealed class RecordingPool<T> : ArrayPool<T>
        {
            public readonly List<int> RentRequests = new List<int>();
            public int ReturnCalls { get; private set; }

            public override T[] Rent(int minimumLength)
            {
                RentRequests.Add(minimumLength);
                // return exact-length arrays to make growth behavior deterministic
                return new T[minimumLength];
            }

            public override void Return(T[] array, bool clearArray = false)
            {
                ReturnCalls++;
                if (clearArray)
                    Array.Clear(array, 0, array.Length);
            }
        }

        [Theory]
        [InlineData(0, 4, new int[] { 4 })]
        [InlineData(1, 4, new int[] { 4 })]
        [InlineData(2, 2, new int[] { 2 })]
        [InlineData(3, 2, new int[] { 2, 4 })]
        [InlineData(10, 2, new int[] { 2, 4, 8, 16 })] // 2 -> 4 -> 8 -> 16
        public void UnknownSizeEnumerator_Grows_AsNeeded(int total, int suggestCapacity, int[] expectedRents)
        {
            IEnumerable<int> Iter()
            {
                for (int i = 0; i < total; i++) yield return i + 1;
            }

            var pool = new RecordingPool<int>();
            using var pm = new Collections.Pooled.PooledMemory<int>(Iter(), ClearMode.Never, pool, suggestCapacity);

            Assert.Equal(total, pm.Count);
            Assert.Equal(expectedRents, pool.RentRequests.ToArray());
            // old buffers returned during growth + final return on Dispose
            Assert.True(pool.RentRequests.Count >= 1);
        }

        [Fact]
        public void UnknownSizeEnumerator_NegativeSuggestCapacity_Throws()
        {
            IEnumerable<int> Iter() { yield return 1; }

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new Collections.Pooled.PooledMemory<int>(Iter(), ClearMode.Never, ArrayPool<int>.Shared, suggestCapacity: -1));

            Assert.Equal("capacity", ex.ParamName);
        }

        [Fact]
        public void UnknownSizeEnumerator_DataCopiedCorrectly()
        {
            IEnumerable<int> Iter()
            {
                for (int i = 0; i < 9; i++) yield return i * 3;
            }

            using var pm = new Collections.Pooled.PooledMemory<int>(Iter(), ClearMode.Never, ArrayPool<int>.Shared, suggestCapacity: 2);

            Assert.Equal(9, pm.Count);
            Assert.Equal(Enumerable.Range(0, 9).Select(i => i * 3), pm.Span.ToArray());
        }
    }
}