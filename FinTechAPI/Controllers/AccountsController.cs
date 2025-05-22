// AccountsController.cs
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FinTechAPI.Data;
using FinTechAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly FinTechDbContext _context;
        
        public AccountsController(FinTechDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Balance = a.Balance
                })
                .ToListAsync();
                
            return Ok(accounts);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
                
            if (account == null)
            {
                return NotFound();
            }
            
            return Ok(account);
        }
        
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount(Account account)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            // Ensure that the account belongs to the authenticated user
            account.UserId = userId;
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, Account account)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            if (id != account.Id)
            {
                return BadRequest();
            }
            
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
                
            if (existingAccount == null)
            {
                return NotFound();
            }
            
            // Update account properties
            existingAccount.Name = account.Name;
            existingAccount.AccountType = account.AccountType;
            existingAccount.Balance = account.Balance;  // Note: Be cautious with direct balance updates in production
            existingAccount.Currency = account.Currency;
            existingAccount.UpdatedAt = DateTime.UtcNow;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccountExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            return NoContent();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
                
            if (account == null)
            {
                return NotFound();
            }
            
            // Additional logic could be added here to handle related transactions or data consistency
            
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        private bool AccountExists(int id)
        {
            return _context.Accounts.Any(e => e.Id == id);
        }
    }
}