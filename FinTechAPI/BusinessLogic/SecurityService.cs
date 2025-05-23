using FinTechAPI.Models;
using System.Collections.Generic;
using System.Linq;
using FinTechAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly FinTechDbContext _context;

        public SecurityService(FinTechDbContext context)
        {
            _context = context;
        }

        // Simple anomaly detection based on transaction amount threshold
        public IEnumerable<Transaction> DetectAnomalies(decimal threshold)
        {
            return _context.Transactions.Where(t => t.Amount > threshold).ToList();
        }
        
        public async Task<IEnumerable<Transaction>> DetectAnomaliesAsync(decimal threshold)
        {
            return await _context.Transactions
                .Where(t => t.Amount > threshold)
                .ToListAsync();
        }

    }
}