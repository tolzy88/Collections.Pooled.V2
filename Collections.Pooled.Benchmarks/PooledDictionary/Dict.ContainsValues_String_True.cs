using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Collections.Pooled.Benchmarks.PooledDictionary
{
    [SimpleJob(RuntimeMoniker.Net80)]
    public class Dict_ContainsValue_String_True : DictContainsBase<string>
    {
        [Benchmark(Baseline = true)]
        public void DictContainsValue_String_True()
        {
            bool result = false;
            for (int j = 0; j < N; j++)
                result = dict.ContainsValue(sampleKeys[j]);
        }

        [Benchmark]
        public void PooledContainsValue_String_True()
        {
            bool result = false;
            for (int j = 0; j < N; j++)
                result = pooled.ContainsValue(sampleKeys[j]);
        }

        protected override string GetT(int i) => i.ToString();

        private string[] sampleKeys;

        public override void GlobalSetup()
        {
            base.GlobalSetup();
            sampleKeys = dict.Keys.ToArray();
        }
    }
}
