using FinTechAPI.Services;
using FinTechAPI.Data;
using FinTechAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Tests
{
    public class SecurityServiceTests
    {
        private FinTechDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<FinTechDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new FinTechDbContext(options);
            return dbContext;
        }
        
        private async Task SeedAccountsAndTransactionsAsync(FinTechDbContext context)
        {
            var user1 = new User { Id = "sec_user_1", UserName = "secUser1", Email = "sec1@example.com", FirstName = "Test", LastName = "User" };
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var account1User1 = new Account { Name = "Acc1", UserId = user1.Id, Balance = 1000, Currency = Currency.USD, AccountType = AccountType.Checking };
            context.Accounts.Add(account1User1);
            await context.SaveChangesAsync();

            context.Transactions.AddRange(
                new Transaction { Amount = 50, UserId = user1.Id, AccountId = account1User1.Id, Type = TransactionType.Expense, TransactionDate = DateTime.UtcNow },
                new Transaction { Amount = 1500, UserId = user1.Id, AccountId = account1User1.Id, Type = TransactionType.Income, TransactionDate = DateTime.UtcNow }, // Аномально большой доход
                new Transaction { Amount = 20, UserId = user1.Id, AccountId = account1User1.Id, Type = TransactionType.Expense, TransactionDate = DateTime.UtcNow },
                new Transaction { Amount = 7000, UserId = user1.Id, AccountId = account1User1.Id, Type = TransactionType.Expense, TransactionDate = DateTime.UtcNow } // Аномально большой расход
            );
            await context.SaveChangesAsync();
        }


        [Fact]
        public async Task DetectAnomaliesAsync_ShouldReturnTransactionsAboveThreshold()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            await SeedAccountsAndTransactionsAsync(dbContext);
            var securityService = new SecurityService(dbContext);
            var threshold = 1000m; // Порог аномалии

            // Act
            var anomalies = await securityService.DetectAnomaliesAsync(threshold);

            // Assert
            Assert.NotNull(anomalies);
            Assert.Equal(2, anomalies.Count()); // Ожидаем две аномальные транзакции (1500 и 7000)
            Assert.All(anomalies, t => Assert.True(t.Amount > threshold));
            Assert.Contains(anomalies, t => t.Amount == 1500);
            Assert.Contains(anomalies, t => t.Amount == 7000);
        }

        [Fact]
        public async Task DetectAnomaliesAsync_ShouldReturnEmpty_WhenNoTransactionsAboveThreshold()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            await SeedAccountsAndTransactionsAsync(dbContext); // Используем те же данные
            var securityService = new SecurityService(dbContext);
            var threshold = 10000m; // Очень высокий порог

            // Act
            var anomalies = await securityService.DetectAnomaliesAsync(threshold);

            // Assert
            Assert.NotNull(anomalies);
            Assert.Empty(anomalies);
        }
    }
}