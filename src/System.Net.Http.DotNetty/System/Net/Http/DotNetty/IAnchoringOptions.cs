namespace System.Net.Http.DotNetty
{
    /// <summary>
    /// 应用后不可更改的配置
    /// </summary>
    public interface IAnchoringOptions
    {
        #region Public 方法

        /// <summary>
        /// 检查已应用
        /// </summary>
        void CheckApplied();

        /// <summary>
        /// 设置已应用
        /// </summary>
        void SetApplied();

        #endregion Public 方法
    }
}