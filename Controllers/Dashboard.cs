using Expense_tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Expense_tracker.Controllers
{
    public class Dashboard : Controller
    {
        private readonly ApplicationDbContext _context;

        public Dashboard(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ActionResult> Index()
        {
            DateTime startDate = DateTime.Today.AddDays(-6);
            DateTime endDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions.Include(x => x.Category).Where(y => y.Date >= startDate && y.Date <= endDate).ToListAsync();

            int TotalIncome = SelectedTransactions.Where(i => i.Category.Type == "Income").Sum(i => i.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            int TotalExpense = SelectedTransactions.Where(i => i.Category.Type == "Expense").Sum(i => i.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            int Balance = TotalExpense - TotalIncome;
            CultureInfo culture= CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            ViewBag.DougnutChartData = SelectedTransactions.Where(i=>i.Category.Type == "Expense").GroupBy(j=>j.Category.CategoryId).Select(k => new
            {
                categoryTitleWithIcon = k.First().Category.Icon+" "+k.First().Category.Title,
                amount = k.Sum(j=>j.Amount),
                formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
            }).OrderByDescending(i=>i.amount).ToList();

            List<SplineChartData> IncomeSummary = SelectedTransactions.Where(i => i.Category.Type == "Income").GroupBy(i => i.Date).Select(i => new SplineChartData()
            {
                day = i.First().Date.ToString("dd-MMM"),
                income = i.Sum(l => l.Amount)
            }).ToList();

            List<SplineChartData> ExpenseSummary = SelectedTransactions.Where(i => i.Category.Type == "Expense").GroupBy(i => i.Date).Select(i => new SplineChartData()
            {
                day = i.First().Date.ToString("dd-MMM"),
                expense = i.Sum(l => l.Amount)
            }).ToList();

            string[] last7days = Enumerable.Range(0, 7).Select(i => startDate.AddDays(i).ToString("dd-MMM")).ToArray();

            ViewBag.SplineChartData = from day in last7days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,

                                      };

            ViewBag.RecentTransactions = await _context.Transactions.Include(i=>i.Category).OrderByDescending(j=> j.Date).Take(5).ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
