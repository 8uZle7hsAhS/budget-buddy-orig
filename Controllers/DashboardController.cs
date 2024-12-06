using Budget_Baddie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Budget_Baddie.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private DateTime _startDate;
        private DateTime _endDate;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
            _startDate = DateTime.Today.AddDays(-6); // Default to last 7 days
            _endDate = DateTime.Today; // Default to today
        }

        private int GetUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // Ensure user is authenticated before accessing the dashboard
        public async Task<ActionResult> Index()
        {
            // Fetch the UserId from the session
            int userId = GetUserId();

            // If UserId is not set, redirect to login page
            if (userId == 0)
            {
                return RedirectToAction("Index", "Log");
            }

            // Fetch the user from the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                // Handle case where user is not found (e.g., session mismatch or deleted user)
                return RedirectToAction("Index", "Log");
            }

            // Ensure default date range values are set correctly
            ViewBag.StartDate = TempData["StartDate"] != null ? (DateTime)TempData["StartDate"] : _startDate;
            ViewBag.EndDate = TempData["EndDate"] != null ? (DateTime)TempData["EndDate"] : _endDate;

            DateTime startDate = ViewBag.StartDate;
            DateTime endDate = ViewBag.EndDate;

            // Fetch user-specific transactions within the date range
            var selectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.UserId == userId && y.Date >= startDate && y.Date <= endDate)
                .ToListAsync();

            // Total Income
            int totalIncome = selectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = totalIncome.ToString("C0");

            // Total Expense
            int totalExpense = selectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = totalExpense.ToString("C0");

            // Calculate Balance
            int balance = totalIncome - totalExpense;
            var culture = CultureInfo.CreateSpecificCulture("fil-PH");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", balance);

            // Doughnut Chart Data
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

            // Spline Chart Data
            var incomeSummary = selectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            var expenseSummary = selectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            // Populate the date range for the Spline Chart
            var dateList = Enumerable.Range(0, (endDate - startDate).Days + 1)
                .Select(i => startDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.DashboardSplineChartData = from day in dateList
                                      join income in incomeSummary on day equals income.day into incomeJoin
                                      from income in incomeJoin.DefaultIfEmpty()
                                      join expense in expenseSummary on day equals expense.day into expenseJoin
                                      from expense in expenseJoin.DefaultIfEmpty()
                                      select new
                                      {
                                          day,
                                          income = income?.income ?? 0,
                                          expense = expense?.expense ?? 0,
                                      };

            // Fetch user-specific recent transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .Where(t => t.UserId == userId)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // Handle date range filtering for dashboard
        public async Task<ActionResult> FilterDates(DateTime? date_start, DateTime? date_end)
        {
            if (date_start.HasValue && date_end.HasValue)
            {
                if (date_start.Value > date_end.Value)
                {
                    ModelState.AddModelError("", "Start date cannot be greater than end date.");
                }
                else if (date_start.Value > DateTime.Now || date_end.Value > DateTime.Now)
                {
                    ModelState.AddModelError("", "Dates cannot be in the future.");
                }
                else
                {
                    // Set new date range
                    _startDate = date_start.Value;
                    _endDate = date_end.Value;

                    TempData["StartDate"] = _startDate;
                    TempData["EndDate"] = _endDate;
                }
            }

            return RedirectToAction("Index");
        }
    }

    // Helper class for chart data
    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
