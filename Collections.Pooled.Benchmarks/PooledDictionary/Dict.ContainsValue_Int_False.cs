using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;

namespace Collections.Pooled.Benchmarks.PooledDictionary
{
    [SimpleJob(RuntimeMoniker.Net80)]
    public class Dict_ContainsValue_Int_False : DictContainsBase<int>
    {
        [Benchmark(Baseline = true)]
        public void DictContainsValue_Int_False()
        {
            bool result = false;
            int missingValue = N;   //The value N is not present in the dictionary.
            for (int j = 0; j < N; j++)
                result = dict.ContainsValue(missingValue);
        }

        [Benchmark]
        public void PooledContainsValue_Int_False()
        {
            bool result = false;
            int missingValue = N;   //The value N is not present in the dictionary.
            for (int j = 0; j < N; j++)
                result = pooled.ContainsValue(missingValue);
        }

        protected override int GetT(int i) => i;
    }
}
