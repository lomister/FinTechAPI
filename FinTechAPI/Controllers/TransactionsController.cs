﻿// TransactionsController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FinTechAPI.Data;
using FinTechAPI.Models;
using FinTechAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly FinTechDbContext _context;
        
        public TransactionsController(FinTechDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
                
            return Ok(transactions);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
                
            if (transaction == null)
            {
                return NotFound();
            }
            
            return Ok(transaction);
        }
        
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            // Verify that account belongs to user
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == transaction.AccountId && a.UserId == userId);
                
            if (account == null)
            {
                return BadRequest(new { message = "Invalid account" });
            }
            
            transaction.UserId = userId;
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            
            _context.Transactions.Add(transaction);
            
            // Update account balance
            if (transaction.Type == TransactionType.Income)
            {
                account.Balance += transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance -= transaction.Amount;
            }
            
            account.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            if (id != transaction.Id)
            {
                return BadRequest();
            }
            
            var existingTransaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
                
            if (existingTransaction == null)
            {
                return NotFound();
            }
            
            // Adjust account balance based on transaction change
            var account = existingTransaction.Account;
            
            
            if(account == null)
            {
                return NotFound();
            }
            
            // Revert previous transaction effect
            if (existingTransaction.Type == TransactionType.Income)
            {
                account.Balance -= existingTransaction.Amount;
            }
            else if (existingTransaction.Type == TransactionType.Expense)
            {
                account.Balance += existingTransaction.Amount;
            }
            
            // Apply new transaction effect
            if (transaction.Type == TransactionType.Income)
            {
                account.Balance += transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance -= transaction.Amount;
            }
            
            // Update transaction
            existingTransaction.Amount = transaction.Amount;
            existingTransaction.Type = transaction.Type;
            existingTransaction.Description = transaction.Description;
            existingTransaction.TransactionDate = transaction.TransactionDate;
            existingTransaction.UpdatedAt = DateTime.UtcNow;
            
            account.UpdatedAt = DateTime.UtcNow;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransactionExists(id))
                {
                    return NotFound();
                }

                throw;
            }
            
            return NoContent();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
                
            if (transaction == null)
            {
                return NotFound();
            }
            
            // Adjust account balance when deleting transaction
            var account = transaction.Account;
            
            if (account == null)
            {
                return NotFound();
            }
            
            if (transaction.Type == TransactionType.Income)
            {
                account.Balance -= transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                account.Balance += transaction.Amount;
            }
            
            account.UpdatedAt = DateTime.UtcNow;
            
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        [HttpGet("account/{accountId}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByAccount(int accountId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            // Verify that account belongs to user
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
                
            if (account == null)
            {
                return BadRequest(new { message = "Invalid account" });
            }
            
            var transactions = await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
                
            return Ok(transactions);
        }
        
        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.Id == id);
        }
    }
}