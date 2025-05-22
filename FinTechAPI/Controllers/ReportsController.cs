using Microsoft.AspNetCore.Mvc;
using FinTechAPI.Services;
using System.Collections.Generic;
using FinTechAPI.Models;

namespace FinTechAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ReportingService _reportingService;

        public ReportsController(ReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        // GET: api/reports/category/{category}
        [HttpGet("category/{category}")]
        public ActionResult<IEnumerable<Transaction>> GetTransactionsByCategory(string category)
        {
            var transactions = _reportingService.GetTransactionsByCategory(category);
            return Ok(transactions);
        }

        // GET: api/reports/date-range?startDate={startDate}&endDate={endDate}
        [HttpGet("date-range")]
        public ActionResult<IEnumerable<Transaction>> GetTransactionsByDateRange(string startDate, string endDate)
        {
            if (DateTime.TryParse(startDate, out DateTime parsedStartDate) && DateTime.TryParse(endDate, out DateTime parsedEndDate))
            {
                var transactions = _reportingService.GetTransactionsByDateRange(parsedStartDate, parsedEndDate);
                return Ok(transactions);
            }
            return BadRequest("Invalid date format.");
        }

        // GET: api/reports/total-amount?category={category}
        [HttpGet("total-amount")]
        public ActionResult<decimal> GetTotalAmount(string category)
        {
            var transactions = _reportingService.GetTransactionsByCategory(category);
            var totalAmount = _reportingService.CalculateTotalAmount(transactions);
            return Ok(totalAmount);
        }
    }
}