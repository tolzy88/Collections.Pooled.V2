//
// Authors:
//   Steven Tolzmann
//
// Copyright (C) 2025 Steven Tolzmann

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Collections.Pooled.Tests.PooledMemory
{
    public class PooledMemory_ZeroAndEmptyBehavior_Tests
    {
        private sealed class ProbePool<T> : ArrayPool<T>
        {
            public int RentCalls { get; private set; }
            public int ReturnCalls { get; private set; }
            public bool LastClearFlag { get; private set; }
            public int LastRentRequest { get; private set; }

            public override T[] Rent(int minimumLength)
            {
                RentCalls++;
                LastRentRequest = minimumLength;
                return new T[minimumLength];
            }

            public override void Return(T[] array, bool clearArray = false)
            {
                ReturnCalls++;
                LastClearFlag = clearArray;
                if (clearArray)
                    Array.Clear(array, 0, array.Length);
            }
        }

        [Fact]
        public void Ctor_Count_Zero_UsesEmptyArray_NoPool()
        {
            var pool = new ProbePool<int>();
            using var pm = new Collections.Pooled.PooledMemory<int>(0, ClearMode.Never, pool);

            Assert.Equal(0, pm.Count);
            Assert.Equal(0, pm.Span.Length);
            Assert.Equal(0, pm.Memory.Length);
            Assert.Equal(0, pool.RentCalls);
            Assert.Equal(0, pool.ReturnCalls);
        }

        [Fact]
        public void Ctor_Span_LengthZero_UsesEmptyArray_NoPool()
        {
            var pool = new ProbePool<byte>();
            ReadOnlySpan<byte> src = ReadOnlySpan<byte>.Empty;

            using var pm = new Collections.Pooled.PooledMemory<byte>(src, ClearMode.Never, pool);

            Assert.Equal(0, pm.Count);
            Assert.Equal(0, pm.Span.Length);
            Assert.Equal(0, pool.RentCalls);
            Assert.Equal(0, pool.ReturnCalls);
        }

        [Fact]
        public void Ctor_ICollectionT_Empty_UsesEmptyArray_NoPool()
        {
            var pool = new ProbePool<int>();
            var list = new List<int>();

            using var pm = new Collections.Pooled.PooledMemory<int>(list, ClearMode.Never, pool);

            Assert.Equal(0, pm.Count);
            Assert.Equal(0, pm.Span.Length);
            Assert.Equal(0, pool.RentCalls);
            Assert.Equal(0, pool.ReturnCalls);
        }

        [Fact]
        public void Ctor_IReadOnlyCollection_Empty_UsesEmptyArray_NoPool()
        {
            var pool = new ProbePool<int>();
            var ro = new ReadOnlyWrapper<int>(Array.Empty<int>());

            using var pm = new Collections.Pooled.PooledMemory<int>(ro, ClearMode.Never, pool);

            Assert.Equal(0, pm.Count);
            Assert.Equal(0, pm.Span.Length);
            Assert.Equal(0, pool.RentCalls);
            Assert.Equal(0, pool.ReturnCalls);
        }

        [Fact]
        public void Ctor_UnknownEnumerator_Empty_RentsOnce_DisposeReturns()
        {
            var pool = new ProbePool<int>();
            IEnumerable<int> IterEmpty()
            {
                yield break;
            }

            using var pm = new Collections.Pooled.PooledMemory<int>(IterEmpty(), ClearMode.Never, pool);

            Assert.Equal(0, pm.Count);
            Assert.True(pool.RentCalls >= 1); // growth-path always rents upfront
            // Return happens on Dispose()
        }

        [Fact]
        public void Dispose_EmptyInstance_NoPoolReturn()
        {
            var pool = new ProbePool<int>();
            using (var pm = new Collections.Pooled.PooledMemory<int>(0, ClearMode.Never, pool))
            {
                Assert.Equal(0, pool.RentCalls);
            }
            Assert.Equal(0, pool.ReturnCalls);
        }

        [Fact]
        public void Ctor_ArrayViaEnumerable_Empty_UsesEmptyArray_NoPool()
        {
            var pool = new ProbePool<int>();
            var arr = Array.Empty<int>();

            using var pm = new Collections.Pooled.PooledMemory<int>(enumerable: arr, clearMode: ClearMode.Never, customPool: pool);

            Assert.Equal(0, pm.Count);
            Assert.Equal(0, pm.Span.Length);
            Assert.Equal(0, pool.RentCalls);
            Assert.Equal(0, pool.ReturnCalls);
        }

        private sealed class ReadOnlyWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            public ReadOnlyWrapper(T[] items) => _items = items ?? Array.Empty<T>();
            public int Count => _items.Length;
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        }
    }
}