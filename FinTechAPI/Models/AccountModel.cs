// Account.cs
using System;
using System.Collections.Generic;

namespace FinTechAPI.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AccountType { get; set; } // Checking, Savings, Investment, etc.
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign keys
        public string UserId { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}