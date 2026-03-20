using ApplicationInsights.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationInsights.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("success")]
        public IActionResult Success()
        {
            _logger.LogInformation("Success endpoint hit.");

            return Ok(new ApiMessageResponse
            {
                Message = "Request completed successfully.",
                StatusCode = 200,
                Data = new { Result = "OK" }
            });
        }

        [HttpPost("echo")]
        public IActionResult Echo([FromBody] SampleRequest request)
        {
            _logger.LogInformation("Echo endpoint hit with payload: {@Request}", request);

            return Ok(new ApiMessageResponse
            {
                Message = "Payload received successfully.",
                StatusCode = 200,
                Data = request
            });
        }

        [HttpPost("badrequest")]
        public IActionResult BadRequestTest([FromBody] SampleRequest request)
        {
            _logger.LogWarning("BadRequest endpoint hit.");

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ApiMessageResponse
                {
                    Message = "Name is required.",
                    StatusCode = 400
                });
            }

            return BadRequest(new ApiMessageResponse
            {
                Message = "Forced bad request for testing.",
                StatusCode = 400,
                Data = request
            });
        }

        [HttpGet("unauthorized")]
        public IActionResult UnauthorizedTest()
        {
            _logger.LogWarning("Unauthorized endpoint hit.");

            return Unauthorized(new ApiMessageResponse
            {
                Message = "Unauthorized test response.",
                StatusCode = 401
            });
        }

        [HttpGet("forbidden")]
        public IActionResult ForbiddenTest()
        {
            _logger.LogWarning("Forbidden endpoint hit.");

            return StatusCode(403, new ApiMessageResponse
            {
                Message = "Forbidden test response.",
                StatusCode = 403
            });
        }

        [HttpGet("notfound")]
        public IActionResult NotFoundTest()
        {
            _logger.LogWarning("NotFound endpoint hit.");

            return NotFound(new ApiMessageResponse
            {
                Message = "Resource not found.",
                StatusCode = 404
            });
        }

        [HttpGet("servererror")]
        public IActionResult ServerErrorTest()
        {
            _logger.LogError("ServerError endpoint hit. Throwing exception intentionally.");
            throw new InvalidOperationException("Intentional exception for testing.");
        }

        [HttpGet("timeout/{ms:int}")]
        public async Task<IActionResult> TimeoutTest(int ms)
        {
            _logger.LogInformation("Timeout endpoint hit with {Delay} ms", ms);

            await Task.Delay(ms);

            return Ok(new ApiMessageResponse
            {
                Message = $"Response delayed by {ms} ms.",
                StatusCode = 200
            });
        }

        [HttpGet("custom/{statusCode:int}")]
        public IActionResult CustomStatusTest(int statusCode)
        {
            _logger.LogInformation("Custom status endpoint hit with status code {StatusCode}", statusCode);

            return StatusCode(statusCode, new ApiMessageResponse
            {
                Message = $"Custom response for status code {statusCode}",
                StatusCode = statusCode
            });
        }
    }
}