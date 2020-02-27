using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace System.Net.Http.DotNetty.TestServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EchoController : ControllerBase
    {
        #region Public 方法

        [HttpGet]
        [HttpPost]
        [HttpDelete]
        [HttpHead]
        [HttpOptions]
        [HttpPatch]
        [HttpPut]
        public async Task<ActionResult<string>> Echo(int delay = 0)
        {
            if (delay > 0)
            {
                await Task.Delay(delay);
            }
            return Ok(Request.Body);
        }

        #endregion Public 方法
    }
}