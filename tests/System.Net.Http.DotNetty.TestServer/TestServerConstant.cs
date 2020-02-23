namespace System.Net.Http.DotNetty.TestServer
{
    public static class TestServerConstant
    {
        #region Public 字段

        public const int AuthProxyPort = 5003;
        public const int HttpPort = 5000;
        public const int HttpsPort = 5001;
        public const string ProxyPassword = "password";
        public const int ProxyPort = 5002;

        public const string ProxyUserName = "username";
        public const string TestHost = "127.0.0.1";

        public const string ThroughProxy = "Through-Proxy";

        public const string CertThumbprint = "08505EB0AE069028765830EAC41BB8E6E99724A1";

        #endregion Public 字段

        #region Public 属性

        public static string EchoUrl { get; } = $"http://{TestHost}:{HttpPort}/api/echo";
        public static string EchoUrlHttps { get; } = $"https://{TestHost}:{HttpsPort}/api/echo";
        public static string ProxyUrl { get; } = $"http://{TestHost}:{ProxyPort}";
        public static string ProxyUrlAuth { get; } = $"http://{TestHost}:{AuthProxyPort}";
        public static string RequestDetailUrl { get; } = $"http://{TestHost}:{HttpPort}/api/requestdetail";
        public static string RequestDetailUrlHttps { get; } = $"https://{TestHost}:{HttpsPort}/api/requestdetail";

        #endregion Public 属性
    }
}