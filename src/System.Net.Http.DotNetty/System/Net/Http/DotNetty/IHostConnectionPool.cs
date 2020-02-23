using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.DotNetty
{
    internal interface IHostConnectionPool : ICleanupable, IDisposable
    {
        #region Public 属性

        /// <summary>
        /// 池中连接数
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 最后一次的使用时间
        /// </summary>
        long LastTicks { get; }

        /// <summary>
        /// 池中最大连接数
        /// </summary>
        int MaxCount { get; }

        #endregion Public 属性

        #region Public 方法

        /// <summary>
        /// 等待一个空闲连接
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="proxyUri"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IDotNettyConnection> WaitForIdleConnection(Uri uri, Uri proxyUri, CancellationToken token);

        /// <summary>
        /// 归还链接
        /// </summary>
        /// <param name="connection"></param>
        void Return(IDotNettyConnection connection);

        #endregion Public 方法
    }
}