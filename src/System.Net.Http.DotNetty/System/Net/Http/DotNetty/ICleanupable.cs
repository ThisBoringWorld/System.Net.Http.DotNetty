using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Http.DotNetty
{
    public interface ICleanupable
    {
        #region Public 方法

        Task<IEnumerable<IDisposable>> Cleanup();

        #endregion Public 方法
    }
}