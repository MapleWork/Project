using Microsoft.EntityFrameworkCore;
using PersonalFinanceApi.Data;
using PersonalFinanceApi.DTOs;
using PersonalFinanceApi.Models;

namespace PersonalFinanceApi.Services
{
    /// <summary>
    /// 分類服務實作類別，處理所有分類相關的業務邏輯
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(AppDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 取得使用者的所有分類，按類型和名稱排序
        /// </summary>
        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(int userId)
        {
            _logger.LogInformation("取得使用者 {UserId} 的所有分類", userId);

            return await _context.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color,
                    Type = c.Type.ToString()
                })
                .ToListAsync();
        }

        /// <summary>
        /// 根據ID取得特定分類
        /// </summary>
        public async Task<CategoryDto> GetCategoryByIdAsync(int id, int userId)
        {
            _logger.LogInformation("取得分類 {CategoryId}，使用者: {UserId}", id, userId);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                _logger.LogWarning("分類不存在: {CategoryId}，使用者: {UserId}", id, userId);
                throw new ArgumentException("分類不存在");
            }

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                Type = category.Type.ToString()
            };
        }

        /// <summary>
        /// 建立新分類
        /// </summary>
        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, int userId)
        {
            _logger.LogInformation("建立新分類，使用者: {UserId}，分類名稱: {CategoryName}", userId, dto.Name);

            ValidateCategoryData(dto);

            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Name.ToLower() == dto.Name.ToLower().Trim());

            if (existingCategory != null)
            {
                _logger.LogWarning("分類名稱已存在: {CategoryName}，使用者: {UserId}", dto.Name, userId);
                throw new ArgumentException("分類名稱已存在");
            }

            var transactionType = Enum.Parse<TransactionType>(dto.Type);

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Color = ValidateAndNormalizeColor(dto.Color),
                Type = transactionType,
                UserId = userId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("分類建立成功: {CategoryId}", category.Id);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                Type = category.Type.ToString()
            };
        }

        /// <summary>
        /// 更新現有分類
        /// </summary>
        public async Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto dto, int userId)
        {
            _logger.LogInformation("更新分類 {CategoryId}，使用者: {UserId}", id, userId);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                _logger.LogWarning("要更新的分類不存在: {CategoryId}，使用者: {UserId}", id, userId);
                throw new ArgumentException("分類不存在");
            }

            ValidateUpdateCategoryData(dto);

            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == userId && 
                                        c.Name.ToLower() == dto.Name.ToLower().Trim() && 
                                        c.Id != id);

            if (existingCategory != null)
            {
                _logger.LogWarning("分類名稱已存在: {CategoryName}，使用者: {UserId}", dto.Name, userId);
                throw new ArgumentException("分類名稱已存在");
            }

            category.Name = dto.Name.Trim();
            category.Color = ValidateAndNormalizeColor(dto.Color);

            await _context.SaveChangesAsync();

            _logger.LogInformation("分類更新成功: {CategoryId}", id);

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                Type = category.Type.ToString()
            };
        }

        /// <summary>
        /// 刪除分類
        /// </summary>
        public async Task DeleteCategoryAsync(int id, int userId)
        {
            _logger.LogInformation("刪除分類 {CategoryId}，使用者: {UserId}", id, userId);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                _logger.LogWarning("要刪除的分類不存在: {CategoryId}，使用者: {UserId}", id, userId);
                throw new ArgumentException("分類不存在");
            }

            var hasTransactions = await _context.Transactions
                .AnyAsync(t => t.CategoryId == id);

            if (hasTransactions)
            {
                _logger.LogWarning("分類有關聯交易記錄，無法刪除: {CategoryId}", id);
                throw new ArgumentException("此分類已有交易記錄，無法刪除。請先將相關交易記錄轉移到其他分類或刪除相關交易記錄");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("分類刪除成功: {CategoryId}", id);
        }

        /// <summary>
        /// 取得指定類型的分類
        /// </summary>
        public async Task<IEnumerable<CategoryDto>> GetCategoriesByTypeAsync(int userId, string type)
        {
            _logger.LogInformation("取得使用者 {UserId} 的 {Type} 類型分類", userId, type);

            if (!Enum.TryParse<TransactionType>(type, out var transactionType))
            {
                throw new ArgumentException("無效的交易類型，請使用 Income 或 Expense");
            }

            return await _context.Categories
                .Where(c => c.UserId == userId && c.Type == transactionType)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color,
                    Type = c.Type.ToString()
                })
                .ToListAsync();
        }

        /// <summary>
        /// 檢查分類是否可以刪除
        /// </summary>
        public async Task<bool> CanDeleteCategoryAsync(int categoryId, int userId)
        {
            _logger.LogInformation("檢查分類 {CategoryId} 是否可刪除，使用者: {UserId}", categoryId, userId);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

            if (category == null)
            {
                return false;
            }

            var hasTransactions = await _context.Transactions
                .AnyAsync(t => t.CategoryId == categoryId);

            return !hasTransactions;
        }

        /// <summary>
        /// 取得分類的使用統計
        /// </summary>
        public async Task<object> GetCategoryUsageStatisticsAsync(int categoryId, int userId)
        {
            _logger.LogInformation("取得分類 {CategoryId} 使用統計，使用者: {UserId}", categoryId, userId);

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

            if (category == null)
            {
                throw new ArgumentException("分類不存在");
            }

            var transactions = await _context.Transactions
                .Where(t => t.CategoryId == categoryId && t.UserId == userId)
                .ToListAsync();

            var totalAmount = transactions.Sum(t => t.Amount);
            var transactionCount = transactions.Count;
            var averageAmount = transactionCount > 0 ? totalAmount / transactionCount : 0;

            var lastTransaction = transactions
                .OrderByDescending(t => t.Date)
                .FirstOrDefault();

            var monthlyUsage = transactions
                .GroupBy(t => new { Year = t.Date.Year, Month = t.Date.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(12)
                .ToList();

            return new
            {
                CategoryInfo = new
                {
                    Id = category.Id,
                    Name = category.Name,
                    Color = category.Color,
                    Type = category.Type.ToString()
                },
                Statistics = new
                {
                    TotalAmount = totalAmount,
                    TransactionCount = transactionCount,
                    AverageAmount = Math.Round(averageAmount, 2),
                    LastTransactionDate = lastTransaction?.Date,
                    CanDelete = transactionCount == 0
                },
                MonthlyUsage = monthlyUsage
            };
        }

        /// <summary>
        /// 驗證建立分類的資料完整性
        /// </summary>
        private static void ValidateCategoryData(CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("分類名稱不可為空白");

            var trimmedName = dto.Name.Trim();
            if (trimmedName.Length < 1 || trimmedName.Length > 50)
                throw new ArgumentException("分類名稱長度必須在1到50個字元之間");

            if (trimmedName.Contains("  "))
                throw new ArgumentException("分類名稱不可包含連續空格");

            if (!Enum.TryParse<TransactionType>(dto.Type, out _))
                throw new ArgumentException("無效的交易類型，請選擇 Income 或 Expense");

            if (string.IsNullOrWhiteSpace(dto.Color))
                throw new ArgumentException("分類顏色不可為空白");
        }

        /// <summary>
        /// 驗證更新分類的資料完整性
        /// </summary>
        private static void ValidateUpdateCategoryData(UpdateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("分類名稱不可為空白");

            var trimmedName = dto.Name.Trim();
            if (trimmedName.Length < 1 || trimmedName.Length > 50)
                throw new ArgumentException("分類名稱長度必須在1到50個字元之間");

            if (trimmedName.Contains("  "))
                throw new ArgumentException("分類名稱不可包含連續空格");

            if (string.IsNullOrWhiteSpace(dto.Color))
                throw new ArgumentException("分類顏色不可為空白");
        }

        /// <summary>
        /// 驗證並標準化顏色格式
        /// </summary>
        private static string ValidateAndNormalizeColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return "#000000";

            color = color.Trim();

            if (!color.StartsWith("#"))
                color = "#" + color;

            if (color.Length == 4 && System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{3}$"))
            {
                return $"#{color[1]}{color[1]}{color[2]}{color[2]}{color[3]}{color[3]}".ToUpper();
            }
            else if (color.Length == 7 && System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$"))
            {
                return color.ToUpper();
            }

            throw new ArgumentException("無效的顏色格式，請使用 #RGB 或 #RRGGBB 格式（例如：#FF0000 或 #F00）");
        }
    }
}