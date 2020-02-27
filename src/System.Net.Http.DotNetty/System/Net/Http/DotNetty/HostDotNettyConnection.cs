using System.Diagnostics;
using System.Net.Http.DotNetty.Handler;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using DNHttpMethod = DotNetty.Codecs.Http.HttpMethod;
using DNHttpVersion = DotNetty.Codecs.Http.HttpVersion;

namespace System.Net.Http.DotNetty
{
    /// <summary>
    /// 单个主机的连接
    /// <para/>
    /// !!! 线程不安全 !!!
    /// </summary>
    internal sealed class HostDotNettyConnection : IDotNettyConnection
    {
        #region Private 字段

        private static readonly DNHttpMethod HttpConnect = new DNHttpMethod("CONNECT");

        private readonly RemoteCertificateValidationCallback _certificateValidationCallback;
        private readonly Action<Bootstrap> _dotNettyBootstrapSetupCallback;
        private readonly Action<IChannelPipeline> _dotNettyPipelineSetupCallback;
        private readonly EndPoint _endPoint;
        private readonly int _maxLength;
        private readonly ICredentials _proxyCredentials;
        private readonly Uri _proxyUri;
        private readonly Uri _uri;
        private readonly bool _useProxy;
        private ICharSequence _authenticationString;
        private Bootstrap _bootstrap = null;
        private IChannel _channel = null;
        private ConnectionUpgradeHandler _connectionUpgradeHandler;
        private bool _disposed = false;
        private IEventLoopGroup _group = null;
        private long _lastTicks = DateTime.UtcNow.Ticks;
        private Action<IFullHttpRequest> _requestSetupFunc;
        private HttpResponseHandler _responseHandler;

        #endregion Private 字段

        #region Public 属性

        public string Host { get; }

        public bool IsHttps { get; private set; }

        public bool IsHttpsProxy { get; private set; }

        public long LastTicks => Interlocked.Read(ref _lastTicks);
        public int Port { get; }

        #endregion Public 属性

        #region Public 构造函数

        public HostDotNettyConnection(Uri uri) : this(uri, null, DotNettyClientOptions.Default)
        {
        }

        public HostDotNettyConnection(Uri uri, Uri proxyUri, IDotNettyClientOptions options)
        {
            _certificateValidationCallback = options.RemoteCertificateValidationCallback;
            _dotNettyPipelineSetupCallback = options.DotNettyPipelineSetupCallback;
            _dotNettyBootstrapSetupCallback = options.DotNettyBootstrapSetupCallback;

            _maxLength = options.MaxLength;
            _proxyCredentials = options.Proxy?.Credentials;
            _useProxy = options.Proxy != null;

            Host = uri.Host;
            Port = uri.Port;

            IsHttps = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            IsHttpsProxy = proxyUri?.Scheme?.Equals("https", StringComparison.OrdinalIgnoreCase) == true;

            _uri = uri;
            _proxyUri = proxyUri;
            _endPoint = GetEndPoint(_proxyUri ?? uri);

            InitDotNetty();
        }

        #endregion Public 构造函数

        #region Public 方法

        public static EndPoint GetEndPoint(Uri uri)
        {
            switch (uri.HostNameType)
            {
                case UriHostNameType.Unknown:
                default:
                    throw new ArgumentException("Unknown Host Type");
                case UriHostNameType.Basic:
                    throw new NotImplementedException();
                case UriHostNameType.Dns:
                    return new DnsEndPoint(uri.Host, uri.Port);

                case UriHostNameType.IPv4:
                case UriHostNameType.IPv6:
                    return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _group.ShutdownGracefullyAsync();
            _bootstrap = null;
            _group = null;
            _channel = null;
        }

        /// <summary>
        /// 执行请求
        /// <para/>
        /// 需手动调用 <see cref="IFullHttpResponse"/> 的Release方法
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<IFullHttpResponse> ExecuteAsync(IFullHttpRequest request, CancellationToken token)
        {
            CheckDisposed();

            Interlocked.Exchange(ref _lastTicks, DateTime.UtcNow.Ticks);

            _requestSetupFunc?.Invoke(request);

            var channel = await GetChannelAsync().ConfigureAwait(false);

            var completionSource = new TaskCompletionSource<IFullHttpResponse>();

            if (token.CanBeCanceled)
            {
                token.Register(() =>
                {
                    _ = CloseChannelAsync().ContinueWith(_ =>
                    {
                        completionSource.TrySetException(new OperationCanceledException());
                    }, TaskScheduler.Default);
                });
            }

            _responseHandler.SetCallback((ctx, innerResponse) =>
            {
                try
                {
                    innerResponse.Retain();
                    completionSource.TrySetResult(innerResponse);
                }
#pragma warning disable CA1031 // 不捕获常规异常类型
                catch (Exception ex)
#pragma warning restore CA1031 // 不捕获常规异常类型
                {
                    completionSource.TrySetException(ex);
                }
            });

            var sendTask = channel.WriteAndFlushAsync(request);

            try
            {
                var response = await completionSource.Task.ConfigureAwait(false);
                return response;
            }
            finally
            {
                if (!sendTask.IsCompleted)
                {
                    await CloseChannelAsync().ConfigureAwait(false);
                }
            }
        }

        #endregion Public 方法

        #region Private 方法

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }

