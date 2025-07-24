using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PersonalFinanceApi.DTOs;
using PersonalFinanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.InteropServices;

namespace PersonalFinanceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// 取得交易紀錄清單 (分頁)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var userId = GetUserId();
                var transactions = await _transactionService.GetTransactionsAsync(userId, page, pageSize);
                
                return Ok(new {
                    success = true,
                    data = transactions,
                    pagination = new { page, pageSize }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "取得交易紀錄失敗" });
            }
        }

        /// <summary>
        /// 取得單一交易紀錄
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(int id)
        {
            try
            {
                var userId = GetUserId();
                var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);
                return Ok(new { success = true, data = transaction }); 
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "取得交易紀錄失敗" });
            }
        }

        /// <summary>
        /// 建立新交易紀錄
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var transaction = await _transactionService.CreateTransactionAsync(dto, userId);

                return CreatedAtAction(
                    nameof(GetTransaction),
                    new { id = transaction.Id },
                    new { success = true, data = transaction });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "建立交易紀錄失敗" });
            }
        }

        /// <summary>
        /// 更新交易紀錄
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateTransactionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var transaction = await _transactionService.UpdateTransactionAsync(id, dto, userId);
                return Ok(new { success = true, data = transaction });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "更新交易紀錄失敗" });
            }
        }

        /// <summary>
        /// 刪除交易紀錄
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            try
            {
                var userId = GetUserId();
                await _transactionService.DeleteTransactionAsync(id, userId);
                return Ok(new { success = true, message = "交易紀錄已刪除" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "刪除交易紀錄失敗" });
            }
        }

        /// <summary>
        /// 取得統計資料
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] int year, [FromQuery] int month)
        {
            try
            {
                if (year < 1900 || year > 2100)
                    return BadRequest(new { success = false, message = "年份範圍錯誤" });

                if (month < 1 || month > 12)
                    return BadRequest(new { success = false, message = "月份範圍錯誤" });

                var userId = GetUserId();
                var statistics = await _transactionService.GetStatisticsAsync(userId, year, month);
                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "取得統計資料失敗" });
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new UnauthorizedAccessException("無效的使用者身份");

            return userId;
        }
    }
}