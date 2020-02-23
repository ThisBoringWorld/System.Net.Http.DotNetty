using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StatisticsServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestDetailController : ControllerBase
    {
        #region 方法

        [HttpGet]
        [HttpPost]
        [HttpDelete]
        [HttpHead]
        [HttpOptions]
        [HttpPatch]
        [HttpPut]
        public async Task<object> Detail()
        {
            await Task.CompletedTask;
            var IP = HttpContext.Request.HttpContext.Connection.RemoteIpAddress.ToString();
            var Headers = Request.Headers.ToDictionary(m => m.Key, m => m.Value.ToString());
            var Cookies = string.Join(" ", Request.Cookies?.Select(m => $"{m.Key}={m.Value}"));
            var Form = Request.HasFormContentType ? string.Join("&", Request.Form?.Select(m => $"{m.Key}={m.Value}")) : string.Empty;
            return new
            {
                IP,
                Headers,
                Cookies,
                Form,
            };
        }

        #endregion 方法
    }
}