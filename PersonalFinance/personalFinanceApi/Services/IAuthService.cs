using PersonalFinanceApi.DTOs;

namespace PersonalFinanceApi.Services
{
    /// <summary>
    /// 認證服務介面，定義使用者註冊與登入相關操作
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 註冊新使用者
        /// </summary>
        /// <param name="request">註冊請求資料</param>
        /// <returns>認證回應，包含JWT令牌和使用者資訊</returns>
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

        /// <summary>
        /// 使用者登入驗證
        /// </summary>
        /// <param name="request">登入請求資料</param>
        /// <returns>認證回應，包含JWT令牌和使用者資訊</returns>
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// 驗證JWT令牌有效性
        /// </summary>
        /// <param name="token">JWT令牌</param>
        /// <returns>令牌是否有效</returns>
        Task<bool> ValidateTokenAsync(string token);
    }
}