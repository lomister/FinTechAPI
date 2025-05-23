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
            // Можно добавить начальные данные пользователей, если это необходимо для тестов
            // dbContext.Users.Add(new User { Id = "test-user-1", UserName = "test1@example.com", Email = "test1@example.com" });
            // dbContext.SaveChanges();
            return dbContext;
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldCreateAndReturnAccount()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var accountService = new AccountService(dbContext);
            var userId = "user-test-1";

            // Убедимся, что пользователь существует, если ваш DbContext настроен с проверкой внешних ключей
            // или если логика сервиса это проверяет. Для InMemory это может быть не так критично.
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

            // Act
            var createdAccount = await accountService.CreateAccountAsync(newAccount, userId);

            // Assert
            Assert.NotNull(createdAccount);
            Assert.Equal("Test Savings", createdAccount.Name);
            Assert.Equal(userId, createdAccount.UserId);
            Assert.True(createdAccount.Id > 0); // ID должен быть присвоен базой данных

            var accountInDb = await dbContext.Accounts.FindAsync(createdAccount.Id);
            Assert.NotNull(accountInDb);
            Assert.Equal("Test Savings", accountInDb.Name);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnAccount_WhenExistsForUser()
        {
            // Arrange
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
            await dbContext.SaveChangesAsync(); // Сохраняем, чтобы получить Id

            // Act
            var result = await accountService.GetAccountByIdAsync(account.Id, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(account.Id, result.Id);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnNull_WhenNotExistsForUser()
        {
            // Arrange
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

            // Act
            var result =
                await accountService.GetAccountByIdAsync(account.Id,
                    userId); // Пытаемся получить счет другого пользователя

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountsByUserIdAsync_ShouldReturnOnlyUserAccounts()
        {
            // Arrange
            var targetUserId = "user-123";
            var otherUserId = "user-789";

            using (var context = GetInMemoryDbContext())
            {
                // Seed data: Accounts for the target user
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

                // Seed data: Account for another user
                var user2Account = new Account
                {
                    Id = 3, UserId = otherUserId, Name = "User2 Investment", Balance = 10000,
                    AccountType = AccountType.Investment, Currency = Currency.EUR, CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(user2Account);

                await context.SaveChangesAsync();

                var accountService = new AccountService(context); // Assuming AccountService takes FinTechDbContext

                // Act
                var result = await accountService.GetAccountsByUserIdAsync(targetUserId);

                // Assert
                Assert.NotNull(result);
                var resultList = result.ToList();

                // 1. Check if the correct number of accounts is returned
                Assert.Equal(user1Accounts.Count, resultList.Count);

                // 2. Check if all returned accounts indeed belong to the target user and properties match
                foreach (var expectedAccountEntity in user1Accounts)
                {
                    var actualDto = resultList.FirstOrDefault(dto => dto.Id == expectedAccountEntity.Id);
                    Assert.NotNull(actualDto); // Ensure the account is found in the results
                    Assert.Equal(expectedAccountEntity.Name, actualDto.Name);
                    Assert.Equal(expectedAccountEntity.Balance, actualDto.Balance);
                    // Add more property checks if your AccountDto has them
                }

                // 3. Check that no accounts from other users are returned
                Assert.DoesNotContain(resultList, dto => dto.Id == user2Account.Id);
            }
        }


        [Fact]
        public async Task UpdateAccountAsync_ShouldUpdateAccount_WhenExistsForUser()
        {
            // Arrange
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
                Id = originalAccount.Id, // Важно передать Id
                Name = "Updated Name",
                AccountType =
                    AccountType
                        .Savings, // Тип счета не должен меняться по логике сервиса, но мы тестируем передачу данных
                Balance = 150, // Баланс тоже не должен меняться напрямую этим методом в вашей реализации
                Currency = Currency.EUR, // Валюта тоже
                UserId = userId // UserId не должен меняться
            };

            // Act
            // В вашем AccountService.UpdateAccountAsync обновляются только Name, AccountType.
            // Баланс и валюта не должны меняться этим методом.
            var updatedAccount =
                await accountService.UpdateAccountAsync(originalAccount.Id, accountUpdateDetails, userId);

            // Assert
            Assert.NotNull(updatedAccount);
            Assert.Equal(originalAccount.Id, updatedAccount.Id);
            Assert.Equal("Updated Name", updatedAccount.Name); // Имя должно обновиться
            Assert.Equal(AccountType.Savings, updatedAccount.AccountType); // Тип должен обновиться

            var accountInDb = await dbContext.Accounts.FindAsync(originalAccount.Id);
            Assert.NotNull(accountInDb);
            Assert.Equal("Updated Name", accountInDb.Name);
            Assert.Equal(AccountType.Savings, accountInDb.AccountType);
            Assert.Equal(100, accountInDb.Balance); // Баланс не должен был измениться
            Assert.Equal(Currency.EUR, accountInDb.Currency); // Валюта не должна была измениться
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldDeleteAccount_WhenExistsForUser()
        {
            // Arrange
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

            // Act
            var success = await accountService.DeleteAccountAsync(accountIdToDelete, userId);

            // Assert
            Assert.True(success);
            var accountInDb = await dbContext.Accounts.FindAsync(accountIdToDelete);
            Assert.Null(accountInDb); // Счет должен быть удален
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