using System.Threading.Tasks;
using LeaderboardService.Controllers;
using LeaderboardService.Models;
using LeaderboardService.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LeaderboardService.Tests
{
    public class LeaderboardControllerTests : IDisposable
    {
        private readonly LeaderboardManager _leaderboardManager;
        private readonly LeaderboardController _controller;

        public LeaderboardControllerTests()
        {
            _leaderboardManager = new LeaderboardManager();
            _controller = new LeaderboardController(_leaderboardManager);
        }

        public void Dispose()
        {
            // Clean up is handled by GC since we're not using any unmanaged resources
        }

        [Fact]
        public void UpdateScore_ValidInput_ReturnsOkWithCorrectScore()
        {
            // Act
            var result = _controller.UpdateScore(1, 100);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var score = Assert.IsType<decimal>(okResult.Value);
            Assert.Equal(100m, score);
        }

        [Theory]
        [InlineData(-1001)]
        [InlineData(1001)]
        public void UpdateScore_InvalidInput_ReturnsBadRequestWithMessage(decimal invalidScore)
        {
            // Act
            var result = _controller.UpdateScore(1, invalidScore);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Score change must be between -1000 and 1000", badRequestResult.Value);
        }

        [Fact]
        public async Task GetLeaderboard_NoParameters_ReturnsAllCustomers()
        {
            // Arrange
            await _leaderboardManager.UpdateScore(1, 100);
            await _leaderboardManager.UpdateScore(2, 200);

            // Act
            var result = _controller.GetLeaderboard(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var customers = Assert.IsType<List<Customer>>(okResult.Value);
            Assert.Equal(2, customers.Count);
            Assert.Equal(2, customers[0].CustomerId);
            Assert.Equal(1, customers[1].CustomerId);
        }

        [Fact]
        public async Task GetLeaderboard_WithRange_ReturnsCorrectRange()
        {
            // Arrange
            await _leaderboardManager.UpdateScore(1, 100);
            await _leaderboardManager.UpdateScore(2, 200);
            await _leaderboardManager.UpdateScore(3, 150);

            // Act
            var result = _controller.GetLeaderboard(2, 3);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var customers = Assert.IsType<List<Customer>>(okResult.Value);
            Assert.Equal(2, customers.Count);
            Assert.Equal(3, customers[0].CustomerId); // Rank 2
            Assert.Equal(1, customers[1].CustomerId); // Rank 3
        }

        [Fact]
        public async Task GetLeaderboard_EmptyLeaderboard_ReturnsEmptyList()
        {
            // Act
            var result = await _controller.GetLeaderboard(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var customers = Assert.IsType<List<Customer>>(okResult.Value);
            Assert.Empty(customers);
        }

        [Fact]
        public async Task GetCustomerWithNeighbors_ExistingCustomer_ReturnsCorrectNeighbors()
        {
            // Arrange
            await _leaderboardManager.UpdateScore(1, 100);
            await _leaderboardManager.UpdateScore(2, 200);
            await _leaderboardManager.UpdateScore(3, 150);

            // Act
            var result = _controller.GetCustomerWithNeighbors(2, 1, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var customers = Assert.IsType<List<Customer>>(okResult.Value);
            Assert.Equal(2, customers[0].CustomerId); // Target
            Assert.Equal(3, customers[1].CustomerId); // Lower neighbor
        }

        [Fact]
        public async Task GetCustomerWithNeighbors_NonExistentCustomer_ReturnsEmptyList()
        {
            // Arrange
            await _leaderboardManager.UpdateScore(1, 100);

            // Act
            var result = _controller.GetCustomerWithNeighbors(999);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var customers = Assert.IsType<List<Customer>>(okResult.Value);
            Assert.Empty(customers);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(2, 2)]
        public async Task GetCustomerWithNeighbors_DifferentRanges_ReturnsCorrectCount(int high, int low)
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                await _leaderboardManager.UpdateScore(i, i * 100);
            }

            // Act
            var result = _controller.GetCustomerWithNeighbors(3, high, low);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var customers = Assert.IsType<List<Customer>>(okResult.Value);
            var expectedCount = Math.Min(1 + high + low, 5); // 5 is total number of customers
            Assert.Equal(expectedCount, customers.Count);
        }

        [Fact]
        public async Task GetCustomerWithNeighbors_HighLowExceedsBounds_ReturnsAllAvailable()
        {
            // Arrange
            await _leaderboardManager.UpdateScore(1, 100);
            await _leaderboardManager.UpdateScore(2, 200);
            await _leaderboardManager.UpdateScore(3, 300);

            // Act
            var result = _controller.GetCustomerWithNeighbors(2, 10, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var customers = Assert.IsType<List<Customer>>(okResult.Value);
            Assert.Equal(3, customers.Count); // Should return all customers
        }
    }
} 