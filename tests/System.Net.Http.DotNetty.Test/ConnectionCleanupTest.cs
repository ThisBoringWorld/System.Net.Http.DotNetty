using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.DotNetty.TestServer;
using System.Threading;
using System.Threading.Tasks;

using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using DNHttpMethod = DotNetty.Codecs.Http.HttpMethod;
using DNHttpVersion = DotNetty.Codecs.Http.HttpVersion;

namespace System.Net.Http.DotNetty.Test
{
    [TestClass]
    public class ConnectionCleanupTest
    {
        #region Private 字段

        private const int ConnectionLimit = 5;
        private const double TimecouSeconds = 2.5;
        private DotNettyHttpRequestExecutor _executor;

        #endregion Private 字段

        #region Public 方法

        [TestMethod]
        public async Task AutoConnectionCleanupTestAsync()
        {
            List<Uri> uris = GetUris();

            Debug.WriteLine("Fill Pool");
            await DoRequestAsync(uris, 150);

            await CleanupAndCheckPoolSizeAsync(uris.Count, ConnectionLimit);

            Debug.WriteLine("Fill Pool Over");
            await Task.Delay(TimeSpan.FromSeconds(1));

            Debug.WriteLine("Re use connection 2");
            await DoRequestAsync(uris, 2);

            Debug.WriteLine("Re use connection 2 Over");
            await Task.Delay(TimeSpan.FromSeconds(TimecouSeconds - 1 + 0.5));

            await CleanupAndCheckPoolSizeAsync(uris.Count, 2);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Debug.WriteLine("Re use connection 1");
            await DoRequestAsync(uris, 1);

            Debug.WriteLine("Re use connection 1 Over");
            await Task.Delay(TimeSpan.FromSeconds(TimecouSeconds - 1 + 0.5));

            await CleanupAndCheckPoolSizeAsync(uris.Count, 1);

            Debug.WriteLine("wait Cleanup all");
            await Task.Delay(TimeSpan.FromSeconds(TimecouSeconds + 0.5));

            await CleanupExecutor();

            if (_executor.HostConnections.Count == 0)
            {
                Console.WriteLine("All Pool Removed");
            }
            else
            {
                foreach (var connectionPool in _executor.HostConnections.Values)
                {
                    Assert.AreEqual(0, connectionPool.Count);
                }
            }

            Debug.WriteLine("Re Fill Pool");

            await DoRequestAsync(uris, 150);

            Debug.WriteLine("Re Fill Pool Over");
            await Task.Delay(TimeSpan.FromSeconds(TimecouSeconds / 2));

            await CleanupAndCheckPoolSizeAsync(uris.Count, ConnectionLimit);
        }

        [TestInitialize]
        public virtual void Init()
        {
            var option = new DotNettyClientOptions()
            {
                ConnectionLimit = ConnectionLimit,
                Proxy = new WebProxy(TestServerConstant.TestHost, TestServerConstant.ProxyPort),
                ResourcesTimeout = TimeSpan.FromSeconds(TimecouSeconds),
                ResourcesCheckInterval = TimeSpan.FromDays(1),
            };
            _executor = new DotNettyHttpRequestExecutor(option);
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            _executor.Dispose();
        }

        #endregion Public 方法

        #region Protected 方法

        protected async Task CleanupAndCheckPoolSizeAsync(int poolSize, int connectionCount)
        {
            Debug.WriteLine($"CleanupAndCheckPoolSize ConnectionLimit:{ConnectionLimit} poolSize:{poolSize} MaxConnectionCount:{ConnectionLimit * poolSize} connectionCount:{connectionCount}");
            await CleanupExecutor();

            Assert.AreEqual(poolSize, _executor.HostConnections.Count);
            foreach (var connectionPool in _executor.HostConnections.Values)
            {
                Assert.AreEqual(connectionCount, connectionPool.Count);
            }
        }

        protected async Task DoRequestAsync(List<Uri> uris, int requestCount)
        {
            foreach (var uri in uris)
            {
                var sw = Stopwatch.StartNew();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var tasks = Enumerable.Range(0, requestCount).Select(async m =>
                {
                    IFullHttpResponse response = null;
                    try
                    {
                        var request = new DefaultFullHttpRequest(DNHttpVersion.Http11, DNHttpMethod.Get, uri.OriginalString);
                        response = await _executor.ExecuteAsync(request, cts.Token).ConfigureAwait(false);

                        Assert.AreEqual(200, response.Status.Code);
                    }
                    catch { }
                    finally
                    {
                        response.SafeRelease();
                    }
                }).ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);
                sw.Stop();

                Debug.WriteLine($"{uri.OriginalString}:{sw.Elapsed}");
            }
        }

        private static List<Uri> GetUris()
        {
            var uris = new List<Uri>();

            for (int i = 0; i < 5; i++)
            {
                uris.Add(new Uri($"http://www.test.baidu{i}.com"));
            }

            return uris;
        }

        private async Task CleanupExecutor()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await _executor.Cleanup(cts.Token);
        }

        #endregion Protected 方法
    }
}