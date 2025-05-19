using System.Threading.Tasks;
using LeaderboardService.Services;
using Xunit;

namespace LeaderboardService.Tests
{
    public class LeaderboardManagerTests
    {
        private readonly LeaderboardManager _manager;

        public LeaderboardManagerTests()
        {
            _manager = new LeaderboardManager();
        }

        [Fact]
        public async Task UpdateScore_NewCustomer_CreatesAndUpdatesScore()
        {
            // Act
            var score = await _manager.UpdateScore(1, 100);

            // Assert
            Assert.Equal(100m, score);
            var customers =await _manager.GetCustomersByRank();
            var customer = Assert.Single(customers.ToList());
            Assert.Equal(1, customer.CustomerId);
            Assert.Equal(100m, customer.Score);
            Assert.Equal(1, customer.Rank);
        }

        [Fact]
        public async Task UpdateScore_ExistingCustomer_UpdatesScore()
        {
            // Arrange
            await _manager.UpdateScore(1, 100);

            // Act
            var score = await _manager.UpdateScore(1, 50);

            // Assert
            Assert.Equal(150m, score);
            var customers = await _manager.GetCustomersByRank();
            var customer = Assert.Single(customers.ToList());
            Assert.Equal(150m, customer.Score);
        }

        [Theory]
        [InlineData(-1001)]
        [InlineData(1001)]
        public async Task UpdateScore_InvalidScore_ThrowsArgumentException(decimal invalidScore)
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () => 
                 await _manager.UpdateScore(1, invalidScore));
            Assert.Equal("Score change must be between -1000 and 1000", exception.Message);
        }

        [Fact]
        public async Task UpdateScore_NegativeScoreBelowZero_RemovesFromLeaderboard()
        {
            // Arrange
           await _manager.UpdateScore(1, 100);

            // Act
           await _manager.UpdateScore(1, -150);

            // Assert
            var customers = await _manager.GetCustomersByRank();
            Assert.Empty(customers);
        }

        [Fact]
        public async Task GetCustomersByRank_MultipleCustomers_CorrectOrder()
        {
            // Arrange
           await _manager.UpdateScore(1, 100);
           await _manager.UpdateScore(2, 200);
           await _manager.UpdateScore(3, 150);

            // Act
            var customers = await _manager.GetCustomersByRank();
            // Assert
            Assert.Equal(3, customers.ToList().Count);
            Assert.Equal(2, customers.ToList()[0].CustomerId);
            Assert.Equal(1, customers.ToList()[0].Rank);
            Assert.Equal(3, customers.ToList()[1].CustomerId);
            Assert.Equal(2, customers.ToList()[1].Rank);
            Assert.Equal(1, customers.ToList()[2].CustomerId);
            Assert.Equal(3, customers.ToList()[2].Rank);
        }

        [Fact]
        public async Task GetCustomersByRank_WithRange_ReturnsCorrectRange()
        {
            // Arrange
            await _manager.UpdateScore(1, 100);
            await _manager.UpdateScore(2, 200);
            await _manager.UpdateScore(3, 150);
            await _manager.UpdateScore(4, 175);

            // Act
            var customers = await _manager.GetCustomersByRank(2, 3);

            // Assert
            Assert.Equal(2, customers.ToList().Count);
            Assert.Equal(4, customers.ToList()[0].CustomerId);
            Assert.Equal(2, customers.ToList()[0].Rank);
            Assert.Equal(3, customers.ToList()[1].CustomerId);
            Assert.Equal(3, customers.ToList()[1].Rank);
        }

        [Fact]
        public async Task GetCustomersByRank_EmptyLeaderboard_ReturnsEmptyList()
        {
            // Act
            var customers = await _manager.GetCustomersByRank();

            // Assert
            Assert.Empty(customers);
        }

        [Fact]
        public async Task GetCustomerWithNeighbors_ExistingCustomer_ReturnsCorrectNeighbors()
        {
            // Arrange
            await _manager.UpdateScore(1, 100);
            await _manager.UpdateScore(2, 200);
            await _manager.UpdateScore(3, 150);
            await _manager.UpdateScore(4, 175);

            // Act
            var customers = (await _manager.GetCustomerWithNeighbors(3, 1, 1)).ToList();

            // Assert
            Assert.Equal(3, customers.Count);
            Assert.Equal(4, customers[0].CustomerId); // Higher neighbor
            Assert.Equal(3, customers[1].CustomerId); // Target
            Assert.Equal(1, customers[2].CustomerId); // Lower neighbor
        }

        [Fact]
        public async Task GetCustomerWithNeighbors_CustomerAtTop_ReturnsCorrectNeighbors()
        {
            // Arrange
            await _manager.UpdateScore(1, 100);
            await _manager.UpdateScore(2, 200);
            await _manager.UpdateScore(3, 150);

            // Act
            var customers = (await _manager.GetCustomerWithNeighbors(2, 1, 1)).ToList();

            // Assert
            Assert.Equal(2, customers.Count); // No higher neighbors possible
            Assert.Equal(2, customers[0].CustomerId);
            Assert.Equal(3, customers[1].CustomerId);
        }

        [Fact]
        public async Task GetCustomerWithNeighbors_CustomerAtBottom_ReturnsCorrectNeighbors()
        {
            // Arrange
            await _manager.UpdateScore(1, 100);
            await _manager.UpdateScore(2, 200);
            await _manager.UpdateScore(3, 150);

            // Act
            var customers = (await _manager.GetCustomerWithNeighbors(1, 1, 1)).ToList();

            // Assert
            Assert.Equal(2, customers.Count); // No lower neighbors possible
            Assert.Equal(3, customers[0].CustomerId);
            Assert.Equal(1, customers[1].CustomerId);
        }

        [Fact]
        public async Task GetCustomerWithNeighbors_NonExistentCustomer_ReturnsEmptyList()
        {
            // Arrange
            await _manager.UpdateScore(1, 100);
            await _manager.UpdateScore(2, 200);

            // Act
            var customers = (await _manager.GetCustomerWithNeighbors(999, 1, 1)).ToList();

            // Assert
            Assert.Empty(customers);
        }

        [Fact]
        public async Task UpdateScore_ConcurrentUpdates_ThreadSafe()
        {
            // Arrange
            var tasks = new List<Task>();
            var customerId = 1L;
            var iterations = 100;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(() => _manager.UpdateScore(customerId, 10)));
            }
            Task.WaitAll(tasks.ToArray());

            // Assert
            var customer = (await _manager.GetCustomersByRank()).Single();
            Assert.Equal(iterations * 10, customer.Score);
        }
    }
} 