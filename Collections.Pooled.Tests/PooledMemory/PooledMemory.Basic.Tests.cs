//
// Authors:
//   Steven Tolzmann
//
// Copyright (C) 2025 Steven Tolzmann

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Collections.Pooled.Tests.PooledMemory
{
    public class PooledMemory_Basic_Tests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(128)]
        public void Construct_WithCount_ExposesSpanAndMemory(int count)
        {
            using var pm = new Collections.Pooled.PooledMemory<int>(count);

            Assert.Equal(count, pm.Count);
            Assert.Equal(count, pm.Span.Length);
            Assert.Equal(count, pm.Memory.Length);

            if (count > 0)
            {
                pm[0] = 11;
                Assert.Equal(11, pm[0]);

                if (count > 1)
                {
                    pm[count - 1] = 99;
                    Assert.Equal(99, pm[count - 1]);
                }
            }
        }

        [Fact]
        public void Construct_WithNegativeCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Collections.Pooled.PooledMemory<int>(-1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(27)]
        public void Indexer_OutOfRange_ThrowsIndexOutOfRange(int count)
        {
            using var pm = new Collections.Pooled.PooledMemory<int>(count);

            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = pm[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { pm[-1] = 1; });

            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = pm[count]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { pm[count] = 1; });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void Construct_FromSpan_CopiesData(int length)
        {
            var src = Enumerable.Range(0, length).ToArray();
            using var pm = new Collections.Pooled.PooledMemory<int>(src.AsSpan());

            Assert.Equal(length, pm.Count);
            Assert.Equal(src, pm.Span.ToArray());

            if (length > 0)
            {
                pm[0] = 999;
                Assert.Equal(999, pm.Span[0]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        public void Construct_FromEnumerable_CopiesData_ListPath(int length)
        {
            var list = Enumerable.Range(1, length).ToList();
            using var pm = new Collections.Pooled.PooledMemory<int>(list);

            Assert.Equal(length, pm.Count);
            Assert.Equal(list.ToArray(), pm.Span.ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void Construct_FromEnumerable_CopiesData_IReadOnlyCollectionPath(int length)
        {
            var ro = new ReadOnlyCollectionWrapper<int>(Enumerable.Range(0, length).ToArray());
            using var pm = new Collections.Pooled.PooledMemory<int>(ro);

            Assert.Equal(length, pm.Count);
            Assert.Equal(ro.ToArray(), pm.Span.ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void Construct_FromEnumerable_CopiesData_IteratorPath(int length)
        {
            IEnumerable<int> Iter()
            {
                for (int i = 0; i < length; i++) yield return i * 3;
            }

            using var pm = new Collections.Pooled.PooledMemory<int>(Iter());

            Assert.Equal(length, pm.Count);
            Assert.Equal(Enumerable.Range(0, length).Select(x => x * 3), pm.Span.ToArray());
        }

        [Fact]
        public void Construct_FromEnumerable_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Collections.Pooled.PooledMemory<int>((IEnumerable<int>)null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(8)]
        public void Enumeration_Generic_MatchesSpan(int count)
        {
            var data = Enumerable.Range(1, count).ToArray();
            using var pm = new Collections.Pooled.PooledMemory<int>(data.AsSpan());

            Assert.Equal(pm.Span.ToArray(), pm.ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Enumeration_NonGeneric_MatchesSpan(int count)
        {
            var data = Enumerable.Range(10, count).ToArray();
            using var pm = new Collections.Pooled.PooledMemory<int>(data.AsSpan());

            var expected = data;
            var actual = new List<int>();

            foreach (var o in (IEnumerable)pm)
                actual.Add((int)o);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Dispose_IsIdempotent()
        {
            var pm = new Collections.Pooled.PooledMemory<int>(4);
            pm.Dispose();
            pm.Dispose(); // no throw
        }

        // Simple IReadOnlyCollection wrapper to hit the IReadOnlyCollection<T> code-path.
        private sealed class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            public ReadOnlyCollectionWrapper(T[] items) => _items = items ?? Array.Empty<T>();
            public int Count => _items.Length;
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
            public T[] ToArray() => _items.ToArray();
        }
    }
}