        private async Task CloseChannelAsync()
        {
            if (Interlocked.Exchange(ref _channel, null) is IChannel channel)
            {
                Debug.WriteLine("CloseChannel");
                await channel.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task<IChannel> GetChannelAsync()
        {
            if (_channel is null)
            {
                _channel = await _bootstrap.ConnectAsync(_endPoint).ConfigureAwait(false);
                if (_connectionUpgradeHandler != null)
                {
                    var request = new DefaultFullHttpRequest(DNHttpVersion.Http11, HttpConnect, $"{Host}:{_uri.Port}");
                    if (_authenticationString != null)
                    {
                        request.Headers.Add(HttpHeaderNames.ProxyAuthorization, _authenticationString);
                    }
                    _ = _channel.WriteAndFlushAsync(request);
                    await _connectionUpgradeHandler.HandshakeTask.ConfigureAwait(false);
                    _connectionUpgradeHandler = null;
                    _requestSetupFunc = null;
                }
            }
            return _channel;
        }

        private void InitDotNetty()
        {
            _responseHandler = new HttpResponseHandler();

            if (_proxyCredentials != null)
            {
                NetworkCredential credential = _proxyCredentials.GetCredential(_uri, "Basic");

                //TODO 除了Basic的其它实现
                string authString = !string.IsNullOrEmpty(credential.Domain) ?
                    credential.Domain + "\\" + credential.UserName + ":" + credential.Password :
                    credential.UserName + ":" + credential.Password;

                _authenticationString = new StringCharSequence($"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(authString))}");

                _requestSetupFunc = request =>
                    request.Headers.Remove(HttpHeaderNames.ProxyAuthorization).Add(HttpHeaderNames.ProxyAuthorization, _authenticationString);
            }

            _group = new SingleThreadEventLoop();
            _bootstrap = new Bootstrap();
            _bootstrap
                .Group(_group)
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.SoKeepalive, true)
                .Option(ChannelOption.Allocator, new UnpooledByteBufferAllocator())
                .Channel<TcpSocketChannel>()
                .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    var aggregatorHandler = new HttpObjectAggregator(_maxLength);
                    var codecHandler = new HttpClientCodec(4096, 8192, 8192, false, false, _useProxy);

                    if (IsHttps)
                    {
                        if (_proxyUri is null)
                        {
                            pipeline.AddTlsHandler(Host, _certificateValidationCallback);
                            pipeline.AddLast(DotNettyHandlerNames.Codec, codecHandler);
                            pipeline.AddLast(DotNettyHandlerNames.Aggregator, aggregatorHandler);
                        }
                        else
                        {
                            pipeline.AddLast(DotNettyHandlerNames.Codec, codecHandler);
                            pipeline.AddLast(DotNettyHandlerNames.Aggregator, aggregatorHandler);
                            _connectionUpgradeHandler = new ConnectionUpgradeHandler(Host, _certificateValidationCallback);
                            pipeline.AddLast(DotNettyHandlerNames.ConnectionUpgrade, _connectionUpgradeHandler);
                        }
                    }
                    else
                    {
                        pipeline.AddLast(DotNettyHandlerNames.Codec, codecHandler);
                        pipeline.AddLast(DotNettyHandlerNames.Aggregator, aggregatorHandler);
                    }

                    pipeline.AddLast(DotNettyHandlerNames.Response, _responseHandler);

                    _dotNettyPipelineSetupCallback?.Invoke(pipeline);
                }));

            _dotNettyBootstrapSetupCallback?.Invoke(_bootstrap);
        }

        #endregion Private 方法
    }
}