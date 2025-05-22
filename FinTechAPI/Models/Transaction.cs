// Transaction.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace FinTechAPI.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        [Required]
        public string Category { get; set; } // Income, Expense, Transfer, etc.
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign keys
        public int AccountId { get; set; }
        public string UserId { get; set; }
        
        // Navigation properties
        public virtual Account Account { get; set; }
        public virtual User User { get; set; }
    }
}