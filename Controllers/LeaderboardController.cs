using Microsoft.AspNetCore.Mvc;
using LeaderboardService.Services;
using LeaderboardService.Models;

namespace LeaderboardService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardManager _leaderboardManager;

        public LeaderboardController(LeaderboardManager leaderboardManager)
        {
            _leaderboardManager = leaderboardManager;
        }

        [HttpPost("/customer/{customerId}/score/{score}")]
        public async Task<ActionResult<decimal>> UpdateScore(long customerId, decimal score)
        {
            try
            {
                var newScore = await _leaderboardManager.UpdateScore(customerId, score);
                return Ok(newScore);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/leaderboard")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetLeaderboard([FromQuery] int? start, [FromQuery] int? end)
        {
            var customers = await _leaderboardManager.GetCustomersByRank(start, end);
            return Ok(customers);
        }

        [HttpGet("/leaderboard/{customerId}")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomerWithNeighbors([FromRoute] long customerId, [FromQuery] int high = 0, [FromQuery] int low = 0)
        {
            var customers = await _leaderboardManager.GetCustomerWithNeighbors(customerId, high, low);
            return Ok(customers);
        }
    }
}