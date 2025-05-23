using FinTechAPI.Models;
using System.Collections.Generic;
using System.Linq;
using FinTechAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Services
{
    public class ReportingService : IReportingService
    {
        private readonly FinTechDbContext _context;

        public ReportingService(FinTechDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(TransactionType transactionType, string userId)
        {
            return await _context.Transactions
                .Where(t => t.Type == transactionType && t.UserId == userId) 
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate) // Используем TransactionDate
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public decimal CalculateTotalAmount(IEnumerable<Transaction> transactions)
        {
            return transactions.Sum(t => t.Amount);
        }

    }
}