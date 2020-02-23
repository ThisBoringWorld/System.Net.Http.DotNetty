using System.Net.Security;
using System.Threading.Tasks;

using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace System.Net.Http.DotNetty.Handler
{
    /// <summary>
    /// 连接升级
    /// </summary>
    internal class ConnectionUpgradeHandler : SimpleChannelInboundHandler<IFullHttpResponse>
    {
        #region Private 字段

        private readonly AsciiString _connectionEstablished = AsciiString.Cached("Connection Established");
        private readonly TaskCompletionSource<int> _handshakeTCS;
        private readonly string _host;
        private readonly RemoteCertificateValidationCallback _remoteCertificateValidationCallback;

        #endregion Private 字段

        #region Public 属性

        public Task HandshakeTask => _handshakeTCS.Task;

        #endregion Public 属性

        #region Public 构造函数

        public ConnectionUpgradeHandler(string host, RemoteCertificateValidationCallback remoteCertificateValidationCallback)
        {
            _host = host;
            _remoteCertificateValidationCallback = remoteCertificateValidationCallback;

            _handshakeTCS = new TaskCompletionSource<int>();
        }

        #endregion Public 构造函数

        #region Protected 方法

        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            base.HandlerRemoved(context);
            _handshakeTCS.SetResult(1);
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpResponse response)
        {
            if (response.Status.ReasonPhrase.ContentEqualsIgnoreCase(_connectionEstablished))
            {
                ctx.Channel.Pipeline.AddTlsHandler(_host, _remoteCertificateValidationCallback);
                ctx.Channel.Pipeline.Remove(DotNettyHandlerNames.ConnectionUpgrade);
            }
        }

        #endregion Protected 方法
    }
}