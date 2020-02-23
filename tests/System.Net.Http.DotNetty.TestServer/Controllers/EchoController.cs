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
        public ActionResult<string> Echo()
        {
            return Ok(Request.Body);
        }

        #endregion Public 方法
    }
}