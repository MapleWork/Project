using Microsoft.EntityFrameworkCore;
using PersonalFinanceApi.Data;
using PersonalFinanceApi.DTOs;
using PersonalFinanceApi.Models;

namespace PersonalFinanceApi.Services
{
    /// <summary>
    /// 交易服務實作類別，處理所有交易記錄相關的業務邏輯
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(AppDbContext context, ILogger<TransactionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 取得使用者的交易記錄清單，按日期降序排列
        /// </summary>
        public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int userId, int page, int pageSize)
        {
            _logger.LogInformation("取得使用者 {UserId} 的交易記錄，頁數: {Page}，每頁: {PageSize}", userId, page, pageSize);

            return await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description,
                    Date = t.Date,
                    Type = t.Type.ToString(),
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category.Name,
                    CategoryColor = t.Category.Color
                })
                .ToListAsync();
        }

        /// <summary>
        /// 根據ID取得特定交易記錄
        /// </summary>
        public async Task<TransactionDto> GetTransactionByIdAsync(int id, int userId)
        {
            _logger.LogInformation("取得交易記錄 {TransactionId}，使用者: {UserId}", id, userId);

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                _logger.LogWarning("交易記錄不存在: {TransactionId}，使用者: {UserId}", id, userId);
                throw new ArgumentException("交易記錄不存在");
            }

            return new TransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Date = transaction.Date,
                Type = transaction.Type.ToString(),
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category.Name,
                CategoryColor = transaction.Category.Color
            };
        }

        /// <summary>
        /// 建立新的交易記錄
        /// </summary>
        public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto, int userId)
        {
            _logger.LogInformation("建立新交易記錄，使用者: {UserId}，金額: {Amount}", userId, dto.Amount);

            ValidateTransactionData(dto);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

            if (category == null)
            {
                _logger.LogWarning("指定的分類不存在: {CategoryId}，使用者: {UserId}", dto.CategoryId, userId);
                throw new ArgumentException("指定的分類不存在");
            }

            var transactionType = Enum.Parse<TransactionType>(dto.Type);
            if (category.Type != transactionType)
            {
                _logger.LogWarning("交易類型與分類類型不匹配: {TransactionType} vs {CategoryType}", dto.Type, category.Type);
                throw new ArgumentException("交易類型與分類類型不匹配");
            }

            var transaction = new Transaction
            {
                Amount = dto.Amount,
                Description = dto.Description.Trim(),
                Date = dto.Date,
                Type = transactionType,
                CategoryId = dto.CategoryId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("交易記錄建立成功: {TransactionId}", transaction.Id);

            return new TransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Date = transaction.Date,
                Type = transaction.Type.ToString(),
                CategoryId = transaction.CategoryId,
                CategoryName = category.Name,
                CategoryColor = category.Color
            };
        }

        /// <summary>
        /// 更新現有交易記錄
        /// </summary>
        public async Task<TransactionDto> UpdateTransactionAsync(int id, UpdateTransactionDto dto, int userId)
        {
            _logger.LogInformation("更新交易記錄 {TransactionId}，使用者: {UserId}", id, userId);

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                _logger.LogWarning("要更新的交易記錄不存在: {TransactionId}，使用者: {UserId}", id, userId);
                throw new ArgumentException("交易記錄不存在");
            }

            ValidateUpdateTransactionData(dto);

            if (transaction.CategoryId != dto.CategoryId)
            {
                var newCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

                if (newCategory == null)
                {
                    _logger.LogWarning("指定的新分類不存在: {CategoryId}，使用者: {UserId}", dto.CategoryId, userId);
                    throw new ArgumentException("指定的分類不存在");
                }

                if (newCategory.Type != transaction.Type)
                {
                    _logger.LogWarning("新分類類型與交易類型不匹配: {CategoryType} vs {TransactionType}", newCategory.Type, transaction.Type);
                    throw new ArgumentException("新分類類型與交易類型不匹配");
                }
            }

            transaction.Amount = dto.Amount;
            transaction.Description = dto.Description.Trim();
            transaction.Date = dto.Date;
            transaction.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();
            await _context.Entry(transaction).Reference(t => t.Category).LoadAsync();

            _logger.LogInformation("交易記錄更新成功: {TransactionId}", id);

            return new TransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                Date = transaction.Date,
                Type = transaction.Type.ToString(),
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category.Name,
                CategoryColor = transaction.Category.Color
            };
        }

        /// <summary>
        /// 刪除交易記錄
        /// </summary>
        public async Task DeleteTransactionAsync(int id, int userId)
        {
            _logger.LogInformation("刪除交易記錄 {TransactionId}，使用者: {UserId}", id, userId);

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                _logger.LogWarning("要刪除的交易記錄不存在: {TransactionId}，使用者: {UserId}", id, userId);
                throw new ArgumentException("交易記錄不存在");
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("交易記錄刪除成功: {TransactionId}", id);
        }

        /// <summary>
        /// 取得指定月份的統計資料
        /// </summary>
        public async Task<object> GetStatisticsAsync(int userId, int year, int month)
        {
            _logger.LogInformation("取得統計資料，使用者: {UserId}，年月: {Year}-{Month}", userId, year, month);

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                .Include(t => t.Category)
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            var totalExpense = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var categoryStats = transactions
                .GroupBy(t => new { t.Category.Id, t.Category.Name, t.Category.Color, t.Category.Type })
                .Select(g => new
                {
                    CategoryId = g.Key.Id,
                    CategoryName = g.Key.Name,
                    CategoryColor = g.Key.Color,
                    Type = g.Key.Type.ToString(),
                    Amount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count(),
                    Percentage = totalIncome + totalExpense > 0 
                        ? Math.Round((g.Sum(t => t.Amount) / (g.Key.Type == TransactionType.Income ? totalIncome : totalExpense)) * 100, 2)
                        : 0
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            var dailyTrends = transactions
                .GroupBy(t => t.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                    Net = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) - 
                          g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return new
            {
                Period = new { Year = year, Month = month, StartDate = startDate, EndDate = endDate },
                Summary = new
                {
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    NetAmount = totalIncome - totalExpense,
                    TransactionCount = transactions.Count,
                    AverageTransactionAmount = transactions.Count > 0 ? Math.Round(transactions.Average(t => t.Amount), 2) : 0
                },
                CategoryBreakdown = categoryStats,
                DailyTrends = dailyTrends
            };
        }

        /// <summary>
        /// 取得使用者的交易記錄總數
        /// </summary>
        public async Task<int> GetTransactionCountAsync(int userId)
        {
            return await _context.Transactions.CountAsync(t => t.UserId == userId);
        }

        /// <summary>
        /// 搜尋交易記錄
        /// </summary>
        public async Task<IEnumerable<TransactionDto>> SearchTransactionsAsync(int userId, string keyword, int page, int pageSize)
        {
            _logger.LogInformation("搜尋交易記錄，使用者: {UserId}，關鍵字: {Keyword}", userId, keyword);

            // 驗證分頁參數
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            // 建立基本查詢
            var query = _context.Transactions
                .Where(t => t.UserId == userId);

            // 處理搜尋關鍵字
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                
                // 檢查是否為數字搜尋（金額）
                if (decimal.TryParse(keyword, out decimal amount))
                {
                    query = query.Where(t => 
                        t.Amount == amount ||
                        t.Description.ToLower().Contains(keyword.ToLower()) ||
                        t.Category.Name.ToLower().Contains(keyword.ToLower()));
                }
                else
                {
                    // 文字搜尋
                    var lowerKeyword = keyword.ToLower();
                    query = query.Where(t => 
                        t.Description.ToLower().Contains(lowerKeyword) ||
                        t.Category.Name.ToLower().Contains(lowerKeyword));
                }
            }

            // 執行查詢並返回結果
            var result = await query
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description,
                    Date = t.Date,
                    Type = t.Type.ToString(),
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category.Name,
                    CategoryColor = t.Category.Color
                })
                .ToListAsync();

            _logger.LogInformation("搜尋完成，找到 {Count} 筆記錄", result.Count);
            return result;
        }

        /// <summary>
        /// 驗證建立交易的資料完整性
        /// </summary>
        private static void ValidateTransactionData(CreateTransactionDto dto)
        {
            if (dto.Amount <= 0)
                throw new ArgumentException("金額必須大於零");

            if (dto.Amount > 999999999)
                throw new ArgumentException("金額過大，超過系統限制");

            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("交易描述不可為空白");

            if (dto.Description.Length > 200)
                throw new ArgumentException("交易描述長度不可超過200個字元");

            if (dto.Date > DateTime.Now.AddDays(1))
                throw new ArgumentException("交易日期不可設定為未來日期");

            if (dto.Date < new DateTime(1900, 1, 1))
                throw new ArgumentException("交易日期過於久遠，請檢查輸入");

            if (!Enum.IsDefined(typeof(TransactionType), dto.Type))
                throw new ArgumentException("交易類型無效，請選擇正確的類型");

            if (dto.CategoryId <= 0)
                throw new ArgumentException("必須選擇有效的交易分類");
        }

        /// <summary>
        /// 驗證更新交易的資料完整性
        /// </summary>
        private static void ValidateUpdateTransactionData(UpdateTransactionDto dto)
        {
            if (dto.Amount <= 0)
                throw new ArgumentException("金額必須大於零");

            if (dto.Amount > 999999999)
                throw new ArgumentException("金額過大，超過系統限制");

            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("交易描述不可為空白");

            if (dto.Description.Length > 200)
                throw new ArgumentException("交易描述長度不可超過200個字元");

            if (dto.Date > DateTime.Now.AddDays(1))
                throw new ArgumentException("交易日期不可設定為未來日期");

            if (dto.Date < new DateTime(1900, 1, 1))
                throw new ArgumentException("交易日期過於久遠，請檢查輸入");

            if (dto.CategoryId <= 0)
                throw new ArgumentException("必須選擇有效的交易分類");
        }
    }
}