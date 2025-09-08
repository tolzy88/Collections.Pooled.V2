//
// Authors:
//   Steven Tolzmann
//
// Copyright (C) 2025 Steven Tolzmann

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Collections.Pooled
{
    /// <summary>
    /// Pooled <see cref="IMemoryOwner{T}"/> implementation that uses <see cref="ArrayPool{T}"/> as the backing store.
    /// The exposed Memory Region is constrained to exactly the requested length. This differs from default <see cref="ArrayPool{T}"/> or <see cref="MemoryManager{T}"/> behavior that may return an array with a length greater than requested.
    /// Also exposes <see cref="IEnumerable{T}"/> and indexer for element access.
    /// See <see href="https://learn.microsoft.com/dotnet/api/system.buffers.imemoryowner-1"/> for more information on <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    public class PooledMemory<T> : IMemoryOwner<T>, IEnumerable<T>, IDisposable
    {
        private readonly ArrayPool<T> _pool;
        private readonly bool _clearOnFree;
        private T[] _array;

        /// <summary>
        /// Gets the number of elements in this block of memory.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the element at the specified index in this block of memory.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in this block of memory.</returns>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)Count)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                return _array[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)Count)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                _array[index] = value;
            }
        }

        /// <summary>
        /// <see cref="Memory{T}"/> representing this block of memory.
        /// </summary>
        public Memory<T> Memory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.AsMemory(0, Count);
        }

        /// <summary>
        /// <see cref="Span{T}"/> representing this block of memory.
        /// </summary>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.AsSpan(0, Count);
        }

        /// <summary>
        /// Returns the ClearMode behavior for this memory owner.
        /// </summary>
        public ClearMode ClearMode => _clearOnFree ? ClearMode.Always : ClearMode.Never;

        /// <summary>
        /// Initializes a new default instance of the <see cref="PooledMemory{T}"/> class with zero elements.
        /// </summary>
        public PooledMemory() : this(count: 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by renting a buffer of the specified length
        /// from an <see cref="ArrayPool{T}"/> and constraining the logical length to the requested <paramref name="count"/>.
        /// </summary>
        /// <param name="count">The number of elements in this memory block. Must be non-negative.</param>
        /// <remarks>
        /// The underlying rented array may be larger than <paramref name="count"/>; the logical length is constrained to <paramref name="count"/>.
        /// Uses <see cref="ArrayPool{T}.Shared"/> and ClearMode.Auto by default.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
        public PooledMemory(int count)
            : this(count, ClearMode.Auto, ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by renting a buffer of the specified length
        /// and configuring the clearing behavior when returning to the pool.
        /// </summary>
        /// <param name="count">The number of elements in this memory block. Must be non-negative.</param>
        /// <param name="clearMode">
        /// Specifies whether the underlying array should be cleared when returned to the pool:
        /// <see cref="ClearMode.Always"/>, <see cref="ClearMode.Never"/>, or <see cref="ClearMode.Auto"/> (clears for reference-containing types).
        /// </param>
        /// <remarks>Uses <see cref="ArrayPool{T}.Shared"/> as the backing pool.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
        public PooledMemory(int count, ClearMode clearMode)
            : this(count, clearMode, ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by renting a buffer of the specified length
        /// from a custom <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <param name="count">The number of elements in this memory block. Must be non-negative.</param>
        /// <param name="customPool">
        /// The pool to rent from. If null, <see cref="ArrayPool{T}.Shared"/> is used.
        /// </param>
        /// <remarks>Uses <see cref="ClearMode.Auto"/> for clearing behavior.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
        public PooledMemory(int count, ArrayPool<T> customPool)
            : this(count, ClearMode.Auto, customPool)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by renting a buffer of the specified length
        /// from the provided <see cref="ArrayPool{T}"/> and configuring the clearing behavior.
        /// </summary>
        /// <param name="count">The number of elements in this memory block. Must be non-negative.</param>
        /// <param name="clearMode">
        /// Specifies whether the underlying array should be cleared when returned to the pool:
        /// <see cref="ClearMode.Always"/>, <see cref="ClearMode.Never"/>, or <see cref="ClearMode.Auto"/> (clears for reference-containing types).
        /// </param>
        /// <param name="customPool">
        /// The pool to rent from. If null, <see cref="ArrayPool{T}.Shared"/> is used.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
        public PooledMemory(int count, ClearMode clearMode, ArrayPool<T> customPool)
        {
            if (count < 0)
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            _pool = customPool ?? ArrayPool<T>.Shared;
            _clearOnFree = ShouldClear(clearMode);
            _array = _pool.Rent(count);
            Count = count;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="ReadOnlySpan{T}"/> into a buffer rented from <see cref="ArrayPool{T}.Shared"/>.
        /// </summary>
        /// <param name="span">The source elements to copy into the new memory block.</param>
        /// <remarks>Uses <see cref="ClearMode.Auto"/> for clearing behavior.</remarks>
        public PooledMemory(ReadOnlySpan<T> span)
            : this(span, ClearMode.Auto, ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="ReadOnlySpan{T}"/> into a buffer, configuring the clearing behavior.
        /// </summary>
        /// <param name="span">The source elements to copy into the new memory block.</param>
        /// <param name="clearMode">
        /// Specifies whether the underlying array should be cleared when returned to the pool:
        /// <see cref="ClearMode.Always"/>, <see cref="ClearMode.Never"/>, or <see cref="ClearMode.Auto"/> (clears for reference-containing types).
        /// </param>
        /// <remarks>Uses <see cref="ArrayPool{T}.Shared"/> as the backing pool.</remarks>
        public PooledMemory(ReadOnlySpan<T> span, ClearMode clearMode)
            : this(span, clearMode, ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="ReadOnlySpan{T}"/> into a buffer rented from a custom <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <param name="span">The source elements to copy into the new memory block.</param>
        /// <param name="customPool">
        /// The pool to rent from. If null, <see cref="ArrayPool{T}.Shared"/> is used.
        /// </param>
        /// <remarks>Uses <see cref="ClearMode.Auto"/> for clearing behavior.</remarks>
        public PooledMemory(ReadOnlySpan<T> span, ArrayPool<T> customPool)
            : this(span, ClearMode.Auto, customPool)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="ReadOnlySpan{T}"/> into a buffer rented from the provided <see cref="ArrayPool{T}"/> and configuring the clearing behavior.
        /// </summary>
        /// <param name="span">The source elements to copy into the new memory block.</param>
        /// <param name="clearMode">
        /// Specifies whether the underlying array should be cleared when returned to the pool:
        /// <see cref="ClearMode.Always"/>, <see cref="ClearMode.Never"/>, or <see cref="ClearMode.Auto"/> (clears for reference-containing types).
        /// </param>
        /// <param name="customPool">
        /// The pool to rent from. If null, <see cref="ArrayPool{T}.Shared"/> is used.
        /// </param>
        public PooledMemory(ReadOnlySpan<T> span, ClearMode clearMode, ArrayPool<T> customPool)
        {
            int count = span.Length; // Can't be negative
            _pool = customPool ?? ArrayPool<T>.Shared;
            _clearOnFree = ShouldClear(clearMode);
            _array = _pool.Rent(count);
            Count = count;
            try
            {
                span.CopyTo(_array);
            }
            catch
            {
                // If CopyTo throws, return the array to the pool before propagating the exception.
                _pool.Return(_array, _clearOnFree);
                throw;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="IEnumerable{T}"/> into a buffer rented from <see cref="ArrayPool{T}.Shared"/>.
        /// </summary>
        /// <param name="enumerable">The source sequence whose elements will be copied into the new memory block.</param>
        /// <remarks>Uses <see cref="ClearMode.Auto"/> for clearing behavior.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="enumerable"/> is null.</exception>
        public PooledMemory(IEnumerable<T> enumerable)
            : this(enumerable, ClearMode.Auto, ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="IEnumerable{T}"/> into a buffer and configuring the clearing behavior.
        /// </summary>
        /// <param name="enumerable">The source sequence whose elements will be copied into the new memory block.</param>
        /// <param name="clearMode">
        /// Specifies whether the underlying array should be cleared when returned to the pool:
        /// <see cref="ClearMode.Always"/>, <see cref="ClearMode.Never"/>, or <see cref="ClearMode.Auto"/> (clears for reference-containing types).
        /// </param>
        /// <remarks>Uses <see cref="ArrayPool{T}.Shared"/> as the backing pool.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="enumerable"/> is null.</exception>
        public PooledMemory(IEnumerable<T> enumerable, ClearMode clearMode)
            : this(enumerable, clearMode, ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="IEnumerable{T}"/> into a buffer rented from a custom <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <param name="enumerable">The source sequence whose elements will be copied into the new memory block.</param>
        /// <param name="customPool">
        /// The pool to rent from. If null, <see cref="ArrayPool{T}.Shared"/> is used.
        /// </param>
        /// <remarks>Uses <see cref="ClearMode.Auto"/> for clearing behavior.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="enumerable"/> is null.</exception>
        public PooledMemory(IEnumerable<T> enumerable, ArrayPool<T> customPool)
            : this(enumerable, ClearMode.Auto, customPool)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledMemory{T}"/> class by copying the contents of the specified
        /// <see cref="IEnumerable{T}"/> into a buffer rented from the provided <see cref="ArrayPool{T}"/> and configuring the clearing behavior.
        /// </summary>
        /// <param name="enumerable">The source sequence whose elements will be copied into the new memory block.</param>
        /// <param name="clearMode">
        /// Specifies whether the underlying array should be cleared when returned to the pool:
        /// <see cref="ClearMode.Always"/>, <see cref="ClearMode.Never"/>, or <see cref="ClearMode.Auto"/> (clears for reference-containing types).
        /// </param>
        /// <param name="customPool">
        /// The pool to rent from. If null, <see cref="ArrayPool{T}.Shared"/> is used.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="enumerable"/> is null.</exception>
        public PooledMemory(IEnumerable<T> enumerable, ClearMode clearMode, ArrayPool<T> customPool)
        {
            _pool = customPool ?? ArrayPool<T>.Shared;
            _clearOnFree = ShouldClear(clearMode);

            switch (enumerable)
            {
                case null:
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.enumerable);
                    break;

                case ICollection<T> c:
                {
                    int count = c.Count;
                    _array = _pool.Rent(count);
                    Count = count;
                    try
                    {
                        c.CopyTo(_array, 0);
                    }
                    catch
                    {
                        // If CopyTo throws, return the array to the pool before propagating the exception.
                        _pool.Return(_array, _clearOnFree);
                        throw;
                    }
                    break;
                }

                case IReadOnlyCollection<T> rc:
                {
                    int count = rc.Count;
                    _array = _pool.Rent(count);
                    Count = count;
                    int i = 0;
                    foreach (var item in rc)
                        _array[i++] = item;
                    break;
                }

                case System.Collections.ICollection nc:
                {
                    int count = nc.Count;
                    _array = _pool.Rent(count);
                    Count = count;
                    try
                    {
                        nc.CopyTo(_array, 0);
                    }
                    catch
                    {
                        // If CopyTo throws, return the array to the pool before propagating the exception.
                        _pool.Return(_array, _clearOnFree);
                        throw;
                    }
                    break;
                }

                default:
                {
                    int capacity = 4; // Start small and grow as needed
                    _array = _pool.Rent(capacity);
                    // Rent may return a bigger array, that is OK
                    capacity = _array.Length;

                    int i = 0;
                    foreach (var item in enumerable)
                    {
                        if (i == capacity)
                        {
                            // Need more space -> double capacity
                            int newCapacity = capacity * 2;
                            T[] newArray = _pool.Rent(newCapacity);
                            // Rent may return a bigger array, that is OK
                            newCapacity = newArray.Length;

                            // Copy over existing
                            Array.Copy(_array, 0, newArray, 0, capacity);

                            // Return old array
                            _pool.Return(_array, _clearOnFree);

                            _array = newArray;
                            capacity = newCapacity;
                        }

                        _array[i++] = item;
                    }

                    Count = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator(); // Span enumerator is a ref struct (fast!)

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return _array[i];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return _array[i];
        }

        /// <summary>
        /// Disposes the <see cref="PooledMemory{T}"/> instance, returning the underlying array to the pool.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref _array, null) is T[] array)
                {
                    try
                    {
                        _pool.Return(
                            array: array,
                            clearArray: _clearOnFree);
                    }
                    catch (ArgumentException)
                    {
                        // If the pool rejected the array, ignore and drop it.
                    }
                }
            }
            // no unmanaged resources to free
        }

        private static readonly bool s_isReferenceOrContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private static bool ShouldClear(ClearMode mode)
        {
            return mode == ClearMode.Always
                || (mode == ClearMode.Auto && s_isReferenceOrContainsReferences);
        }
    }
}
