using System.Collections.Concurrent;
using LeaderboardService.Models;

namespace LeaderboardService.Services
{
    /// <summary>
    /// Manages a leaderboard system for customers, handling score updates and rank calculations.
    /// Thread-safe implementation using ConcurrentDictionary.
    /// </summary>
    public class LeaderboardManager
    {
        private readonly ConcurrentDictionary<long, Customer> _customers;

        /// <summary>
        /// Initializes a new instance of the LeaderboardManager class.
        /// </summary>
        public LeaderboardManager()
        {
            _customers = new ConcurrentDictionary<long, Customer>();
        }

        /// <summary>
        /// Updates a customer's score and recalculates rankings.
        /// </summary>
        /// <param name="customerId">The unique identifier of the customer.</param>
        /// <param name="scoreChange">The amount to change the score by (between -1000 and 1000).</param>
        /// <returns>The customer's new total score after the update.</returns>
        /// <exception cref="ArgumentException">Thrown when score change is outside the valid range of -1000 to 1000.</exception>
        public async Task<decimal> UpdateScore(long customerId, decimal scoreChange)
        {
            if (scoreChange < -1000 || scoreChange > 1000)
            {
                throw new ArgumentException("Score change must be between -1000 and 1000");
            }

            var t = await Task.Run(() =>
             {
                 var customer = _customers.GetOrAdd(customerId, new Customer { CustomerId = customerId });
                 customer.Score += scoreChange;
                 return Task.FromResult(customer.Score);
             });
            return t;
        }


        /// <summary>
        /// Retrieves a list of customers sorted by their rank within an optional range.
        /// Only includes customers with positive scores.
        /// </summary>
        /// <param name="start">Optional starting rank to filter from (inclusive).</param>
        /// <param name="end">Optional ending rank to filter to (inclusive).</param>
        /// <returns>An ordered collection of customers by rank, optionally filtered by rank range.</returns>
        public async Task<IEnumerable<Customer>> GetCustomersByRank(int? start = null, int? end = null)
        {
            var sortedCustomers = _customers.Values
                .Where(c => c.Score > 0)
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.CustomerId)
                .ToList();

            // Update ranks
            for (int i = 0; i < sortedCustomers.Count; i++)
            {
                sortedCustomers[i].Rank = i + 1;
            }

            if (start.HasValue && end.HasValue)
            {
                return sortedCustomers
                    .Where(c => c.Rank >= start.Value && c.Rank <= end.Value)
                    .ToList();
            }

            return await Task.FromResult(sortedCustomers);
        }


        /// <summary>
        /// Updates the ranks of all customers based on their current scores.
        /// Customers are ranked in descending order by score, with customer ID as a tiebreaker.
        /// Only customers with positive scores are ranked.
        /// </summary>
        //private void UpdateRanks()
        //{
        //    var sortedCustomers = _customers.Values
        //        .Where(c => c.Score > 0)
        //        .OrderByDescending(c => c.Score)
        //        .ThenBy(c => c.CustomerId);

        //    int rank = 1;
        //    foreach (var customer in sortedCustomers)
        //    {
        //        customer.Rank = rank++;
        //    }
        //}

        /// <summary>
        /// Gets a specific customer and their neighboring customers based on rank.
        /// </summary>
        /// <param name="customerId">The ID of the customer to lookup.</param>
        /// <param name="high">Optional number of higher-ranked neighbors to return (default 0).</param>
        /// <param name="low">Optional number of lower-ranked neighbors to return (default 0).</param>
        /// <returns>A list of customers including the target customer and their neighbors within the specified range.</returns>
        /// <remarks>
        /// The result includes:
        /// - Up to 'high' number of customers with ranks higher than the target customer
        /// - The target customer
        /// - Up to 'low' number of customers with ranks lower than the target customer
        /// </remarks>
        public async Task<IEnumerable<Customer>> GetCustomerWithNeighbors(long customerId, int high = 0, int low = 0)
        {
            // Get all customers with their current ranks
            var allCustomers = (await GetCustomersByRank()).ToList();
            
            // Find the target customer
            var targetCustomer = allCustomers.FirstOrDefault(c => c.CustomerId == customerId);
            if (targetCustomer == null)
            {
                return Enumerable.Empty<Customer>();
            }

            // Find the index of the target customer
            int targetIndex = allCustomers.IndexOf(targetCustomer);
            
            // Calculate the range of indexes to return
            int startIndex = Math.Max(targetIndex - high, 0);
            int endIndex = Math.Min(targetIndex + low, allCustomers.Count - 1);
            
            // Return the slice of customers within the range
            return allCustomers.GetRange(startIndex, endIndex - startIndex + 1);
        }
    }
} 