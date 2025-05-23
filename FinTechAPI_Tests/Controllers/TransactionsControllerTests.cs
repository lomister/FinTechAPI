// using Xunit;
// using Moq;
// using FinTechAPI.Controllers;
// using FinTechAPI.Services; // Для ITransactionService
// using FinTechAPI.Models;
// using Microsoft.AspNetCore.Mvc;
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using System.Linq;
// // FinTechAPI.Data и Microsoft.EntityFrameworkCore не нужны, если сервис полностью мокирован
//
// namespace FinTechAPI.Tests
// {
//     public class TransactionsControllerTests
//     {
//         private readonly Mock<ITransactionService> _mockTransactionService;
//         private readonly TransactionsController _controller;
//         private readonly string _testUserId = "tx-user-id";
//         private readonly string _testUserEmail = "tx-user@example.com";
//
//         public TransactionsControllerTests()
//         {
//             _mockTransactionService = new Mock<ITransactionService>();
//             // Предполагаем, что TransactionsController был рефакторен для использования ITransactionService
//             _controller = new TransactionsController(_mockTransactionService.Object); 
//             _controller.ControllerContext = new ControllerContext
//             {
//                 HttpContext = ControllerTestHelpers.CreateHttpContext(_testUserId, _testUserEmail)
//             };
//         }
//
//         [Fact]
//         public async Task GetTransactions_ShouldReturnOkObjectResult_WithUserTransactions()
//         {
//             // Arrange
//             var transactions = new List<Transaction>
//             {
//                 new Transaction { Id = 1, UserId = _testUserId, Amount = 100 },
//                 new Transaction { Id = 2, UserId = _testUserId, Amount = 50 }
//             };
//             _mockTransactionService.Setup(s => s.GetTransactionsAsync(_testUserId))
//                 .ReturnsAsync(transactions);
//
//             // Act
//             var result = await _controller.GetTransactions();
//
//             // Assert
//             var okResult = Assert.IsType<OkObjectResult>(result.Result);
//             var returnValue = Assert.IsAssignableFrom<IEnumerable<Transaction>>(okResult.Value);
//             Assert.Equal(2, returnValue.Count());
//             Assert.All(returnValue, t => Assert.Equal(_testUserId, t.UserId));
//         }
//
//         [Fact]
//         public async Task GetTransaction_ShouldReturnOkObjectResult_WhenTransactionExistsForUser()
//         {
//             // Arrange
//             var transactionId = 1;
//             var transaction = new Transaction { Id = transactionId, UserId = _testUserId, Amount = 100 };
//             _mockTransactionService.Setup(s => s.GetTransactionByIdAsync(transactionId, _testUserId))
//                 .ReturnsAsync(transaction);
//
//             // Act
//             var result = await _controller.GetTransaction(transactionId);
//
//             // Assert
//             var okResult = Assert.IsType<OkObjectResult>(result.Result);
//             var returnValue = Assert.IsType<Transaction>(okResult.Value);
//             Assert.Equal(transactionId, returnValue.Id);
//             Assert.Equal(_testUserId, returnValue.UserId);
//         }
//
//         [Fact]
//         public async Task GetTransaction_ShouldReturnNotFound_WhenTransactionDoesNotExistForUser()
//         {
//             // Arrange
//             var transactionId = 99;
//             _mockTransactionService.Setup(s => s.GetTransactionByIdAsync(transactionId, _testUserId))
//                 .ReturnsAsync((Transaction)null!);
//
//             // Act
//             var result = await _controller.GetTransaction(transactionId);
//
//             // Assert
//             Assert.IsType<NotFoundResult>(result.Result);
//         }
//         
//         [Fact]
//         public async Task CreateTransaction_ShouldReturnCreatedAtActionResult_WithCreatedTransaction()
//         {
//             // Arrange
//             var accountId = 1; // Предполагаем, что такой счет существует и принадлежит пользователю
//             var transactionToCreate = new Transaction { AccountId = accountId, Amount = 75, Type = TransactionType.Income, Description = "Bonus" };
//             var createdTransaction = new Transaction { Id = 3, AccountId = accountId, UserId = _testUserId, Amount = 75, Type = TransactionType.Income, Description = "Bonus" };
//
//             _mockTransactionService.Setup(s => s.AccountBelongsToUserAsync(accountId, _testUserId)).ReturnsAsync(true);
//             _mockTransactionService.Setup(s => s.CreateTransactionAsync(It.Is<Transaction>(t => t.AccountId == accountId), _testUserId))
//                 .ReturnsAsync(createdTransaction);
//
//             // Act
//             var result = await _controller.CreateTransaction(transactionToCreate);
//
//             // Assert
//             var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
//             Assert.Equal(nameof(TransactionsController.GetTransaction), createdAtActionResult.ActionName);
//             Assert.Equal(createdTransaction.Id, createdAtActionResult.RouteValues["id"]);
//             var returnValue = Assert.IsType<Transaction>(createdAtActionResult.Value);
//             Assert.Equal(createdTransaction.Id, returnValue.Id);
//         }
//         
//         [Fact]
//         public async Task CreateTransaction_ShouldReturnBadRequest_WhenAccountIsInvalidOrNotBelongsToUser()
//         {
//             // Arrange
//             var invalidAccountId = 999;
//             var transactionToCreate = new Transaction { AccountId = invalidAccountId, Amount = 100, Type = TransactionType.Expense };
//             
//             // Случай, когда сервис говорит, что счет не принадлежит пользователю
//             _mockTransactionService.Setup(s => s.AccountBelongsToUserAsync(invalidAccountId, _testUserId)).ReturnsAsync(false);
//
//             // Act
//             var result = await _controller.CreateTransaction(transactionToCreate);
//
//             // Assert
//             var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
//             Assert.NotNull(badRequestResult.Value); // Проверяем, что есть сообщение об ошибке
//              // Можно проверить конкретное сообщение, если оно стандартизировано
//             var message = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null);
//             Assert.Equal("Invalid account or account does not belong to the user.", message);
//         }
//
//
//         // TODO: Напишите тесты для UpdateTransaction, DeleteTransaction, GetTransactionsByAccount
//         // - Успешные сценарии
//         // - Сценарии, когда транзакция/счет не найдены
//         // - Сценарии, когда ID в URL и теле не совпадают (для Update)
//         // - Сценарии неавторизованного доступа (когда userId пустой)
//         // - Валидация (например, при создании транзакции, если счет не принадлежит пользователю)
//     }
// }