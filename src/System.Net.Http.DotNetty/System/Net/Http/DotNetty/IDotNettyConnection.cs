using System.Threading;
using System.Threading.Tasks;

using DotNetty.Codecs.Http;

namespace System.Net.Http.DotNetty
{
    internal interface IDotNettyConnection : IDisposable
    {
        #region Public 属性

        /// <summary>
        /// 最后一次的使用时间
        /// </summary>
        long LastTicks { get; }

        #endregion Public 属性

        #region Public 方法

        /// <summary>
        /// 执行请求
        /// </summary>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IFullHttpResponse> ExecuteAsync(IFullHttpRequest message, CancellationToken token);

        #endregion Public 方法
    }
}