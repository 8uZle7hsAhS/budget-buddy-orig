using Budget_Baddie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Budget_Baddie.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private DateTime _startDate;
        private DateTime _endDate;

        // Constructor
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
            _startDate = DateTime.Today.AddDays(-6); // Default to last 7 days
            _endDate = DateTime.Today; // Default to today
        }

        // Ensure user is authenticated before accessing the dashboard
        public async Task<IActionResult> Index()
        {
            // Fetch all users from the database
            var users = await _context.Users.OrderByDescending(u => u.UserId).ToListAsync();

            // Ensure default date range values are set correctly
            ViewBag.StartDate = TempData["StartDate"] != null ? (DateTime)TempData["StartDate"] : _startDate;
            ViewBag.EndDate = TempData["EndDate"] != null ? (DateTime)TempData["EndDate"] : _endDate;

            DateTime startDate = ViewBag.StartDate;
            DateTime endDate = ViewBag.EndDate;

            // Fetch all transactions across all users within the date range
            var selectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= startDate && y.Date <= endDate)
                .ToListAsync();

            // Total Income for all users
            int totalIncome = selectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = totalIncome.ToString("C0");

            // Total Expense for all users
            int totalExpense = selectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = totalExpense.ToString("C0");

            // Total number of users
            ViewBag.TotalUsers = users.Count;

            // Get the latest registered user
            var latestUser = users.FirstOrDefault();
            if (latestUser != null)
            {
                ViewBag.LatestUserId = latestUser.UserId; // Store the latest UserId
            }

            // Doughnut Chart Data for all users
            ViewBag.DoughnutChartData = selectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // Spline Chart Data for Income vs Expense
            var splineChartData = selectedTransactions
                .GroupBy(t => t.Date)
                .Select(g => new
                {
                    day = g.Key.ToString("dd-MMM"),
                    income = g.Where(t => t.Category.Type == "Income").Sum(t => t.Amount),
                    expense = g.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount)
                })
                .OrderBy(d => DateTime.ParseExact(d.day, "dd-MMM", CultureInfo.InvariantCulture))
                .ToList();

            ViewBag.SplineChartData = splineChartData;

            // Spline Chart Data for Income Summary
            var incomeSummary = selectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            // Spline Chart Data for Expense Summary
            var expenseSummary = selectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            ViewBag.IncomeSummary = incomeSummary;
            ViewBag.ExpenseSummary = expenseSummary;

            return View();
        }

        // Helper method to get the UserId from session
        private int GetUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }
    }
}
