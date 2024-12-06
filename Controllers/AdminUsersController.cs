using Budget_Baddie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Budget_Baddie.Controllers
{
    public class AdminUserController : Controller
    {
        public class UserSummary
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public decimal Expense { get; set; }
            public decimal Income { get; set; }
            public string TitleWithIcon { get; set; }  // Changed to TitleWithIcon
        }

        private readonly ApplicationDbContext _context;
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;

        public AdminUserController(ApplicationDbContext context)
        {
            _context = context;
            _startDate = DateTime.Today.AddDays(-6); // Default to last 7 days
            _endDate = DateTime.Today; // Default to today
        }

        public async Task<IActionResult> Index()
        {
            // Fetch users
            var users = await _context.Users
                .OrderByDescending(u => u.UserId)
                .ToListAsync();

            // Default date range
            DateTime startDate = _startDate;
            DateTime endDate = _endDate;

            // Fetch all transactions within the date range
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .ToListAsync();

            // Prepare data for doughnut chart grouped by user
            var userChartData = transactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.UserId)
                .ToDictionary(
                    g => g.Key, // UserId
                    g => g.GroupBy(t => t.Category.CategoryId) // Group by category
                          .Select(k => new
                          {
                              categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                              amount = k.Sum(t => t.Amount),
                              formattedAmount = k.Sum(t => t.Amount).ToString("C0")
                          })
                          .OrderByDescending(k => k.amount)
                          .ToList()
                );

            // Prepare transaction data grouped by user for user summary
            var userTransactionData = transactions
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Expense = g.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount),
                    Income = g.Where(t => t.Category.Type == "Income").Sum(t => t.Amount),
                })
                .ToList();

            // Prepare palette colors
            ViewBag.PaletteColors = new string[]
            {
        "#0e8d76", "#a4b219", "#cb9b00", "#8a442c",
        "#0454b5", "#7d0a0a", "#822690", "#4c2090",
        "#313e93", "#0096ac"
            };

            // Prepare data for each user
            var userSummaries = users.Select(user => new UserSummary
            {
                UserId = user.UserId,
                Username = user.Username,
                Income = userTransactionData.FirstOrDefault(ut => ut.UserId == user.UserId)?.Income ?? 0,
                Expense = userTransactionData.FirstOrDefault(ut => ut.UserId == user.UserId)?.Expense ?? 0,
                TitleWithIcon = user.Username // Can be customized further
            }).OrderBy(u => u.UserId).ToList();

            // Pass data to the view
            ViewBag.UserSummaries = userSummaries;
            ViewBag.DoughnutChartData = userChartData;

            return View();
        }
    }
}