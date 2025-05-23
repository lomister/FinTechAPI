using FinTechAPI.Models;

namespace FinTechAPI.DTOs
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public TransactionType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AccountId { get; set; }
        // Возможно, вы захотите добавить AccountName или другие детали счета
    }
}
