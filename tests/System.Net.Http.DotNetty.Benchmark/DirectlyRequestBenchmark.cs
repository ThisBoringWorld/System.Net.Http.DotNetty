using System.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace System.Net.Http.DotNetty.Benchmark
{
    [SimpleJob(RuntimeMoniker.Net461, baseline: true)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [RPlotExporter, RankColumn]
    public class DirectlyRequestBenchmark
    {
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
            var client = new HttpClient(new HttpClientHandler());

            for (int i = 0; i < RequestCount; i++)
            {
                var html = await client.GetStringAsync(Url).ConfigureAwait(false);
            }
        }

        //[Benchmark]
        public async Task HttpDotNettyClientHandler()
        {
            var client = new HttpClient(new HttpDotNettyClientHandler());

            for (int i = 0; i < RequestCount; i++)
            {
                var html = await client.GetStringAsync(Url).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task MultiHttpClientHandler()
        {
            ServicePointManager.DefaultConnectionLimit = 25;
            var client = new HttpClient(new HttpClientHandler());

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
            };

            var client = new HttpClient(new HttpDotNettyClientHandler(option));

            var tasks = Enumerable.Range(0, RequestCount).Select(async m =>
            {
                var html = await client.GetStringAsync(Url).ConfigureAwait(false);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        //[Benchmark]
        //public async Task MultiHttpDotNettyExecutor()
        //{
        //    var option = new DotNettyClientOptions()
        //    {
        //        ConnectionLimit = 25,
        //    };

        //    var executor = new DotNettyHttpRequestExecutor(option);

        //    var tasks = Enumerable.Range(0, RequestCount).Select(async m =>
        //    {
        //        var uri = new Uri(Url);
        //        var response = await executor.ExecuteAsync(new DNHttpRequest(DNVersion.Http11, DNMethod.Get, uri.PathAndQuery),
        //                                                   uri,
        //                                                   proxyUri: null,
        //                                                   CancellationToken.None).ConfigureAwait(false);
        //        var html = response.Content.ReadString(response.Content.WriterIndex, Encoding.UTF8);

        //        response.SafeRelease();
        //    });

        //    await Task.WhenAll(tasks).ConfigureAwait(false);
        //}

        //[Benchmark]
        //public async Task HostDotNettyConnection()
        //{
        //    var option = new DotNettyClientOptions()
        //    {
        //        ConnectionLimit = 25,
        //    };

        //    var uri = new Uri(Url);

        //    var connection = new HostDotNettyConnection(uri, null, option);

        //    var tasks = Enumerable.Range(0, RequestCount).Select(async m =>
        //    {
        //        var request = new DNHttpRequest(DNVersion.Http11, DNMethod.Get, uri.PathAndQuery);
        //        if (!request.Headers.Contains(HttpHeaderNames.Host))
        //        {
        //            request.Headers.Add(HttpHeaderNames.Host, $"{uri.Host}:{uri.Port}");
        //        };

        //        var response = await connection.ExecuteAsync(request,
        //                                                    CancellationToken.None).ConfigureAwait(false);
        //        var html = response.Content.ReadString(response.Content.WriterIndex, Encoding.UTF8);

        //        response.SafeRelease();
        //    });

        //    await Task.WhenAll(tasks).ConfigureAwait(false);
        //}

        #endregion Public 方法
    }
}