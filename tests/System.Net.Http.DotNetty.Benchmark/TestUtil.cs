using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Http.DotNetty.Benchmark
{
    public static class TestUtil
    {
        #region Public 方法

        public static async Task TimedRunAsync(Func<Task> func, string name, int turn = 5)
        {
            var count = 0L;
            for (int i = 0; i < turn; i++)
            {
                var st = Stopwatch.StartNew();
                await func().ConfigureAwait(false);

                st.Stop();

                count += st.ElapsedTicks;

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: Test - {name} -- Turn - {i + 1} Time - {st.Elapsed}");
            }

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: Test - {name} -- Turn Count - {turn} AVG Time - {new TimeSpan(count / turn)}");
        }

        #endregion Public 方法
    }
}