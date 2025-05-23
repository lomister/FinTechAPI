using Xunit;
using FinTechAPI.Services;
using FinTechAPI.Data;
using FinTechAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTechAPI.Tests
{
    public class ReportingServiceTests
    {
        private FinTechDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<FinTechDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new FinTechDbContext(options);
            return dbContext;
        }

        private async Task SeedDataAsync(FinTechDbContext context, string userId)
        {
            context.Users.Add(new User { Id = userId, UserName = "reportUser", Email = "report@example.com", FirstName = "Test", LastName = "User" });
            
            var account = new Account { Name = "Report Account", UserId = userId, AccountType = AccountType.Checking, Balance = 1000, Currency = Currency.USD };
            context.Accounts.Add(account);
            await context.SaveChangesAsync(); // Сохраняем аккаунт, чтобы получить его Id

            context.Transactions.AddRange(
                new Transaction { UserId = userId, AccountId = account.Id, Type = TransactionType.Income, Amount = 200, TransactionDate = new DateTime(2023, 1, 10), Description = "Salary" },
                new Transaction { UserId = userId, AccountId = account.Id, Type = TransactionType.Expense, Amount = 50, TransactionDate = new DateTime(2023, 1, 12), Description = "Groceries" },
                new Transaction { UserId = userId, AccountId = account.Id, Type = TransactionType.Income, Amount = 300, TransactionDate = new DateTime(2023, 2, 5), Description = "Freelance" },
                new Transaction { UserId = userId, AccountId = account.Id, Type = TransactionType.Expense, Amount = 100, TransactionDate = new DateTime(2023, 2, 15), Description = "Bills" }
            );
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetTransactionsByTypeAsync_ShouldReturnCorrectTransactions()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var userId = "user-report-type";
            await SeedDataAsync(dbContext, userId);
            var reportingService = new ReportingService(dbContext);

            // Act
            var incomeTransactions = await reportingService.GetTransactionsByTypeAsync(TransactionType.Income, userId);
            var expenseTransactions = await reportingService.GetTransactionsByTypeAsync(TransactionType.Expense, userId);

            // Assert
            Assert.Equal(2, incomeTransactions.Count());
            Assert.All(incomeTransactions, t => Assert.Equal(TransactionType.Income, t.Type));
            Assert.All(incomeTransactions, t => Assert.Equal(userId, t.UserId));

            Assert.Equal(2, expenseTransactions.Count());
            Assert.All(expenseTransactions, t => Assert.Equal(TransactionType.Expense, t.Type));
            Assert.All(expenseTransactions, t => Assert.Equal(userId, t.UserId));
        }

        [Fact]
        public async Task GetTransactionsByDateRangeAsync_ShouldReturnCorrectTransactions()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var userId = "user-report-date"; // Этот userId не используется в текущей реализации метода, но может понадобиться если логика изменится
            await SeedDataAsync(dbContext, userId); // SeedData привязывает транзакции к userId
            var reportingService = new ReportingService(dbContext);
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var transactionsInJanuary = await reportingService.GetTransactionsByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Equal(2, transactionsInJanuary.Count());
            Assert.Contains(transactionsInJanuary, t => t.Description == "Salary");
            Assert.Contains(transactionsInJanuary, t => t.Description == "Groceries");
        }

        [Fact]
        public void CalculateTotalAmount_ShouldReturnCorrectSum()
        {
            // Arrange
            var reportingService = new ReportingService(null!); // DbContext не используется этим методом
            var transactions = new List<Transaction>
            {
                new Transaction { Amount = 100 },
                new Transaction { Amount = 150 },
                new Transaction { Amount = -20 } // Расход
            };

            // Act
            var totalAmount = reportingService.CalculateTotalAmount(transactions);

            // Assert
            Assert.Equal(230, totalAmount); // 100 + 150 - 20
        }
    }
}