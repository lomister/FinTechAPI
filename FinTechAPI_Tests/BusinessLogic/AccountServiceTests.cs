using Xunit;
using FinTechAPI.Services;
using FinTechAPI.Data;
using FinTechAPI.Models;
using Microsoft.EntityFrameworkCore;
using
    Moq; // Для IAccountService, если бы мы тестировали что-то, что его использует. Но тут тестируем сам AccountService.

namespace FinTechAPI.Tests
{
    public class AccountServiceTests
    {
        private FinTechDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<FinTechDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new FinTechDbContext(options);
            return dbContext;
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldCreateAndReturnAccount()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var accountService = new AccountService(dbContext);
            var userId = "user-test-1";

            dbContext.Users.Add(new User
            {
                Id = userId, UserName = "testUser", Email = "test@example.com", FirstName = "Test", LastName = "User"
            });
            await dbContext.SaveChangesAsync();

            var newAccount = new Account
            {
                Name = "Test Savings",
                AccountType = AccountType.Savings,
                Balance = 100,
                Currency = Currency.EUR
            };

            var createdAccount = await accountService.CreateAccountAsync(newAccount, userId);

            Assert.NotNull(createdAccount);
            Assert.Equal("Test Savings", createdAccount.Name);
            Assert.Equal(userId, createdAccount.UserId);
            Assert.True(createdAccount.Id > 0);

