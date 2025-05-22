using FinTechAPI.Models;
using System.Collections.Generic;
using System.Linq;
using FinTechAPI.Data;

namespace FinTechAPI.Services
{
    public class SecurityService
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
    }
}