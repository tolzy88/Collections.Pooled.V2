using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;

namespace Collections.Pooled.Benchmarks.PooledSet
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class Set_Clear : SetBase
    {
        [Benchmark(Baseline = true)]
        public void HashSet_Clear()
        {
            hashSet.Clear();
        }

        [Benchmark]
        public void PooledSet_Clear()
        {
            pooledSet.Clear();
        }

        private int[] startingElements;
        private HashSet<int> hashSet;
        private PooledSet<int> pooledSet;

        [Params(SetSize_Small)]
        public int InitialSetSize;

        [IterationSetup(Target = nameof(HashSet_Clear))]
        public void HashIterationSetup()
        {
            hashSet.UnionWith(startingElements);
        }

        [IterationSetup(Target = nameof(PooledSet_Clear))]
        public void PooledIterationSetup()
        {
            pooledSet.UnionWith(startingElements);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var intGenerator = new RandomTGenerator<int>(InstanceCreators.IntGenerator);
            startingElements = intGenerator.MakeNewTs(InitialSetSize);

            hashSet = new HashSet<int>();
            pooledSet = new PooledSet<int>();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            pooledSet?.Dispose();
        }
    }
}