            var accountInDb = await dbContext.Accounts.FindAsync(createdAccount.Id);
            Assert.NotNull(accountInDb);
            Assert.Equal("Test Savings", accountInDb.Name);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnAccount_WhenExistsForUser()
        {
            var dbContext = GetInMemoryDbContext();
            var accountService = new AccountService(dbContext);
            var userId = "user-test-2";
            dbContext.Users.Add(new User
            {
                Id = userId, UserName = "testUser2", Email = "test2@example.com", FirstName = "Test", LastName = "User"
            });

            var account = new Account
            {
                Name = "My Checking", UserId = userId, AccountType = AccountType.Checking, Balance = 500,
                Currency = Currency.USD
            };
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();

            var result = await accountService.GetAccountByIdAsync(account.Id, userId);

            Assert.NotNull(result);
            Assert.Equal(account.Id, result.Id);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnNull_WhenNotExistsForUser()
        {
            var dbContext = GetInMemoryDbContext();
            var accountService = new AccountService(dbContext);
            var userId = "user-test-3";
            var otherUserId = "user-test-other";
            dbContext.Users.Add(new User
            {
                Id = userId, UserName = "testUser3", Email = "test3@example.com", FirstName = "Test", LastName = "User"
            });
            dbContext.Users.Add(new User
            {
                Id = otherUserId, UserName = "testUserOther", Email = "testOther@example.com", FirstName = "Test",
                LastName = "User"
            });

            var account = new Account
            {
                Name = "Other User Account", UserId = otherUserId, AccountType = AccountType.Checking, Balance = 200,
                Currency = Currency.GBP
            };
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();

            var result =
                await accountService.GetAccountByIdAsync(account.Id,
                    userId); 

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldReturnOnlyUserAccounts()
        {
            var targetUserId = "user-123";
            var otherUserId = "user-789";

            using (var context = GetInMemoryDbContext())
            {
                var user1Accounts = new List<Account>
                {
                    new Account
                    {
                        Id = 1, UserId = targetUserId, Name = "User1 Checking", Balance = 1000,
                        AccountType = AccountType.Checking, Currency = Currency.USD, CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Account
                    {
                        Id = 2, UserId = targetUserId, Name = "User1 Savings", Balance = 5000,
                        AccountType = AccountType.Savings, Currency = Currency.USD, CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
                context.Accounts.AddRange(user1Accounts);

                var user2Account = new Account
                {
                    Id = 3, UserId = otherUserId, Name = "User2 Investment", Balance = 10000,
                    AccountType = AccountType.Investment, Currency = Currency.EUR, CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(user2Account);

                await context.SaveChangesAsync();

                var accountService = new AccountService(context);

                var result = await accountService.GetAccountsByUserIdAsync(targetUserId);

                Assert.NotNull(result);
                var resultList = result.ToList();

                Assert.Equal(user1Accounts.Count, resultList.Count);

                foreach (var expectedAccountEntity in user1Accounts)
                {
                    var actualDto = resultList.FirstOrDefault(dto => dto.Id == expectedAccountEntity.Id);
                    Assert.NotNull(actualDto); 
                    Assert.Equal(expectedAccountEntity.Name, actualDto.Name);
                    Assert.Equal(expectedAccountEntity.Balance, actualDto.Balance);
                }

                Assert.DoesNotContain(resultList, dto => dto.Id == user2Account.Id);
            }
        }

        [Fact]
        public async Task UpdateAccountAsync_ShouldUpdateAccount_WhenExistsForUser()
        {
            var dbContext = GetInMemoryDbContext();
            var accountService = new AccountService(dbContext);
            var userId = "user-update-1";
            dbContext.Users.Add(new User
            {
                Id = userId, UserName = "updateUser1", Email = "update1@example.com", FirstName = "Test",
                LastName = "User"
            });

            var originalAccount = new Account
            {
                Name = "Original Name", UserId = userId, Balance = 100, AccountType = AccountType.Investment,
                Currency = Currency.USD
            };
            dbContext.Accounts.Add(originalAccount);
            await dbContext.SaveChangesAsync();

            var accountUpdateDetails = new Account
            {
                Id = originalAccount.Id, 
                Name = "Updated Name",
                AccountType =
                    AccountType
                        .Savings,
                Balance = 150, 
                Currency = Currency.EUR, 
                UserId = userId 
            };

            var updatedAccount =
                await accountService.UpdateAccountAsync(originalAccount.Id, accountUpdateDetails, userId);

            Assert.NotNull(updatedAccount);
            Assert.Equal(originalAccount.Id, updatedAccount.Id);
            Assert.Equal("Updated Name", updatedAccount.Name); 
            Assert.Equal(AccountType.Savings, updatedAccount.AccountType); 

            var accountInDb = await dbContext.Accounts.FindAsync(originalAccount.Id);
            Assert.NotNull(accountInDb);
            Assert.Equal("Updated Name", accountInDb.Name);
            Assert.Equal(AccountType.Savings, accountInDb.AccountType);
            Assert.Equal(100, accountInDb.Balance); 
            Assert.Equal(Currency.EUR, accountInDb.Currency); 
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldDeleteAccount_WhenExistsForUser()
        {
            var dbContext = GetInMemoryDbContext();
            var accountService = new AccountService(dbContext);
            var userId = "user-delete-1";
            dbContext.Users.Add(new User
            {
                Id = userId, UserName = "deleteUser1", Email = "delete1@example.com", FirstName = "Test",
                LastName = "User"
            });

            var account = new Account
            {
                Name = "To Be Deleted", UserId = userId, Balance = 50, AccountType = AccountType.Checking,
                Currency = Currency.USD
            };
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            var accountIdToDelete = account.Id;

            var success = await accountService.DeleteAccountAsync(accountIdToDelete, userId);

            Assert.True(success);
            var accountInDb = await dbContext.Accounts.FindAsync(accountIdToDelete);
            Assert.Null(accountInDb);
        }

        // TODO: Добавьте тесты для следующих сценариев:
        // - CreateAccountAsync: попытка создать счет для несуществующего UserId (если есть такая проверка)
        // - UpdateAccountAsync: попытка обновить несуществующий счет
        // - UpdateAccountAsync: попытка обновить счет другого пользователя
        // - DeleteAccountAsync: попытка удалить несуществующий счет
        // - DeleteAccountAsync: попытка удалить счет другого пользователя
        // - DeleteAccountAsync: попытка удалить счет, если с ним связаны транзакции (в зависимости от вашей логики каскадного удаления)
        // - AccountExistsAsync
    }
}