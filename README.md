# Collections.Pooled.V2

## About this fork

The original project has not shown any signs of life for a while and is still targeting the very old .NET Core 2.1. I forked this repo and retargeted it to .NET Standard 2.1 since I will only be using it for new development in .NET Core projects. If it helps you - great! I'll do my best to try and respond to any issues/inquiries.

## Info

This library is based on classes from `System.Collections.Generic` that have been altered 
to take advantage of the new `System.Span<T>` and `System.Buffers.ArrayPool<T>` libraries 
to minimize memory allocations, improve performance, and/or allow greater interoperablity 
with modern API's.

Collections.Pooled.V2 supports .NET Standard 2.1 (.NET Core 3.0+). An extensive set of unit tests and benchmarks have
been ported from [corefx](https://github.com/dotnet/corefx).

```
Collections.Pooled.Tests
  Tests in group: 27501
   Total Duration: 3.8 sec

Outcomes
   27501 Passed
```

## Installation

[Available on NuGet!](https://www.nuget.org/packages/Collections.Pooled.V2)
```
Install-Package Collections.Pooled.V2
dotnet add package Collections.Pooled.V2
```

## `PooledList<T>`

`PooledList<T>` is based on the corefx source code for `System.Collections.Generic.List<T>`,
modified to use ArrayPool for internal array-storage allocation, and to support `Span<T>`.

There are some API changes worth noting:

  * `Find` and `FindLast` have become `TryFind` and `TryFindLast`, following the standard
    .NET "try" pattern.
  * The new `PooledList<T>.Span` property returns a `Span<T>` over the portion of the internal
    array store that is populated. This span can be further sliced, or passed into other methods
    that can read from or write to a span.
    * Enumerating the Span property can be more than twice as fast as enumerating the list itself,
      but as we are unable to do the normal "collection modified during enumeration" checks, please
      use with caution.
    * The list is unable to increment its version-checking flag when setting values via the Span.
      Therefore, "collection modified during enumeration" checks can be bypassed. Please use with caution.
  * `GetRange` now returns a `Span<T>`. For example, `foo.GetRange(5, 100)` is equivalent to `foo.Span.Slice(5, 100)`.
  * `CopyTo` now takes a `Span<T>` and doesn't offer any start-index or count parameters. Use the `Span` property and slicing instead.
  * `AddRange` and `InsertRange` can now accept a `ReadOnlySpan<T>`.
  * The new `AddSpan` and `InsertSpan` methods ensure the internal storage has the capacity
    to add the requested number of items, and return a `Span<T>` that can be used to write
    directly to that section of the internal storage. Caveats about "collection modified during enumeration"
    checks apply here as well.
  * Delegate types such as `Predicate<T>` and `Converter<T1, T2>` have been replaced with standard `Func<>` equivalents.
  * **PooledList implements IDisposable.** Disposing the list returns the internal array to the ArrayPool.
    If you forget to dispose the list, nothing will break, but memory allocations and GC pauses will be closer to those
    of `List<T>` (you will still benefit from pooling of intermediate arrays as the PooledList is resized).
  * A selection of `ToPooledList()` extension methods are provided.
  * You can optionally supply a custom implementation of `ArrayPool<T>` to the constructor, to be used instead of the
    default `ArrayPool<T>.Shared` pool.
  * The `clearMode` constructor parameter gives you control over whether data is cleared before returning
    arrays to the ArrayPool.
  * The `sizeToCapacity` constructor parameter causes the list to start out with `Count == Capacity`. 
    All entries in the list will have the default value for the type, or if `clearMode` is set to `ClearMode.Never`
	then entries in the list may have a previously-used value from the array pool. This feature is primarily useful
	when working with value types and avoiding unnecessary allocations.

#### Performance

Adding items to a list is one area where ArrayPool helps us quite a bit:
![List Add Benchmarks](./docs/benchmarks/netcoreapp2.2/List_Add.svg) 

## `PooledDictionary<TKey, TValue>`

`PooledDictionary<TKey, TValue>` is based on the corefx source code for `System.Collections.Generic.Dictionary<TKey, TValue>`,
modified to use ArrayPool for internal storage allocation, and to support `Span<T>`.

There are some API changes worth noting:

  * New methods include: `AddRange`, `GetOrAdd`, `AddOrUpdate`
  * Both constructors and AddRange can take a sequence of `KeyValuePair<TKey, TValue>` objects, or a sequence of 
    `ValueTuple<TKey, TValue>` objects.
  * Significantly reduced memory allocations when adding many items.
  * **PooledDictionary implements IDisposable.** Disposing the dictionary returns the internal arrays to the ArrayPool.
    If you forget to dispose the dictionary, nothing will break, but memory allocations and GC pauses will be closer to those
    of `Dictionary<TKey, TValue>` (you will still benefit from pooling of intermediate arrays as the PooledDictionary is resized).
  * A selection of `ToPooledDictionary()` extension methods are provided.
  * The `ClearMode` constructor parameter gives you control over whether data is cleared before returning
    arrays to the ArrayPool.

#### Performance

Adding to dictionaries is where using ArrayPool really has an impact:
![Dictionary Add Benchmarks](./docs/benchmarks/netcoreapp2.2/Dict_Add.svg) 

## `PooledSet<T>`

`PooledSet<T>` is based on the corefx source code for `System.Generic.Collections.HashSet<T>`,
modified to use ArrayPool for internal storage allocation, and to support `ReadOnlySpan<T>` for all set functions.

  * Constructors, and all set methods have overloads that accept `ReadOnlySpan<T>`.
  * There are also overloads that accept arrays but forward to the span implementation. They are present 
    to avoid ambiguous method calls.
  * PooledSet slightly outstrips HashSet in performance in almost every case, while allocating less memory.
    In cases where the internal arrays must be resized, PooledSet enjoys a substantial advantage.
  * **PooledSet implements IDisposable.** Disposing the set returns the internal arrays to the ArrayPool.
    If you forget to dispose the set, nothing will break, but memory allocations and GC pauses will be closer to those
    of `HashSet<T>` (you will still benefit from pooling of intermediate arrays as the PooledSet is resized).
  * A selection of `ToPooledSet()` extension methods are provided.
  * The `ClearMode` constructor parameter gives you control over whether data is cleared before returning
    arrays to the ArrayPool.

#### Performance

Here's what pooling does for us when adding to a PooledSet. 

![Set Add Benchmarks](./docs/benchmarks/netcoreapp2.2/Set_Add.svg) 

## `PooledStack<T>`

`PooledStack<T>` is based on the corefx source code for `System.Generic.Collections.Stack<T>`, 
modified to use ArrayPool for internal storage allocation.

  * Other than the ability to pass Spans into the constructor, the only other API change from
    `Stack<T>` is the addition of the RemoveWhere method: because sometimes you just need to remove
    something from a stack.
  * Significantly reduced memory allocations when pushing many items.
  * **PooledStack implements IDisposable.** Disposing the stack returns the internal array to the ArrayPool.
    If you forget to dispose the stack, nothing will break, but memory allocations and GC pauses will be closer to those
    of `Stack<T>` (you will still benefit from pooling of intermediate arrays as the PooledStack is resized).
  * A selection of `ToPooledStack()` extension methods are provided.
  * You can optionally supply a custom implementation of `ArrayPool<T>` to the constructor, to be used instead of the
    default `ArrayPool<T>.Shared` pool.
  * The `ClearMode` constructor parameter gives you control over whether data is cleared before returning
    arrays to the ArrayPool.

#### Performance

Once again, pushing to a stack shows off some of the advantages of using ArrayPool:
![Stack Push Benchmarks](./docs/benchmarks/netcoreapp2.2/Stack_Push.svg) 

## `PooledQueue<T>`

`PooledQueue<T>` is based on the corefx source code for `System.Generic.Collections.Queue<T>`, 
modified to use ArrayPool for internal storage allocation.

  * Other than the ability to pass Spans into the constructor, the only other API change from
    `Queue<T>` is the addition of the RemoveWhere method: because sometimes you just need to remove
    something from a queue.
  * Significantly reduced memory allocations when enqueueing many items.
  * **PooledQueue implements IDisposable.** Disposing the queue returns the internal array to the ArrayPool.
    If you forget to dispose the queue, nothing will break, but memory allocations and GC pauses will be closer to those
    of `Queue<T>` (you will still benefit from pooling of intermediate arrays as the PooledQueue is resized).
  * A selection of `ToPooledQueue()` extension methods are provided.
  * You can optionally supply a custom implementation of `ArrayPool<T>` to the constructor, to be used instead of the
    default `ArrayPool<T>.Shared` pool.
  * The `ClearMode` constructor parameter gives you control over whether data is cleared before returning
    arrays to the ArrayPool.

#### Performance

![Queue Enqueue Benchmarks](./docs/benchmarks/netcoreapp2.2/Queue_Enqueue.svg)

## `PooledMemory<T>`

`PooledMemory<T>` is an `IMemoryOwner<T>` implementation that uses `ArrayPool<T>` for internal storage.  
Unlike directly renting from `ArrayPool<T>`, the exposed memory region is always constrained to **exactly the requested length** rather than the entire rented buffer.

  * Provides `Memory<T>` and `Span<T>` views into the pooled buffer, with the logical length fixed to the requested count.
  * Exposes an indexer (`this[int index]`) and implements `IEnumerable<T>` for simple element access.
  * Multiple constructors are available:
    - From a `count` (pre-allocated size).
    - From a `ReadOnlySpan<T>` or `IEnumerable<T>` (copying contents).
    - With optional `ClearMode` and/or a custom `ArrayPool<T>`.
  * **PooledMemory implements IDisposable.** Disposing the instance returns the rented array to the pool.  
    If not disposed, the array will eventually be collected by the GC, but you lose the pooling benefit.
  * `ClearMode` allows you to control whether arrays are cleared before returning to the pool:
    - `Always`: always clear.
    - `Never`: never clear.
    - `Auto`: clear only when `T` is a reference type or contains references.
  * You can supply your own `ArrayPool<T>` implementation if you don’t want to use the shared global pool.

