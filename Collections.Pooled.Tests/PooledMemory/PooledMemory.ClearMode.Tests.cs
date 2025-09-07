//
// Authors:
//   Steven Tolzmann
//
// Copyright (C) 2025 Steven Tolzmann

using System;
using System.Buffers;
using Xunit;

namespace Collections.Pooled.Tests.PooledMemory
{
    public class PooledMemory_ClearMode_Tests
    {
        private sealed class TestPool<T> : ArrayPool<T>
        {
            private readonly T[] _buffer;
            public bool Returned { get; private set; }
            public bool LastClearFlag { get; private set; }
            public T[] LastReturnedArray { get; private set; }

            public TestPool(int size) => _buffer = new T[size];

            public override T[] Rent(int minimumLength)
            {
                if (minimumLength > _buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(minimumLength));
                return _buffer;
            }

            public override void Return(T[] array, bool clearArray = false)
            {
                Returned = true;
                LastClearFlag = clearArray;
                LastReturnedArray = array;
                if (clearArray)
                {
                    Array.Clear(array, 0, array.Length);
                }
            }
        }

        private struct WithRef
        {
            public string Ref;
            public int Val;
        }

        [Fact]
        public void ClearModeProperty_ReflectsMode()
        {
            using (var pm1 = new Collections.Pooled.PooledMemory<int>(4, ClearMode.Always))
                Assert.Equal(ClearMode.Always, pm1.ClearMode);

            using (var pm2 = new Collections.Pooled.PooledMemory<int>(4, ClearMode.Never))
                Assert.Equal(ClearMode.Never, pm2.ClearMode);

            // Auto on int (no refs) should behave like Never
            using (var pm3 = new Collections.Pooled.PooledMemory<int>(4, ClearMode.Auto))
                Assert.Equal(ClearMode.Never, pm3.ClearMode);
        }

        [Fact]
        public void Dispose_Never_ValueType_DoesNotClear()
        {
            var pool = new TestPool<int>(8);
            using (var pm = new Collections.Pooled.PooledMemory<int>(8, ClearMode.Never, pool))
            {
                pm[0] = 1;
                pm[7] = 2;
            }

            Assert.True(pool.Returned);
            Assert.False(pool.LastClearFlag);
            Assert.NotNull(pool.LastReturnedArray);
        }

        [Fact]
        public void Dispose_Always_ValueType_Clears()
        {
            var pool = new TestPool<int>(8);
            using (var pm = new Collections.Pooled.PooledMemory<int>(8, ClearMode.Always, pool))
            {
                pm[0] = 123;
                pm[7] = 456;
            }

            Assert.True(pool.LastClearFlag);
            for (int i = 0; i < pool.LastReturnedArray.Length; i++)
                Assert.Equal(default, pool.LastReturnedArray[i]);
        }

        [Fact]
        public void Dispose_Auto_ValueType_NoRefs_DoesNotClear()
        {
            var pool = new TestPool<int>(4);
            using (var pm = new Collections.Pooled.PooledMemory<int>(4, ClearMode.Auto, pool))
            {
                pm[0] = 42;
            }

            Assert.True(pool.Returned);
            Assert.False(pool.LastClearFlag);
        }

        [Fact]
        public void Dispose_Auto_ReferenceType_Clears()
        {
            var pool = new TestPool<string>(4);
            using (var pm = new Collections.Pooled.PooledMemory<string>(4, ClearMode.Auto, pool))
            {
                pm[0] = "a";
                pm[1] = "b";
            }

            Assert.True(pool.LastClearFlag);
            for (int i = 0; i < pool.LastReturnedArray.Length; i++)
                Assert.Null(pool.LastReturnedArray[i]);
        }

        [Fact]
        public void Dispose_Auto_ValueTypeContainingRef_Clears()
        {
            var pool = new TestPool<WithRef>(3);
            using (var pm = new Collections.Pooled.PooledMemory<WithRef>(3, ClearMode.Auto, pool))
            {
                pm[0] = new WithRef { Ref = "x", Val = 1 };
            }

            Assert.True(pool.LastClearFlag);
            for (int i = 0; i < pool.LastReturnedArray.Length; i++)
                Assert.Equal(default, pool.LastReturnedArray[i]);
        }

        [Fact]
        public void Dispose_Never_ReferenceType_DoesNotClearBuffer()
        {
            var pool = new TestPool<string>(3);
            using (var pm = new Collections.Pooled.PooledMemory<string>(3, ClearMode.Never, pool))
            {
                pm[0] = "keep";
                pm[2] = "me";
            }

            Assert.True(pool.Returned);
            Assert.False(pool.LastClearFlag);
            Assert.Equal("keep", pool.LastReturnedArray[0]);
            Assert.Equal("me", pool.LastReturnedArray[2]);
        }
    }
}