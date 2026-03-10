using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using XlentApi.Services;

namespace XlentApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly AzureOpenAiService aiService;

        public TestController(AzureOpenAiService aiService)
        {
            this.aiService = aiService;
        }
        // test endpoint to verify that the API is running som returer en enkel JSON object
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            await aiService.TestApi();
            return Ok(new { message = "API is running!" });
        }
    }
}
