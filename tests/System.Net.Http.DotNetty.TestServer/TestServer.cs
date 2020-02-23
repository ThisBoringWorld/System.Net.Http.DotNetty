using System.IO;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace System.Net.Http.DotNetty.TestServer
{
    public class TestServer
    {
        #region Public 方法

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                              .UseKestrel(options =>
                              {
                                  options.Listen(IPAddress.Parse(TestServerConstant.TestHost), TestServerConstant.HttpsPort, listenOptions =>
                                  {
                                      listenOptions.UseHttps(Path.Combine(AppContext.BaseDirectory + @"\TestRoot.pfx"));
                                  });
                                  options.Listen(IPAddress.Parse(TestServerConstant.TestHost), TestServerConstant.HttpPort);
                              });
                });

        public static void Main(string[] args)
        {
            StartAll(args);
        }

        public static void StartAll(string[] args)
        {
            var anonymousProxyServer = new ProxyServer(TestServerConstant.ProxyPort, false);
            anonymousProxyServer.StartProxyServer();

            var authProxyServer = new ProxyServer(TestServerConstant.AuthProxyPort, true);
            authProxyServer.Authenticates.Add(TestServerConstant.ProxyUserName, new ProxyAuthenticateInfo()
            {
                UserName = TestServerConstant.ProxyUserName,
                Password = TestServerConstant.ProxyPassword,
            });
            authProxyServer.StartProxyServer();

            CreateHostBuilder(args).Build().Run();
        }

        #endregion Public 方法
    }
}