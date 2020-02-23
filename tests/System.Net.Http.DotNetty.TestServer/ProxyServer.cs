using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

using TProxyServer = Titanium.Web.Proxy.ProxyServer;

namespace System.Net.Http.DotNetty.TestServer
{
    public class ProxyAuthenticateInfo
    {
        #region Public 字段

        public int ConnectTime = 0;

        public int RequestTime = 0;

        #endregion Public 字段

        #region Public 属性

        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        #endregion Public 属性
    }

    /// <summary>
    /// 代理服务器
    /// </summary>
    public class ProxyServer
    {
        #region Private 字段

        private readonly bool _auth;
        private readonly ExplicitProxyEndPoint _explicitProxy;

        private readonly TProxyServer _proxyServer = new TProxyServer(false, false, false)
        {
            ThreadPoolWorkerThread = 100,
            MaxCachedConnections = 100,
        };

        #endregion Private 字段

        #region Public 属性

        public Dictionary<string, ProxyAuthenticateInfo> Authenticates { get; } = new Dictionary<string, ProxyAuthenticateInfo>();
        public bool IsSystemProxy { get; set; } = false;

        public ProxyAuthenticateInfo SystemProxyInfo { get; set; }

        #endregion Public 属性

        #region Public 构造函数

        public ProxyServer(int port, bool auth)
        {
            _explicitProxy = new ExplicitProxyEndPoint(IPAddress.Any, port, false);
            _auth = auth;
        }

        #endregion Public 构造函数

        #region Public 方法

        public void DisableSystemProxy()
        {
            _proxyServer.DisableSystemProxy(ProxyProtocolType.Http);
            if (_auth)
            {
                _proxyServer.ProxyBasicAuthenticateFunc = BasicAuthenticate;
            }
            IsSystemProxy = false;
            Debug.WriteLine(nameof(DisableSystemProxy));
        }

        public void SetAsSystemProxy()
        {
            SystemProxyInfo = new ProxyAuthenticateInfo();
            _proxyServer.SetAsSystemProxy(_explicitProxy, ProxyProtocolType.Http);
            _proxyServer.ProxyBasicAuthenticateFunc = null;
            IsSystemProxy = true;
            Debug.WriteLine(nameof(SetAsSystemProxy));
        }

        public void StartProxyServer()
        {
            _proxyServer.AddEndPoint(_explicitProxy);
            if (_auth)
            {
                _proxyServer.ProxyBasicAuthenticateFunc = BasicAuthenticate;
            }
            else
            {
                _proxyServer.ProxyBasicAuthenticateFunc = null;
            }
            _proxyServer.BeforeRequest += BeforeRequest;
            _proxyServer.BeforeResponse += BeforeResponse;
            _proxyServer.Start();
            Debug.WriteLine(nameof(StartProxyServer));
        }

        public void StopProxyServer()
        {
            _proxyServer.Stop();
            Debug.WriteLine(nameof(StopProxyServer));
        }

        #endregion Public 方法

        #region Private 方法

        private async Task<bool> BasicAuthenticate(SessionEventArgsBase session, string username, string password)
        {
            if (!_auth)
            {
                return true;
            }
            await Task.CompletedTask;
            if (Authenticates.TryGetValue(username, out var accountInfo))
            {
                if (password.Equals(accountInfo.Password))
                {
                    Interlocked.Increment(ref accountInfo.ConnectTime);
                    return true;
                }
            }

            return false;
        }

        private Task BeforeRequest(object sender, SessionEventArgs e)
        {
            if (e.HttpClient.Request.Url.StartsWith("http://www.test.baidu", StringComparison.Ordinal))
            {
                e.GenericResponse(Encoding.UTF8.GetBytes("Over"), HttpStatusCode.OK, null, false);
                return Task.CompletedTask;
            }
            if (!_auth)
            {
                return Task.CompletedTask;
            }
            if (IsSystemProxy)
            {
                if (e.HttpClient.Request.Url.StartsWith(TestServerConstant.TestHost, StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref SystemProxyInfo.RequestTime);
                }
                return Task.CompletedTask;
            }
            if (e.HttpClient.Request.Headers.Headers.TryGetValue("Proxy-Authorization", out var authorizationHeader))
            {
                var authorizationBase64String = authorizationHeader.Value.Split(' ')[1];
                var authorization = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationBase64String));
                var authorizations = authorization.Split(':');
                var username = authorizations[0];
                var password = authorizations[1];

                var accountInfo = Authenticates[username];
                if (password.Equals(accountInfo.Password, StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref accountInfo.RequestTime);
                }
            }

            return Task.CompletedTask;
        }

        private Task BeforeResponse(object sender, SessionEventArgs e)
        {
            e.HttpClient.Response.Headers.AddHeader(TestServerConstant.ThroughProxy, "1");
            return Task.CompletedTask;
        }

        #endregion Private 方法
    }
}