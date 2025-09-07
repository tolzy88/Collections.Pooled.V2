//
// Authors:
//   Steven Tolzmann
//
// Copyright (C) 2025 Steven Tolzmann

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Collections.Pooled;
using Xunit;

namespace Collections.Pooled.Tests.PooledMemory
{
    public class PooledMemory_EnumerationTests
    {
        [Fact]
        public void PatternBasedEnumerator_Int32_ReturnsExpectedSequence()
        {
            int[] source = { 1, 2, 3, 4, 5 };
            using var mem = new PooledMemory<int>(span: source);

            var results = new List<int>();
            foreach (var item in mem) // uses public GetEnumerator() => Span<T>.Enumerator
                results.Add(item);

            Assert.Equal(source, results);
        }

        [Fact]
        public void IEnumerableT_Enumerator_Int32_ReturnsExpectedSequence()
        {
            int[] source = { -1, 0, 1, 2 };
            using var mem = new PooledMemory<int>(span: source);

            var results = new List<int>();
            foreach (var item in (IEnumerable<int>)mem) // uses explicit IEnumerable<T>.GetEnumerator()
                results.Add(item);

            Assert.Equal(source, results);
        }

        [Fact]
        public void IEnumerable_Enumerator_Int32_ReturnsExpectedSequence()
        {
            int[] source = { 10, 20, 30 };
            using var mem = new PooledMemory<int>(span: source);

            var results = new List<object>();
            foreach (var item in (IEnumerable)mem) // uses explicit IEnumerable.GetEnumerator()
                results.Add(item!);

            Assert.Equal(source.Cast<object>(), results);
        }

        [Fact]
        public void PatternBasedEnumerator_ReferenceTypes_ReturnsExpectedSequence()
        {
            string[] source = { "alpha", "beta", "gamma" };
            using var mem = new PooledMemory<string>(span: source);

            var results = new List<string>();
            foreach (var s in mem) // uses public GetEnumerator() => Span<T>.Enumerator
                results.Add(s);

            Assert.Equal(source, results);
        }

        [Fact]
        public void Enumerate_Empty_ReturnsNoItems()
        {
            using var mem = new PooledMemory<int>(0);

            // Pattern-based
            int countPattern = 0;
            foreach (var _ in mem)
                countPattern++;
            Assert.Equal(0, countPattern);

            // IEnumerable<T>
            int countGeneric = 0;
            foreach (var _ in (IEnumerable<int>)mem)
                countGeneric++;
            Assert.Equal(0, countGeneric);

            // IEnumerable (non-generic)
            int countNonGeneric = 0;
            foreach (var _ in (IEnumerable)mem)
                countNonGeneric++;
            Assert.Equal(0, countNonGeneric);
        }
    }
}
