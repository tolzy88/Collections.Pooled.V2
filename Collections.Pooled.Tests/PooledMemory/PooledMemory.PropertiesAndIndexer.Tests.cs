//
// Authors:
//   Steven Tolzmann
//
// Copyright (C) 2025 Steven Tolzmann

using System;
using System.Linq;
using Xunit;

namespace Collections.Pooled.Tests.PooledMemory
{
    public class PooledMemory_PropertiesAndIndexer_Tests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(32)]
        public void MemoryAndSpan_LengthsMatchCount(int count)
        {
            using var pm = new Collections.Pooled.PooledMemory<int>(count);

            Assert.Equal(count, pm.Count);
            Assert.Equal(count, pm.Span.Length);
            Assert.Equal(count, pm.Memory.Length);
        }

        [Fact]
        public void MemoryAndSpan_AreWritableAndReflectIndexer()
        {
            using var pm = new Collections.Pooled.PooledMemory<int>(5);

            // write via indexer, read via Span
            pm[0] = 101;
            pm[4] = 202;
            Assert.Equal(101, pm.Span[0]);
            Assert.Equal(202, pm.Span[4]);

            // write via Memory.Span, read via indexer
            var span = pm.Memory.Span;
            span[1] = 303;
            span[3] = 404;

            Assert.Equal(303, pm[1]);
            Assert.Equal(404, pm[3]);
        }

        [Fact]
        public void ClearModeProperty_ReflectsMode_Auto_Int_Never()
        {
            using var pm = new Collections.Pooled.PooledMemory<int>(1, ClearMode.Auto);
            Assert.Equal(ClearMode.Never, pm.ClearMode);
        }

        [Fact]
        public void ClearModeProperty_ReflectsMode_Auto_String_Always()
        {
            using var pm = new Collections.Pooled.PooledMemory<string>(1, ClearMode.Auto);
            Assert.Equal(ClearMode.Always, pm.ClearMode);
        }
    }
}