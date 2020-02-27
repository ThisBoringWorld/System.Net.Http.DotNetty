using System.Diagnostics;
using System.Linq;
using System.Net.Http.DotNetty.TestServer;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Http.DotNetty.Test.HandlerTest.Directly
{
    [TestClass]
    public class ProxyHttpsAuthTimeoutTest
    {
        #region Private 字段

        private HttpClient _httpClient;

        #endregion Private 字段

        #region Public 方法

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient.Dispose();
        }

        [TestMethod]
        public async Task HttpRequestTestAsync()
        {
            var requestCount = 2;

            var text = "HelloWorld中文=1";
            var content = Encoding.UTF8.GetBytes(text);

            Task[] tasks = null;

            for (int i = 0; i < 2; i++)
            {
                tasks = Enumerable.Range(0, requestCount).Select(async m =>
                {
                    using var cts = new CancellationTokenSource(1000);
                    try
                    {
                        var result = await _httpClient.PostAsync($"{TestServerConstant.EchoUrlHttps}?delay=1500", new ByteArrayContent(content), cts.Token);
                        Assert.Fail();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch
                    {
                        Assert.Fail();
                    }
                }).ToArray();

                await Task.WhenAll(tasks);
            }

            Debug.WriteLine("Timeout Over");

            requestCount = TestConstant.DefaultRequestCount;
            tasks = Enumerable.Range(0, requestCount).Select(async m =>
            {
                try
                {
                    var result = await _httpClient.PostAsync(TestServerConstant.EchoUrlHttps, new ByteArrayContent(content));
                    var html = await result.Content.ReadAsStringAsync();
                    Assert.AreEqual(text, html);
                }
                catch
                {
                    Assert.Fail();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
            Debug.WriteLine("General Over");
        }

        [TestInitialize]
        public void Init()
        {
            var option = new DotNettyClientOptions()
            {
                RemoteCertificateValidationCallback = (s, s1, s2, s3) => TestServerConstant.CertThumbprint.Equals(s2.ChainElements[0].Certificate.Thumbprint, StringComparison.OrdinalIgnoreCase),
                Proxy = new WebProxy(TestServerConstant.TestHost, TestServerConstant.AuthProxyPort)
                {
                    Credentials = new NetworkCredential(TestServerConstant.ProxyUserName, TestServerConstant.ProxyPassword)
                },
                ConnectionLimit = 1,
            };
            var handler = new HttpDotNettyClientHandler(option);
            _httpClient = new HttpClient(handler);
        }

        #endregion Public 方法
    }
}