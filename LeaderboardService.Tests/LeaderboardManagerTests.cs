using LeaderboardService.Services;

namespace LeaderboardService.Tests;

public class LeaderboardManagerTests
{
    private readonly LeaderboardManager _manager;

    public LeaderboardManagerTests()
    {
        _manager = new LeaderboardManager();
    }

    [Fact]
    public void UpdateScore_ValidScore_ReturnsUpdatedScore()
    {
        // Arrange
        long customerId = 1;
        decimal scoreChange = 100;

        // Act
        decimal result = _manager.UpdateScore(customerId, scoreChange);

        // Assert
        Assert.Equal(100, result);
    }

    [Theory]
    [InlineData(-1001)]
    [InlineData(1001)]
    public void UpdateScore_InvalidScore_ThrowsArgumentException(decimal scoreChange)
    {
        // Arrange
        long customerId = 1;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _manager.UpdateScore(customerId, scoreChange));
    }

    [Fact]
    public void UpdateScore_MultipleUpdates_AccumulatesScore()
    {
        // Arrange
        long customerId = 1;

        // Act
        _manager.UpdateScore(customerId, 100);
        decimal result = _manager.UpdateScore(customerId, 150);

        // Assert
        Assert.Equal(250, result);
    }

    [Fact]
    public void GetCustomersByRank_EmptyLeaderboard_ReturnsEmptyList()
    {
        // Act
        var result = _manager.GetCustomersByRank();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomersByRank_MultipleCustomers_ReturnsOrderedList()
    {
        // Arrange
        _manager.UpdateScore(1, 100);
        _manager.UpdateScore(2, 200);
        _manager.UpdateScore(3, 150);

        // Act
        var result = _manager.GetCustomersByRank().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].CustomerId); // Highest score (200)
        Assert.Equal(3, result[1].CustomerId); // Second highest (150)
        Assert.Equal(1, result[2].CustomerId); // Lowest score (100)
    }

    [Fact]
    public void GetCustomersByRank_WithRange_ReturnsCorrectRange()
    {
        // Arrange
        _manager.UpdateScore(1, 100);
        _manager.UpdateScore(2, 200);
        _manager.UpdateScore(3, 150);
        _manager.UpdateScore(4, 300);

        // Act
        var result = _manager.GetCustomersByRank(2, 3).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].CustomerId); // Rank 2
        Assert.Equal(3, result[1].CustomerId); // Rank 3
    }

    [Fact]
    public void GetCustomerWithNeighbors_CustomerExists_ReturnsCorrectNeighbors()
    {
        // Arrange
        _manager.UpdateScore(1, 100); // Rank 5
        _manager.UpdateScore(2, 200); // Rank 3
        _manager.UpdateScore(3, 150); // Rank 4
        _manager.UpdateScore(4, 300); // Rank 1
        _manager.UpdateScore(5, 250); // Rank 2

        // Act
        var result = _manager.GetCustomerWithNeighbors(2, 1, 1).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(5, result[0].CustomerId); // One higher rank
        Assert.Equal(2, result[1].CustomerId); // Target customer
        Assert.Equal(3, result[2].CustomerId); // One lower rank
    }

    [Fact]
    public void GetCustomerWithNeighbors_CustomerNotFound_ReturnsEmptyList()
    {
        // Arrange
        _manager.UpdateScore(1, 100);
        _manager.UpdateScore(2, 200);

        // Act
        var result = _manager.GetCustomerWithNeighbors(999, 1, 1);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomerWithNeighbors_AtTopOfLeaderboard_ReturnsCorrectNeighbors()
    {
        // Arrange
        _manager.UpdateScore(1, 300); // Top rank
        _manager.UpdateScore(2, 200);
        _manager.UpdateScore(3, 100);

        // Act
        var result = _manager.GetCustomerWithNeighbors(1, 1, 1).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].CustomerId); // Target customer (top rank)
        Assert.Equal(2, result[1].CustomerId); // One lower rank
    }

    [Fact]
    public void GetCustomerWithNeighbors_AtBottomOfLeaderboard_ReturnsCorrectNeighbors()
    {
        // Arrange
        _manager.UpdateScore(1, 300);
        _manager.UpdateScore(2, 200);
        _manager.UpdateScore(3, 100); // Bottom rank

        // Act
        var result = _manager.GetCustomerWithNeighbors(3, 1, 1).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].CustomerId); // One higher rank
        Assert.Equal(3, result[1].CustomerId); // Target customer (bottom rank)
    }
} 