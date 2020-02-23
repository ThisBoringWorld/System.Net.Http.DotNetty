using System.Linq;
using System.Threading.Tasks;

using BenchmarkDotNet.Running;

using Cuture.Http;

namespace System.Net.Http.DotNetty.Benchmark
{
    public class Program
    {
        #region Public 方法

        public static async Task Main(string[] args)
        {
            //await TestUtil.TimedRunAsync(async () =>
            //{
            //    await new ProxyAuthRequestBenchmark().MultiHttpDotNettyClientHandler();
            //}, "", 10);
            //await RunTestAsync().ConfigureAwait(false);
            //return;
            var summary = BenchmarkRunner.Run<DirectlyRequestBenchmark>(new AllowNonOptimized());
            //var summary = BenchmarkRunner.Run<ProxyRequestBenchmark>(new AllowNonOptimized());
            //var summary = BenchmarkRunner.Run<ProxyAuthRequestBenchmark>(new AllowNonOptimized());
            await Task.CompletedTask;   //方便测试
            //Console.WriteLine("Over");
        }

        #endregion Public 方法

        #region Private 方法

        private static async Task RunTestAsync()
        {
            HttpDefaultSetting.DefaultConnectionLimit = 50;

            var client = new HttpClient(new HttpDotNettyClientHandler());

            var uri = new Uri("http://127.0.0.1:5000/api/requestdetail");

            int requestCount = 10_000;

            await TestUtil.TimedRunAsync(async () =>
            {
                var tasks = Enumerable.Range(0, requestCount).Select(async _ =>
                {
                    var request = uri.ToHttpRequest()
                                     .AsRequest();
                    var responseMessage = await client.SendAsync(request).ConfigureAwait(false);
                    var content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        throw new NullReferenceException();
                    }
                });

                await Task.WhenAll(tasks);
            }, $"DotNetty Http Client Request:{requestCount}");

            await TestUtil.TimedRunAsync(async () =>
            {
                var tasks = Enumerable.Range(0, requestCount).Select(async _ =>
                {
                    var request = uri.ToHttpRequest();
                    var content = await request.GetAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        throw new NullReferenceException();
                    }
                });

                await Task.WhenAll(tasks);
            }, $"Cuture.Http Request:{requestCount}");

            var fxClient = new HttpClient();
            await TestUtil.TimedRunAsync(async () =>
            {
                var tasks = Enumerable.Range(0, requestCount).Select(async _ =>
                {
                    var request = uri.ToHttpRequest()
                                     .AsRequest();
                    var responseMessage = await fxClient.SendAsync(request).ConfigureAwait(false);
                    var content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        throw new NullReferenceException();
                    }
                });

                await Task.WhenAll(tasks);
            }, $"HttpClient Request:{requestCount}");
        }

        #endregion Private 方法
    }
}