// Services/TransactionService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Budget_Baddie.Models;

namespace Budget_Baddie.Services
{
    public class TransactionService
    {
        private readonly List<Transaction> _transactions = new()
        {
            new Transaction
            {
                TransactionId = 1,
                Amount = 100,
                Date = DateTime.Now.AddDays(-2),
                Category = new Category { Title = "Food", Icon = "🍔", Type = "Expense" }
            },
            new Transaction
            {
                TransactionId = 2,
                Amount = 250,
                Date = DateTime.Now.AddDays(-1),
                Category = new Category { Title = "Salary", Icon = "💼", Type = "Income" }
            },
            // Add more sample transactions as needed
        };

        public Task<List<Transaction>> GetTransactionsAsync(string sortBy)
        {
            var sortedTransactions = sortBy.ToLower() switch
            {
                "amount" => _transactions.OrderBy(t => t.Amount).ToList(),
                "date" => _transactions.OrderBy(t => t.Date).ToList(),
                "category" => _transactions.OrderBy(t => t.Category?.Title).ToList(),
                _ => _transactions
            };

            return Task.FromResult(sortedTransactions);
        }
    }
}