using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace XlentApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // test endpoint to verify that the API is running som returer en enkel JSON object
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "API is running!" });
        }
    }
}
