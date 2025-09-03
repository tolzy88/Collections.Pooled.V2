﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;

namespace Collections.Pooled.Benchmarks.PooledDictionary
{
    [SimpleJob(RuntimeMoniker.Net80)]
    public class Dict_ContainsValue_Int_True : DictContainsBase<int>
    {
        [Benchmark(Baseline = true)]
        public void DictContainsValue_Int_True()
        {
            bool result = false;
            for (int j = 0; j < N; j++)
                result = dict.ContainsValue(j);
        }

        [Benchmark]
        public void PooledContainsValue_Int_True()
        {
            bool result = false;
            for (int j = 0; j < N; j++)
                result = pooled.ContainsValue(j);
        }

        protected override int GetT(int i) => i;
    }
}
