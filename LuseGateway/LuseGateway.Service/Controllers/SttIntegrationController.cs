using Microsoft.AspNetCore.Mvc;
using LuseGateway.Core.Services;
using LuseGateway.Core.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LuseGateway.Service.Controllers
{
    [ApiController]
    [Route("api/integration")]
    public class SttIntegrationController : ControllerBase
    {
        private readonly ISttApiClient _sttApiClient;
        private readonly ILogger<SttIntegrationController> _logger;

        public SttIntegrationController(ISttApiClient sttApiClient, ILogger<SttIntegrationController> logger)
        {
            _sttApiClient = sttApiClient;
            _logger = logger;
        }

        [HttpGet("server-time")]
        public async Task<IActionResult> GetServerTime()
        {
            try
            {
                var time = await _sttApiClient.GetServerTimeAsync();
                return Ok(new { ServerTime = time });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("market-prices")]
        public async Task<ActionResult<IEnumerable<SttMarketPrice>>> GetMarketPrices()
        {
            try
            {
                var prices = await _sttApiClient.GetMarketPricesAsync();
                return Ok(prices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("turnover-volume")]
        public async Task<ActionResult<IEnumerable<SttTurnoverVolumeDeal>>> GetTurnoverVolumeDeals()
        {
            try
            {
                var data = await _sttApiClient.GetTurnoverVolumeDealsAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("market-caps")]
        public async Task<ActionResult<IEnumerable<SttMarketCap>>> GetMarketCaps()
        {
            try
            {
                var data = await _sttApiClient.GetMarketCapsAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("trade-daily-summary")]
        public async Task<ActionResult<IEnumerable<SttTradeDailySummary>>> GetTradeDailySummary()
        {
            try
            {
                var data = await _sttApiClient.GetTradeDailySummariesAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("order-status/{orderId}")]
        public async Task<ActionResult<SttOrderStatus>> GetOrderStatus(int orderId)
        {
            try
            {
                var status = await _sttApiClient.GetOrderStatusAsync(orderId);
                if (status == null)
                    return NotFound($"Order {orderId} not found in STT");
                    
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpPost("authenticate-test")]
        public async Task<IActionResult> TestAuthentication([FromQuery] string? username, [FromQuery] string? password)
        {
             // Note: This exposes creds in query params which is not safe for prod but okay for local debugging
             // However, SttApiClient caches the token, so we can just call AuthenticateAsync
             try
             {
                 if (string.IsNullOrEmpty(username)) 
                 {
                     // Use configured defaults if not provided
                     // We can't easily access the configured username/password here without injecting IConfiguration
                     // so we'll just try to force auth if we can, or just ping.
                     // Actually, ISttApiClient.AuthenticateAsync takes args.
                     return BadRequest("Username and Password are required for this manual test");
                 }

                 var result = await _sttApiClient.AuthenticateAsync(username, password);
                 return Ok(result);
             }
             catch (Exception ex)
             {
                 return StatusCode(500, new { Error = ex.Message });
             }
        }
    }
}
