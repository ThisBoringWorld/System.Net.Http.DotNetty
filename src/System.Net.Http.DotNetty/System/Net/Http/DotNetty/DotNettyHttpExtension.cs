using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Threading.Tasks;

using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

using DNHttpMethod = DotNetty.Codecs.Http.HttpMethod;
using DNHttpVersion = DotNetty.Codecs.Http.HttpVersion;

namespace System.Net.Http.DotNetty
{
    internal static partial class DotNettyHttpExtension
    {
        #region RequestMessage、Response

        public static async Task<IFullHttpRequest> ToFullHttpRequestAsync(this HttpRequestMessage message, bool fullUri = true)
        {
            var request = new DefaultFullHttpRequest(DNHttpVersion.Http11, new DNHttpMethod(message.Method.Method), fullUri ? message.RequestUri.AbsoluteUri : message.RequestUri.PathAndQuery);

            foreach (var item in message.Headers)
            {
                request.Headers.Add(AsciiString.Cached(item.Key), item.Value);
            }

            if (message.Content != null)
            {
                //HACK 优化其余Header的处理
                if (message.Content.Headers.ContentType != null)
                {
                    request.Headers.Add(HttpHeaderNames.ContentType, message.Content.Headers.ContentType);
                }
                if (message.Content.Headers.ContentLength != null)
                {
                    request.Headers.Add(HttpHeaderNames.ContentLength, message.Content.Headers.ContentLength);
                }

                var content = await message.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                request.Content.WriteBytes(content, 0, content.Length);
            }

            return request;
        }

        public static HttpResponseMessage ToHttpResponseMessage(this IFullHttpResponse response)
        {
            var result = new HttpResponseMessage((HttpStatusCode)response.Status.Code);

            foreach (var item in response.Headers)
            {
                result.Headers.TryAddWithoutValidation(item.Key.ToString(), item.Value.ToString());
            }
            var contentLength = response.Headers.Get(HttpHeaderNames.ContentLength, null);
            var length = int.Parse(contentLength.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            var data = new byte[length];

            response.Content.ReadBytes(data, 0, length);

            result.Content = new StreamContent(new MemoryStream(data));

            result.Version = new Version(response.ProtocolVersion.MajorVersion, response.ProtocolVersion.MinorVersion);
            return result;
        }

        #endregion RequestMessage、Response

        #region IChannelPipeline

        public static TlsHandler AddTlsHandler(this IChannelPipeline pipeline, string host, RemoteCertificateValidationCallback remoteCertificateValidationCallback)
        {
            var tlsHandler = new TlsHandler(stream => new SslStream(stream, true, remoteCertificateValidationCallback), new ClientTlsSettings(host));
            pipeline.AddFirst(DotNettyHandlerNames.TLS, tlsHandler);

            return tlsHandler;
        }

        #endregion IChannelPipeline
    }
}