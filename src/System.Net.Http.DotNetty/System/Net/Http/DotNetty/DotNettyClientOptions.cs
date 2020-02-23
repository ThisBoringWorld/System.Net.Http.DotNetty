using System.Net.Security;

using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;

namespace System.Net.Http.DotNetty
{
    /// <summary>
    /// DotNetty客户端选项
    /// </summary>
    public interface IDotNettyClientOptions : IAnchoringOptions
    {
        #region Public 属性

        /// <summary>
        /// 针对单个连接目标地址的最大连接数
        /// </summary>
        int ConnectionLimit { get; }

        /// <summary>
        /// dotnet bootstrap 设置回调
        /// </summary>
        Action<Bootstrap> DotNettyBootstrapSetupCallback { get; }

        /// <summary>
        /// dotnet pipeline 设置回调
        /// </summary>
        Action<IChannelPipeline> DotNettyPipelineSetupCallback { get; }

        /// <summary>
        /// 最大http响应长度
        /// </summary>
        int MaxLength { get; }

        /// <summary>
        /// 代理
        /// </summary>
        IWebProxy Proxy { get; }

        /// <summary>
        /// 远程证书验证回调
        /// </summary>
        RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; }

        /// <summary>
        /// 资源超时 检查间隔
        /// </summary>
        TimeSpan ResourcesCheckInterval { get; }

        /// <summary>
        /// 资源超时
        /// <para/>
        /// 超时后进行回收，如果短于长请求时间，会出现异常
        /// </summary>
        TimeSpan ResourcesTimeout { get; }

        #endregion Public 属性
    }

    /// <summary>
    /// DotNetty客户端选项
    /// </summary>
    public sealed class DotNettyClientOptions : IDotNettyClientOptions, IAnchoringOptions
    {
        #region 默认设置

        /// <summary>
        /// 默认设置
        /// </summary>
        public static IDotNettyClientOptions Default { get; } = new DefaultDotNettyClientOptions();

        #endregion 默认设置

        #region 常量定义

        /// <summary>
        /// 默认最大http响应长度
        /// </summary>
        public const int DefaultMaxLength = 2 * 1024 * 1024;

        /// <summary>
        /// 资源超时 检查间隔
        /// </summary>
        public static readonly TimeSpan DefaultResourcesCheckInterval = TimeSpan.FromMinutes(2);

        /// <summary>
        /// 默认资源超时
        /// </summary>
        public static readonly TimeSpan DefaultResourcesTimeout = TimeSpan.FromMinutes(3.5);

        /// <summary>
        /// 默认请求超时时间
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);

        #endregion 常量定义

        #region Private 字段

        /// <summary>
        /// 是否已应用
        /// </summary>
        private bool _alreadyApplied = false;

        private int _connectionLimit = Default.ConnectionLimit;
        private Action<Bootstrap> _dotNettyBootstrapSetupCallback = Default.DotNettyBootstrapSetupCallback;
        private Action<IChannelPipeline> _dotNettyPipelineSetupCallback = Default.DotNettyPipelineSetupCallback;
        private int _maxLength = DefaultMaxLength;
        private IWebProxy _proxy = Default.Proxy;
        private RemoteCertificateValidationCallback _remoteCertificateValidationCallback = Default.RemoteCertificateValidationCallback;
        private TimeSpan _resourcesCheckInterval = Default.ResourcesCheckInterval;
        private TimeSpan _resourcesTimeout = Default.ResourcesTimeout;

        #endregion Private 字段

        #region Public 属性

        /// <summary>
        /// 针对单个连接目标地址的最大连接数
        /// </summary>
        public int ConnectionLimit { get => _connectionLimit; set => ChangeValue(ref _connectionLimit, value); }

        /// <summary>
        /// dotnet bootstrap 设置回调
        /// </summary>
        public Action<Bootstrap> DotNettyBootstrapSetupCallback { get => _dotNettyBootstrapSetupCallback; set => ChangeValue(ref _dotNettyBootstrapSetupCallback, value); }

        /// <summary>
        /// dotnet pipeline 设置回调
        /// </summary>
        public Action<IChannelPipeline> DotNettyPipelineSetupCallback { get => _dotNettyPipelineSetupCallback; set => ChangeValue(ref _dotNettyPipelineSetupCallback, value); }

        /// <summary>
        /// 最大http响应长度
        /// <para/>
        /// 默认为 <see cref="DefaultMaxLength"/>
        /// </summary>
        public int MaxLength { get => _maxLength; set => ChangeValue(ref _maxLength, value); }

        /// 代理
        /// </summary>
        public IWebProxy Proxy { get => _proxy; set => ChangeValue(ref _proxy, value); }

        /// <summary>
        /// 远程证书验证回调
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get => _remoteCertificateValidationCallback; set => ChangeValue(ref _remoteCertificateValidationCallback, value); }

        /// <summary>
        /// 资源超时 检查间隔
        /// </summary>
        public TimeSpan ResourcesCheckInterval { get => _resourcesCheckInterval; set => ChangeValue(ref _resourcesCheckInterval, value); }

        /// <summary>
        /// 资源超时
        /// <para/>
        /// 超时后进行回收，如果短于长请求时间，会出现异常
        /// </summary>
        public TimeSpan ResourcesTimeout { get => _resourcesTimeout; set => ChangeValue(ref _resourcesTimeout, value); }

        #endregion Public 属性

        #region 状态

        void IAnchoringOptions.CheckApplied()
        {
            if (_alreadyApplied)
            {
                throw new InvalidOperationException("do not change value for a applied option");
            }
        }

        void IAnchoringOptions.SetApplied()
        {
            _alreadyApplied = true;
        }

        private void ChangeValue<T>(ref T target, T value)
        {
            (this as IAnchoringOptions).CheckApplied();
            target = value;
        }

        #endregion 状态
    }

    /// <summary>
    /// 默认DotNetty客户端选项
    /// </summary>
    internal class DefaultDotNettyClientOptions : IDotNettyClientOptions
    {
        #region Public 属性

        /// <summary>
        /// 针对单个连接目标地址的最大连接数
        /// </summary>
        public int ConnectionLimit { get; } = ServicePointManager.DefaultConnectionLimit;

        public Action<Bootstrap> DotNettyBootstrapSetupCallback => null;

        public Action<IChannelPipeline> DotNettyPipelineSetupCallback => null;

        public int MaxLength => DotNettyClientOptions.DefaultMaxLength;

        /// <summary>
        /// 代理
        /// </summary>
        public IWebProxy Proxy { get; } = WebRequest.DefaultWebProxy;

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback => CertUtil.CheckByLocalMachineCerts;

        /// <summary>
        /// 资源超时 检查间隔
        /// </summary>
        public TimeSpan ResourcesCheckInterval => DotNettyClientOptions.DefaultResourcesCheckInterval;

        /// <summary>
        /// 资源超时
        /// <para/>
        /// 超时后进行回收，如果短于长请求时间，会出现异常
        /// </summary>
        public TimeSpan ResourcesTimeout => DotNettyClientOptions.DefaultResourcesTimeout;

        #endregion Public 属性

        #region Public 方法

        void IAnchoringOptions.CheckApplied()
        {
        }

        void IAnchoringOptions.SetApplied()
        {
        }

        #endregion Public 方法
    }
}