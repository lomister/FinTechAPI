using FinTechAPI.Models;

namespace FinTechAPI.Services;

public interface IReportingService
{
    Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(TransactionType transactionType, string userId);

    Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);

    public decimal CalculateTotalAmount(IEnumerable<Transaction> transactions);

}