using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;

namespace Collections.Pooled.Benchmarks.PooledDictionary
{
    [SimpleJob(RuntimeMoniker.Net80)]
    public class Dict_ContainsValue_String_False : DictContainsBase<string>
    {
        [Benchmark(Baseline = true)]
        public void DictContainsValue_String_False()
        {
            bool result = false;
            string missingValue = N.ToString();   //The value N is not present in the dictionary.
            for (int j = 0; j < N; j++)
                result = dict.ContainsValue(missingValue);
        }

        [Benchmark]
        public void PooledContainsValue_String_False()
        {
            bool result = false;
            string missingValue = N.ToString();   //The value N is not present in the dictionary.
            for (int j = 0; j < N; j++)
                result = pooled.ContainsValue(missingValue);
        }

        protected override string GetT(int i) => i.ToString();
    }
}
