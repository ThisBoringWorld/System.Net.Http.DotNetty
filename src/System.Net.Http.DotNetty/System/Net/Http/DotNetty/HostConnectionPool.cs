using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.DotNetty
{
    internal class HostConnectionPool : IHostConnectionPool
    {
        #region Private 字段

        private readonly ConcurrentQueue<IDotNettyConnection> _connectionQueue = new ConcurrentQueue<IDotNettyConnection>();
        private readonly IDotNettyClientOptions _options;
        private readonly SemaphoreSlim _waitEvent;
        private SpinLock _cleanupLock = new SpinLock(false);
        private int _count = 0;
        private bool _disposed = false;
        private long _lastTicks = DateTime.UtcNow.Ticks;

        #endregion Private 字段

        #region Public 属性

        public int Count => _count;
        public long LastTicks => Interlocked.Read(ref _lastTicks);
        public int MaxCount { get; }

        #endregion Public 属性

        #region Public 构造函数

        public HostConnectionPool() : this(DotNettyClientOptions.Default)
        {
        }

        public HostConnectionPool(IDotNettyClientOptions options)
        {
            _options = options;
            MaxCount = _options.ConnectionLimit;

            if (MaxCount < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(options.ConnectionLimit)} must be greater than 0");
            }

            _waitEvent = new SemaphoreSlim(options.ConnectionLimit, options.ConnectionLimit);
        }

        #endregion Public 构造函数

        #region Public 方法

        public Task<IEnumerable<IDisposable>> Cleanup()
        {
            CheckDisposed();

            var cleanupConnections = new List<IDisposable>();
            var returnConnections = new List<IDotNettyConnection>();

            bool gotLock = false;

            try
            {
                _cleanupLock.Enter(ref gotLock);

                while (_connectionQueue.TryDequeue(out var connection))
                {
                    var connectionTimespan = DateTime.UtcNow - new DateTime(connection.LastTicks);
                    if (connectionTimespan >= _options.ResourcesTimeout)
                    {
                        cleanupConnections.Add(connection);
                    }
                    else
                    {
                        returnConnections.Add(connection);
                    }
                }
                if (returnConnections.Count > 0)
                {
                    foreach (var item in returnConnections)
                    {
                        _connectionQueue.Enqueue(item);
                    }
                }
            }
            finally
            {
                if (gotLock)
                {
                    _cleanupLock.Exit(false);
                }
            }

            if (cleanupConnections.Count > 0)
            {
                Interlocked.Add(ref _count, -cleanupConnections.Count);
            }
            return Task.FromResult(cleanupConnections as IEnumerable<IDisposable>);
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _disposed))
            {
                return;
            }

            Volatile.Write(ref _disposed, true);

            var allConnection = Enumerable.Range(0, Count).Select(m =>
            {
                SpinWait spinWait = new SpinWait();
                IDotNettyConnection connection;
                while (!_connectionQueue.TryDequeue(out connection))
                {
                    spinWait.SpinOnce();
                }
                return connection;
            }).ToArray();

            Debug.WriteLine($"{nameof(HostConnectionPool)} Dispose {nameof(IDotNettyConnection)} {allConnection.Length}");

            foreach (var item in allConnection)
            {
                item.Dispose();
            }

            _waitEvent.Dispose();
        }

        public void Return(IDotNettyConnection connection)
        {
            _connectionQueue.Enqueue(connection);
            _waitEvent.Release();

            Interlocked.Exchange(ref _lastTicks, DateTime.UtcNow.Ticks);
        }

        public async Task<IDotNettyConnection> WaitForIdleConnection(Uri uri, Uri proxyUri, CancellationToken token)
        {
            CheckDisposed();

            await _waitEvent.WaitAsync(token).ConfigureAwait(false);

            bool gotLock = false;
            IDotNettyConnection connection;

            try
            {
                _cleanupLock.Enter(ref gotLock);
                if (!_connectionQueue.TryDequeue(out connection))
                {
                    if (_count < MaxCount)
                    {
                        CheckDisposed();
                        _count++;
                        connection = new HostDotNettyConnection(uri, proxyUri, _options);
                    }
                    else
                    {
                        throw new NotImplementedException("未知情况。。。");
                    }
                }
            }
            finally
            {
                if (gotLock)
                {
                    _cleanupLock.Exit(false);
                }
            }
            return connection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (Volatile.Read(ref _disposed))
            {
                throw new ObjectDisposedException(null);
            }
        }

        #endregion Public 方法
    }
}