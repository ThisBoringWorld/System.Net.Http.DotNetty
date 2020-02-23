using System.Threading;
using System.Threading.Tasks;

using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;

namespace System.Net.Http.DotNetty
{
    public sealed class HttpDotNettyClientHandler : HttpMessageHandler
    {
        #region Private 属性

        private readonly IDotNettyClientOptions _options;

        internal DotNettyHttpRequestExecutor RequestExecutor { get; }

        #endregion Private 属性

        #region Public 构造函数

        public HttpDotNettyClientHandler() : this(DotNettyClientOptions.Default)
        {
        }

        public HttpDotNettyClientHandler(IDotNettyClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            RequestExecutor = new DotNettyHttpRequestExecutor(options);
        }

        #endregion Public 构造函数

        #region Private 方法

        private async Task<HttpResponseMessage> InternalExecuteAsync(HttpRequestMessage message, Uri proxyUri, CancellationToken token)
        {
            var targetUri = message.RequestUri;

            //TODO 过滤不支持的http版本
            var request = await message.ToFullHttpRequestAsync(false).ConfigureAwait(false);

            IFullHttpResponse response = null;
            HttpResponseMessage outterResponse;

            try
            {
                response = await RequestExecutor.InternalExecuteAsync(request, targetUri, proxyUri, token).ConfigureAwait(false);
                outterResponse = response.ToHttpResponseMessage();
            }
            finally
            {
                response.SafeRelease();
            }

            outterResponse.Version = message.Version;
            outterResponse.RequestMessage = message;

            return outterResponse;
        }

        #endregion Private 方法

        #region Protected 方法

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            RequestExecutor.Dispose();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token)
        {
            if (token.CanBeCanceled)
            {
                using (var newTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    newTokenSource.CancelAfter(DotNettyClientOptions.DefaultTimeout);
#pragma warning disable CA1062 // 验证公共方法的参数
                    return await InternalExecuteAsync(message, DotNettyHttpRequestExecutor.GetProxyUri(message.RequestUri, _options.Proxy), newTokenSource.Token).ConfigureAwait(false);
#pragma warning restore CA1062 // 验证公共方法的参数
                }
            }
            else
            {
                return await InternalExecuteAsync(message, DotNettyHttpRequestExecutor.GetProxyUri(message.RequestUri, _options.Proxy), token).ConfigureAwait(false);
            }
        }

        #endregion Protected 方法
    }
}