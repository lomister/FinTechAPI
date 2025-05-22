using FinTechAPI.Models;
using System.Collections.Generic;
using System.Linq;
using FinTechAPI.Data;

namespace FinTechAPI.Services
{
    public class ReportingService
    {
        private readonly FinTechDbContext _context;

        public ReportingService(FinTechDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Transaction> GetTransactionsByCategory(string category)
        {
            return _context.Transactions.Where(t => t.Category == category).ToList();
        }

        public IEnumerable<Transaction> GetTransactionsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _context.Transactions
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public decimal CalculateTotalAmount(IEnumerable<Transaction> transactions)
        {
            return transactions.Sum(t => t.Amount);
        }
    }
}