using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PersonalFinanceApi.Data;
using PersonalFinanceApi.DTOs;
using PersonalFinanceApi.Models;

namespace PersonalFinanceApi.Services
{
    /// <summary>
    /// 認證服務實作類別，處理使用者註冊、登入和令牌管理
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context, 
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 註冊新使用者並建立預設分類
        /// </summary>
        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            _logger.LogInformation("開始處理使用者註冊請求: {Username}", request.Username);

            // 驗證輸入資料
            await ValidateRegistrationRequestAsync(request);

            // 檢查使用者名稱重複
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                _logger.LogWarning("註冊失敗：使用者名稱已存在 {Username}", request.Username);
                throw new ArgumentException("使用者名稱已存在");
            }

            // 檢查電子郵件重複
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                _logger.LogWarning("註冊失敗：電子郵件已存在 {Email}", request.Email);
                throw new ArgumentException("電子郵件已存在");
            }

            // 建立新使用者
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("使用者註冊成功: {UserId}", user.Id);

            // 建立預設分類
            await CreateDefaultCategoriesAsync(user.Id);

            // 產生JWT令牌
            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email
            };
        }

        /// <summary>
        /// 處理使用者登入請求
        /// </summary>
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            _logger.LogInformation("開始處理使用者登入請求: {Username}", request.Username);

            // 查詢使用者
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                _logger.LogWarning("登入失敗：使用者不存在 {Username}", request.Username);
                throw new ArgumentException("使用者名稱或密碼錯誤");
            }

            // 驗證密碼
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("登入失敗：密碼錯誤 {Username}", request.Username);
                throw new ArgumentException("使用者名稱或密碼錯誤");
            }

            _logger.LogInformation("使用者登入成功: {UserId}", user.Id);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email
            };
        }

        /// <summary>
        /// 驗證JWT令牌有效性
        /// </summary>
        public Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("JWT令牌驗證失敗: {Error}", ex.Message);
                return Task.FromResult(false);
            }
        }
        
        /// <summary>
        /// 產生JWT令牌
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("created_at", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 驗證註冊請求資料
        /// </summary>
        private static async Task ValidateRegistrationRequestAsync(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("使用者名稱不可為空");

            if (request.Username.Length < 3 || request.Username.Length > 50)
                throw new ArgumentException("使用者名稱長度必須在3到50個字元之間");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("電子郵件不可為空");

            if (!IsValidEmail(request.Email))
                throw new ArgumentException("電子郵件格式不正確");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("密碼不可為空");

            if (request.Password.Length < 6)
                throw new ArgumentException("密碼長度至少需要6個字元");

            await Task.CompletedTask;
        }

        /// <summary>
        /// 驗證電子郵件格式
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 為新註冊使用者建立預設分類
        /// </summary>
        private async Task CreateDefaultCategoriesAsync(int userId)
        {
            var defaultCategories = new List<Category>
            {
                // 收入分類
                new Category { Name = "薪資", Color = "#4CAF50", Type = TransactionType.Income, UserId = userId },
                new Category { Name = "投資收益", Color = "#2196F3", Type = TransactionType.Income, UserId = userId },
                new Category { Name = "兼職收入", Color = "#00BCD4", Type = TransactionType.Income, UserId = userId },
                new Category { Name = "獎金", Color = "#009688", Type = TransactionType.Income, UserId = userId },
                new Category { Name = "其他收入", Color = "#607D8B", Type = TransactionType.Income, UserId = userId },

                // 支出分類
                new Category { Name = "餐飲", Color = "#FF9800", Type = TransactionType.Expense, UserId = userId },
                new Category { Name = "交通", Color = "#9C27B0", Type = TransactionType.Expense, UserId = userId },
                new Category { Name = "購物", Color = "#E91E63", Type = TransactionType.Expense, UserId = userId },
                new Category { Name = "娛樂", Color = "#3F51B5", Type = TransactionType.Expense, UserId = userId },
                new Category { Name = "醫療", Color = "#F44336", Type = TransactionType.Expense, UserId = userId },
                new Category { Name = "教育", Color = "#795548", Type = TransactionType.Expense, UserId = userId },
                new Category { Name = "居住", Color = "#FF5722", Type = TransactionType.Expense, UserId = userId },
                new Category { Name = "保險", Color = "#673AB7", Type = TransactionType.Expense, UserId = userId }
            };

            _context.Categories.AddRange(defaultCategories);
            await _context.SaveChangesAsync();

            _logger.LogInformation("已為使用者 {UserId} 建立 {CategoryCount} 個預設分類", userId, defaultCategories.Count);
        }
    }
}