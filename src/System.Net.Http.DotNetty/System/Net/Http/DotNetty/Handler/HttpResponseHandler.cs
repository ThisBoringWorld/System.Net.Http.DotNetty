using System.Threading;

using DotNetty.Codecs.Http;
using DotNetty.Transport.Channels;

namespace System.Net.Http.DotNetty.Handler
{
    internal class HttpResponseHandler : SimpleChannelInboundHandler<object>
    {
        #region Private 字段

        private Action<IChannelHandlerContext, IFullHttpResponse> _callback;

        #endregion Private 字段

        #region Public 属性

        public override bool IsSharable => true;

        #endregion Public 属性

        #region Public 方法

        public void SetCallback(Action<IChannelHandlerContext, IFullHttpResponse> callback)
        {
            Volatile.Write(ref _callback, callback);
        }

        #endregion Public 方法

        #region Protected 方法

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            if (msg is IFullHttpResponse response)
            {
                if (Volatile.Read(ref _callback) is Action<IChannelHandlerContext, IFullHttpResponse> callback)
                {
                    callback.Invoke(ctx, response);
                }
            }
            else
            {
            }
        }

        #endregion Protected 方法
    }
}