using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.Http.DotNetty.Test
{
    [TestClass]
    public abstract class ConnectionPoolTest
    {
        #region Private 字段

        private ConcurrentDictionary<IDotNettyClientOptions, IHostConnectionPool> _connectionPools;

        #endregion Private 字段

        #region Public 方法

        [TestCleanup]
        public virtual void Cleanup()
        {
            foreach (var item in _connectionPools)
            {
                item.Value.Dispose();
            }

            _connectionPools.Clear();
        }

        [TestInitialize]
        public virtual void Init()
        {
            _connectionPools = new ConcurrentDictionary<IDotNettyClientOptions, IHostConnectionPool>();
        }

        [TestMethod]
        public async Task Rent100LimitTestAsync()
        {
            await ConnectionLimitRentTestAsync(100, 5000).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Rent5LimitTestAsync()
        {
            await ConnectionLimitRentTestAsync(5, 500).ConfigureAwait(false);
        }

        #endregion Public 方法

        #region Internal 方法

        internal abstract IHostConnectionPool CreateConnectionPool(IDotNettyClientOptions options);

        #endregion Internal 方法

        #region Protected 方法

        protected async Task ConnectionLimitRentTestAsync(int connectionLimit, int rentCount)
        {
            var options = TestUtilities.GetDotNettyClientOptions();
            options.ForEach(m =>
            {
                m.ConnectionLimit = connectionLimit;
                _connectionPools.TryAdd(m, CreateConnectionPool(m));
            });

            await DoRentAsync(rentCount);
        }

        protected async Task DoRentAsync(int rentCount)
        {
            var uri = new Uri("http://www.baidu.com");
            //var random = new Random();

            foreach (var connectionPool in _connectionPools.Values)
            {
                var sw = Stopwatch.StartNew();
                var tasks = Enumerable.Range(0, rentCount).Select(async m =>
                {
                    IDotNettyConnection connection = null;
                    try
                    {
                        connection = await connectionPool.WaitForIdleConnection(uri, null, CancellationToken.None).ConfigureAwait(false);
                        //await Task.Delay(random.Next(1, 10)).ConfigureAwait(false);
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                    finally
                    {
                        connectionPool.Return(connection);
                    }
                }).ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);

                sw.Stop();

                Console.WriteLine($"MaxCount:{connectionPool.MaxCount} Count:{connectionPool.Count} RentCount:{rentCount} Time:{sw.Elapsed}");
                Assert.AreEqual(connectionPool.Count, connectionPool.MaxCount);
            }
        }

        #endregion Protected 方法
    }

    [TestClass]
    public class HostConnectionPoolTest : ConnectionPoolTest
    {
        #region Internal 方法

        internal override IHostConnectionPool CreateConnectionPool(IDotNettyClientOptions options)
        {
            return new HostConnectionPool(options);
        }

        #endregion Internal 方法
    }
}