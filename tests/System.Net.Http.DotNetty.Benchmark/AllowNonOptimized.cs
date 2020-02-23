using System.Linq;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;

namespace System.Net.Http.DotNetty.Benchmark
{
    public class AllowNonOptimized : ManualConfig
    {
        #region Public 构造函数

        public AllowNonOptimized()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLS

            Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }

        #endregion Public 构造函数
    }
}