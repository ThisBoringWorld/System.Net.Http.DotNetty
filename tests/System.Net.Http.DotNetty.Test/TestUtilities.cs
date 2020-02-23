using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Http.DotNetty.Test
{
    internal class TestUtilities
    {
        public static List<DotNettyClientOptions> GetDotNettyClientOptions()
        {
            var result = new List<DotNettyClientOptions>()
            {
                new DotNettyClientOptions()
                {
                    ConnectionLimit = 5,
                    Proxy = new WebProxy("http://127.0.0.1:8888"),
                },
                new DotNettyClientOptions()
                {
                    ConnectionLimit = 5,
                    Proxy = new WebProxy("http://127.0.0.1:8889"),
                },
                new DotNettyClientOptions()
                {
                    ConnectionLimit = 5,
                    Proxy = new WebProxy("http://192.168.0.1:8888"),
                },
                new DotNettyClientOptions()
                {
                    ConnectionLimit = 5,
                    Proxy = new WebProxy("http://192.168.0.1:8889"),
                },
                new DotNettyClientOptions()
                {
                    ConnectionLimit = 5,
                    Proxy = new WebProxy("https://192.168.0.1:443"),
                },
                new DotNettyClientOptions()
                {
                    ConnectionLimit = 5,
                    Proxy = new WebProxy("https://192.168.0.1:442"),
                },
            };
            return result;
        }
    }
}
