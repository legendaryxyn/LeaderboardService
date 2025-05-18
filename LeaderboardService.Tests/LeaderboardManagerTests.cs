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
        public void UpdateScore_NewCustomer_CreatesAndUpdatesScore()
        {
            // Act
            var score = _manager.UpdateScore(1, 100);

            // Assert
            Assert.Equal(100m, score);
            var customers = _manager.GetCustomersByRank();
            var customer = Assert.Single(customers);
            Assert.Equal(1, customer.CustomerId);
            Assert.Equal(100m, customer.Score);
            Assert.Equal(1, customer.Rank);
        }

        [Fact]
        public void UpdateScore_ExistingCustomer_UpdatesScore()
        {
            // Arrange
            _manager.UpdateScore(1, 100);

            // Act
            var score = _manager.UpdateScore(1, 50);

            // Assert
            Assert.Equal(150m, score);
            var customers = _manager.GetCustomersByRank();
            var customer = Assert.Single(customers);
            Assert.Equal(150m, customer.Score);
        }

        [Theory]
        [InlineData(-1001)]
        [InlineData(1001)]
        public void UpdateScore_InvalidScore_ThrowsArgumentException(decimal invalidScore)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                _manager.UpdateScore(1, invalidScore));
            Assert.Equal("Score change must be between -1000 and 1000", exception.Message);
        }

        [Fact]
        public void UpdateScore_NegativeScoreBelowZero_RemovesFromLeaderboard()
        {
            // Arrange
            _manager.UpdateScore(1, 100);

            // Act
            _manager.UpdateScore(1, -150);

            // Assert
            var customers = _manager.GetCustomersByRank();
            Assert.Empty(customers);
        }

        [Fact]
        public void GetCustomersByRank_MultipleCustomers_CorrectOrder()
        {
            // Arrange
            _manager.UpdateScore(1, 100);
            _manager.UpdateScore(2, 200);
            _manager.UpdateScore(3, 150);

            // Act
            var customers = _manager.GetCustomersByRank().ToList();

            // Assert
            Assert.Equal(3, customers.Count);
            Assert.Equal(2, customers[0].CustomerId);
            Assert.Equal(1, customers[0].Rank);
            Assert.Equal(3, customers[1].CustomerId);
            Assert.Equal(2, customers[1].Rank);
            Assert.Equal(1, customers[2].CustomerId);
            Assert.Equal(3, customers[2].Rank);
        }

        [Fact]
        public void GetCustomersByRank_WithRange_ReturnsCorrectRange()
        {
            // Arrange
            _manager.UpdateScore(1, 100);
            _manager.UpdateScore(2, 200);
            _manager.UpdateScore(3, 150);
            _manager.UpdateScore(4, 175);

            // Act
            var customers = _manager.GetCustomersByRank(2, 3).ToList();

            // Assert
            Assert.Equal(2, customers.Count);
            Assert.Equal(4, customers[0].CustomerId);
            Assert.Equal(2, customers[0].Rank);
            Assert.Equal(3, customers[1].CustomerId);
            Assert.Equal(3, customers[1].Rank);
        }

        [Fact]
        public void GetCustomersByRank_EmptyLeaderboard_ReturnsEmptyList()
        {
            // Act
            var customers = _manager.GetCustomersByRank();

            // Assert
            Assert.Empty(customers);
        }

        [Fact]
        public void GetCustomerWithNeighbors_ExistingCustomer_ReturnsCorrectNeighbors()
        {
            // Arrange
            _manager.UpdateScore(1, 100);
            _manager.UpdateScore(2, 200);
            _manager.UpdateScore(3, 150);
            _manager.UpdateScore(4, 175);

            // Act
            var customers = _manager.GetCustomerWithNeighbors(3, 1, 1).ToList();

            // Assert
            Assert.Equal(3, customers.Count);
            Assert.Equal(4, customers[0].CustomerId); // Higher neighbor
            Assert.Equal(3, customers[1].CustomerId); // Target
            Assert.Equal(1, customers[2].CustomerId); // Lower neighbor
        }

        [Fact]
        public void GetCustomerWithNeighbors_CustomerAtTop_ReturnsCorrectNeighbors()
        {
            // Arrange
            _manager.UpdateScore(1, 100);
            _manager.UpdateScore(2, 200);
            _manager.UpdateScore(3, 150);

            // Act
            var customers = _manager.GetCustomerWithNeighbors(2, 1, 1).ToList();

            // Assert
            Assert.Equal(2, customers.Count); // No higher neighbors possible
            Assert.Equal(2, customers[0].CustomerId);
            Assert.Equal(3, customers[1].CustomerId);
        }

        [Fact]
        public void GetCustomerWithNeighbors_CustomerAtBottom_ReturnsCorrectNeighbors()
        {
            // Arrange
            _manager.UpdateScore(1, 100);
            _manager.UpdateScore(2, 200);
            _manager.UpdateScore(3, 150);

            // Act
            var customers = _manager.GetCustomerWithNeighbors(1, 1, 1).ToList();

            // Assert
            Assert.Equal(2, customers.Count); // No lower neighbors possible
            Assert.Equal(3, customers[0].CustomerId);
            Assert.Equal(1, customers[1].CustomerId);
        }

        [Fact]
        public void GetCustomerWithNeighbors_NonExistentCustomer_ReturnsEmptyList()
        {
            // Arrange
            _manager.UpdateScore(1, 100);
            _manager.UpdateScore(2, 200);

            // Act
            var customers = _manager.GetCustomerWithNeighbors(999, 1, 1);

            // Assert
            Assert.Empty(customers);
        }

        [Fact]
        public void UpdateScore_ConcurrentUpdates_ThreadSafe()
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
            var customer = _manager.GetCustomersByRank().Single();
            Assert.Equal(iterations * 10, customer.Score);
        }
    }
} 