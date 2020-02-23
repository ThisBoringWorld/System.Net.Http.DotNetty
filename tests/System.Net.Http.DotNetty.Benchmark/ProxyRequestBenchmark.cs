using System.Linq;
using System.Net.Http.DotNetty.TestServer;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace System.Net.Http.DotNetty.Benchmark
{
    [SimpleJob(RuntimeMoniker.Net461, baseline: true)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [RPlotExporter, RankColumn]
    public class ProxyRequestBenchmark
    {
        #region Private 字段

        private readonly IWebProxy _proxy = new WebProxy(TestServerConstant.TestHost, TestServerConstant.ProxyPort);

        #endregion Private 字段

        #region Public 字段

        [Params(10_000)]
        public int RequestCount = 10_000;

        [Params("http://127.0.0.1:5000/api/requestdetail")]
        public string Url = "http://127.0.0.1:5000/api/requestdetail";

        #endregion Public 字段

        #region Public 方法

        //[Benchmark]
        public async Task HttpClientHandler()
        {
            var client = new HttpClient(new HttpClientHandler()
            {
                UseProxy = true,
                Proxy = _proxy,
            });

            for (int i = 0; i < RequestCount; i++)
            {
                var html = await client.GetStringAsync(Url).ConfigureAwait(false);
            }
        }

        //[Benchmark]
        public async Task HttpDotNettyClientHandler()
        {
            var client = new HttpClient(new HttpDotNettyClientHandler(new DotNettyClientOptions()
            {
                Proxy = _proxy
            }));

            for (int i = 0; i < RequestCount; i++)
            {
                var html = await client.GetStringAsync(Url).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task MultiHttpClientHandler()
        {
            ServicePointManager.DefaultConnectionLimit = 25;
            var client = new HttpClient(new HttpClientHandler()
            {
                UseProxy = true,
                Proxy = _proxy,
            });

            var tasks = Enumerable.Range(0, RequestCount).Select(async m =>
            {
                var html = await client.GetStringAsync(Url).ConfigureAwait(false);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task MultiHttpDotNettyClientHandler()
        {
            var option = new DotNettyClientOptions()
            {
                ConnectionLimit = 25,
                Proxy = _proxy,
            };

            var client = new HttpClient(new HttpDotNettyClientHandler(option));

            var tasks = Enumerable.Range(0, RequestCount).Select(async m =>
            {
                var html = await client.GetStringAsync(Url).ConfigureAwait(false);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        #endregion Public 方法
    }
}