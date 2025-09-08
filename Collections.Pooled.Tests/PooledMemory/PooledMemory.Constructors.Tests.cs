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
    public class PooledMemory_Constructors_Tests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        public void Ctor_Count_CustomPool_IsUsed(int count)
        {
            var pool = new ProbePool<int>(Math.Max(1, count));
            using var pm = new Collections.Pooled.PooledMemory<int>(count, ClearMode.Never, pool);

            if (count != 0) // Empty arrays are not rented from the pool
            {
                Assert.True(pool.RentCalled);
            }
            Assert.Equal(count, pm.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(9)]
        public void Ctor_Span_CustomPool_IsUsed(int length)
        {
            var data = Enumerable.Range(0, length).ToArray();
            var pool = new ProbePool<int>(Math.Max(1, length));

            using var pm = new Collections.Pooled.PooledMemory<int>(data.AsSpan(), ClearMode.Never, pool);

            if (length != 0) // Empty arrays are not rented from the pool
            {
                Assert.True(pool.RentCalled);
            }
            Assert.Equal(data, pm.Span.ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Ctor_Enumerable_ICollectionT_Path(int length)
        {
            var list = Enumerable.Range(0, length).ToList();
            using var pm = new Collections.Pooled.PooledMemory<int>(list);

            Assert.Equal(length, pm.Count);
            Assert.Equal(list, pm.Span.ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        public void Ctor_Enumerable_IReadOnlyCollection_Path(int length)
        {
            var ro = new ReadOnlyCollectionWrapper<int>(Enumerable.Range(100, length).ToArray());
            using var pm = new Collections.Pooled.PooledMemory<int>(ro);

            Assert.Equal(length, pm.Count);
            Assert.Equal(ro.ToArray(), pm.Span.ToArray());
        }

        private sealed class ProbePool<T> : ArrayPool<T>
        {
            private readonly T[] _buffer;
            public bool RentCalled { get; private set; }
            public ProbePool(int size) => _buffer = new T[size];
            public override T[] Rent(int minimumLength)
            {
                RentCalled = true;
                if (minimumLength > _buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(minimumLength));
                return _buffer;
            }
            public override void Return(T[] array, bool clearArray = false) { }
        }

        private sealed class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            public ReadOnlyCollectionWrapper(T[] items) => _items = items ?? Array.Empty<T>();
            public int Count => _items.Length;
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
            public T[] ToArray() => _items.ToArray();
        }

        private sealed class NonGenericCollection<T> : ICollection
        {
            private readonly T[] _items;
            public NonGenericCollection(T[] items) => _items = items ?? Array.Empty<T>();

            public int Count => _items.Length;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public void CopyTo(Array array, int index)
            {
                // Use Array.Copy semantics
                Array.Copy(_items, 0, array, index, _items.Length);
            }

            public IEnumerator GetEnumerator() => _items.GetEnumerator();
        }
    }
}