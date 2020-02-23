using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.DotNetty.TestServer;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

namespace System.Net.Http.DotNetty.Test.HandlerTest.Directly
{
    [TestClass]
    public class DirectlyHttpsTest
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
            var requestCount = TestConstant.DefaultRequestCount;
            var tasks = Enumerable.Range(0, requestCount).Select(async m =>
            {
                var result = await _httpClient.GetStringAsync(TestServerConstant.RequestDetailUrlHttps);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(result));
            }).ToArray();

            await Task.WhenAll(tasks);
            Debug.WriteLine("Get Over");

            var text = "HelloWorld中文=1";
            var content = Encoding.UTF8.GetBytes(text);
            tasks = Enumerable.Range(0, requestCount).Select(async m =>
            {
                var result = await _httpClient.PostAsync(TestServerConstant.EchoUrlHttps, new ByteArrayContent(content));
                Assert.AreEqual(text, await result.Content.ReadAsStringAsync());
            }).ToArray();

            await Task.WhenAll(tasks);
            Debug.WriteLine("Post Over");

            tasks = Enumerable.Range(0, requestCount).Select(async m =>
            {
                var request = new HttpRequestMessage(HttpMethod.Put, TestServerConstant.RequestDetailUrlHttps)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>() { { "HelloWorld中文", "1" } }),
                };

                request.Headers.Add("TestHeader", "1");

                var result = await _httpClient.SendAsync(request);
                var jObject = JObject.Parse(await result.Content.ReadAsStringAsync());

                Assert.AreEqual(text, jObject["Form"].ToString());
                Assert.AreEqual("1", jObject["Headers"]["TestHeader"].ToString());
            }).ToArray();

            await Task.WhenAll(tasks);
            Debug.WriteLine("Send Over");
        }

        [TestInitialize]
        public void Init()
        {
            var option = new DotNettyClientOptions()
            {
                RemoteCertificateValidationCallback = (s, s1, s2, s3) => TestServerConstant.CertThumbprint.Equals(s2.ChainElements[0].Certificate.Thumbprint, StringComparison.OrdinalIgnoreCase),
            };
            var handler = new HttpDotNettyClientHandler(option);
            _httpClient = new HttpClient(handler);
        }

        #endregion Public 方法
    }
}