using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DotNetty.Codecs.Http;

namespace System.Net.Http.DotNetty
{
    public sealed class DotNettyHttpRequestExecutor : IDisposable
    {
        #region Private 字段

        private readonly CancellationTokenSource _autoCleanCTS;

        private readonly SemaphoreSlim _connectionsLock = new SemaphoreSlim(1, 1);
        private readonly object _lockRoot = new object();
        private readonly IDotNettyClientOptions _options;
        private bool _disposed = false;

        /// <summary>
        /// 最后进行清理的时间
        /// </summary>
        private DateTime _lastCleanupTime = DateTime.UtcNow;

        internal Dictionary<int, HostConnectionPool> HostConnections { get; } = new Dictionary<int, HostConnectionPool>();

        #endregion Private 字段

        #region Public 构造函数

        public DotNettyHttpRequestExecutor(IDotNettyClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (options.ResourcesTimeout <= TimeSpan.FromSeconds(0))
            {
                throw new ArgumentOutOfRangeException($"{nameof(options)}.{nameof(options.ResourcesTimeout)}");
            }

            if (options.ResourcesCheckInterval <= TimeSpan.FromSeconds(0))
            {
                throw new ArgumentOutOfRangeException($"{nameof(options)}.{nameof(options.ResourcesCheckInterval)}");
            }

            if (_options is IAnchoringOptions anchoringOptions)
            {
                anchoringOptions.SetApplied();
            }

            _autoCleanCTS = new CancellationTokenSource();

            var token = _autoCleanCTS.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(options.ResourcesCheckInterval, token).ConfigureAwait(false);

                    Debug.WriteLine($"{DateTime.Now}:Start Auto Cleanup");

                    await Cleanup(token).ConfigureAwait(false);

                    Debug.WriteLine($"{DateTime.Now}:Auto Cleanup Over");
                }
            }, token);
        }

        #endregion Public 构造函数

        #region Public 方法

        /// <summary>
        /// 清理资源、连接等
        /// </summary>
        public async Task Cleanup(CancellationToken token)
        {
            CheckDisposed();

            List<IDisposable> waitDisposeObjects = null;

            await _connectionsLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
#if DEBUG
                var minInterval = TimeSpan.FromSeconds(0.1);
#else
                var minInterval = TimeSpan.FromSeconds(5);
#endif
                if (DateTime.UtcNow - _lastCleanupTime < minInterval)
                {
                    Debug.WriteLine("Cleanup too frequently");
                    return;
                }

                _lastCleanupTime = DateTime.UtcNow;

                token.ThrowIfCancellationRequested();

                Debug.WriteLine("Start Cleanup");

                waitDisposeObjects = new List<IDisposable>();
                var keys = HostConnections.Keys.ToArray();

                foreach (var key in keys)
                {
                    token.ThrowIfCancellationRequested();
                    if (HostConnections.TryGetValue(key, out var connectionPool))
                    {
                        if (connectionPool.Count > 0)
                        {
                            if (await connectionPool.Cleanup().ConfigureAwait(false) is IEnumerable<IDisposable> objs)
                            {
                                waitDisposeObjects.AddRange(objs);
                            }
                        }
                        if (connectionPool.Count == 0)
                        {
                            if (HostConnections.Remove(key))
                            {
                                waitDisposeObjects.Add(connectionPool);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested)
                {
                    throw;
                }
            }
            finally
            {
                _connectionsLock.Release();
            }

            if (waitDisposeObjects != null)
            {
                Debug.WriteLine($"Start Dispose Count:{waitDisposeObjects.Count}");
                foreach (var item in waitDisposeObjects)
                {
                    item.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _disposed))
            {
                return;
            }

            lock (_lockRoot)
            {
                if (Volatile.Read(ref _disposed))
                {
                    return;
                }
                Volatile.Write(ref _disposed, true);
            }

            _autoCleanCTS.Cancel(true);

            foreach (var item in HostConnections.Values)
            {
                item.Dispose();
            }

            HostConnections.Clear();

            _connectionsLock.Dispose();
            _autoCleanCTS.Dispose();
        }

        #region IFullHttpRequest

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<IFullHttpResponse> ExecuteAsync(IFullHttpRequest request)
        {
#pragma warning disable CA1062 // 验证公共方法的参数
            var targetUri = new Uri(request.Uri);
#pragma warning restore CA1062 // 验证公共方法的参数
            return ExecuteAsync(request, targetUri, GetProxyUri(targetUri, _options.Proxy), CancellationToken.None);
        }

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<IFullHttpResponse> ExecuteAsync(IFullHttpRequest request, CancellationToken token)
        {
#pragma warning disable CA1062 // 验证公共方法的参数
            var targetUri = new Uri(request.Uri);
#pragma warning restore CA1062 // 验证公共方法的参数
            return ExecuteAsync(request, targetUri, GetProxyUri(targetUri, _options.Proxy), token);
        }

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="targetUri"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<IFullHttpResponse> ExecuteAsync(IFullHttpRequest request, Uri targetUri, CancellationToken token) =>
            ExecuteAsync(request, targetUri, GetProxyUri(targetUri, _options.Proxy), token);

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="targetUri"></param>
        /// <param name="proxy"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<IFullHttpResponse> ExecuteAsync(IFullHttpRequest request, Uri targetUri, IWebProxy proxy, CancellationToken token) =>
            ExecuteAsync(request, targetUri, GetProxyUri(targetUri, proxy), token);

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="targetUri"></param>
        /// <param name="proxyUri"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<IFullHttpResponse> ExecuteAsync(IFullHttpRequest request, Uri targetUri, Uri proxyUri, CancellationToken token)
        {
            CheckDisposed();

            if (token.CanBeCanceled)
            {
                using (var newTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    newTokenSource.CancelAfter(DotNettyClientOptions.DefaultTimeout);
                    return await InternalExecuteAsync(request, targetUri, proxyUri, newTokenSource.Token).ConfigureAwait(false);
                }
            }
            else
            {
                return await InternalExecuteAsync(request, targetUri, proxyUri, token).ConfigureAwait(false);
            }
        }

        internal async Task<IFullHttpResponse> InternalExecuteAsync(IFullHttpRequest request, Uri targetUri, Uri proxyUri, CancellationToken token)
        {
            if (!request.Headers.Contains(HttpHeaderNames.Host))
            {
                request.Headers.Add(HttpHeaderNames.Host, $"{targetUri.Host}:{targetUri.Port}");
            }

            int targetHash = GetTargetHashCode(targetUri, proxyUri);

            var connectionPool = await GetConnectionQueueAsync(targetHash).ConfigureAwait(false);

            IDotNettyConnection connection = null;
            try
            {
#pragma warning disable CA2000 // 丢失范围之前释放对象
                connection = await connectionPool.WaitForIdleConnection(targetUri, proxyUri, token).ConfigureAwait(false);
#pragma warning restore CA2000 // 丢失范围之前释放对象
                return await connection.ExecuteAsync(request, token).ConfigureAwait(false);
            }
            finally
            {
                if (connection != null)
                {
                    connectionPool.Return(connection);
                }
            }
        }

        #endregion IFullHttpRequest

        #endregion Public 方法

        #region 工具方法

        /// <summary>
        /// 获取代理Uri
        /// </summary>
        /// <param name="targetUrl"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static Uri GetProxyUri(string targetUrl, IWebProxy proxy)
        {
            if (proxy is null)
            {
                return null;
            }

            var targetUri = new Uri(targetUrl);

            Uri proxyUri = proxy.GetProxy(targetUri);

            //HACK FX下会可能返回自身。。。
            if (proxyUri != null && targetUri.Equals(proxyUri))
            {
                proxyUri = null;
            }

            return proxyUri;
        }

        /// <summary>
        /// 获取代理Uri
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static Uri GetProxyUri(Uri targetUri, IWebProxy proxy)
        {
            if (proxy is null)
            {
                return null;
            }

            Uri proxyUri = proxy.GetProxy(targetUri);

            //HACK FX下会可能返回自身。。。
#pragma warning disable CA1062 // 验证公共方法的参数
            if (proxyUri != null && targetUri.Equals(proxyUri))
#pragma warning restore CA1062 // 验证公共方法的参数
            {
                proxyUri = null;
            }

            return proxyUri;
        }

        /// <summary>
        /// 获取 Uri 的HashCode
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static int GetTargetHashCode(Uri uri)
        {
            if (uri is null)
            {
                return 0;
            }
            var hashCode = -1630794015;
            hashCode = hashCode * -1521134295 + uri.Host.GetHashCode();
            hashCode = hashCode * -1521134295 + uri.Port;
            return hashCode;
        }

        /// <summary>
        /// 获取 Uri 的HashCode
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="uri2"></param>
        /// <returns></returns>
        public static int GetTargetHashCode(Uri uri, Uri uri2)
        {
            if (uri is null)
            {
                return 0;
            }

            if (uri2 is null)
            {
                var hashCode = -1630794015;
                hashCode = hashCode * -1521134295 + uri.Host.GetHashCode();
                hashCode = hashCode * -1521134295 + uri.Port;
                return hashCode;
            }
            else
            {
                var hashCode = -1630794015;
                hashCode = hashCode * -1521134295 + uri.Host.GetHashCode();
                hashCode = hashCode * -1521134295 + uri.Port;
                hashCode = hashCode * -1521134295 + uri2.Host.GetHashCode();
                hashCode = hashCode * -1521134295 + uri2.Port;
                return hashCode;
            }
        }

        #endregion 工具方法

        #region Private 方法

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (Volatile.Read(ref _disposed))
            {
                throw new ObjectDisposedException(null);
            }
        }

        private async Task<IHostConnectionPool> GetConnectionQueueAsync(int targetHash)
        {
            HostConnectionPool pool;
            await _connectionsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!HostConnections.TryGetValue(targetHash, out pool))
                {
                    CheckDisposed();
                    pool = new HostConnectionPool(_options);
                    HostConnections.Add(targetHash, pool);
                }
            }
            finally
            {
                _connectionsLock.Release();
            }

            return pool;
        }

        #endregion Private 方法
    }
}