using PersonalFinanceApi.DTOs;

namespace PersonalFinanceApi.Services
{
    /// <summary>
    /// 交易服務介面，定義交易記錄相關操作
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// 取得使用者的交易記錄清單（支援分頁）
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>交易記錄清單</returns>
        Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int userId, int page, int pageSize);

        /// <summary>
        /// 根據ID取得特定交易記錄
        /// </summary>
        /// <param name="id">交易記錄ID</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>交易記錄</returns>
        Task<TransactionDto> GetTransactionByIdAsync(int id, int userId);

        /// <summary>
        /// 建立新的交易記錄
        /// </summary>
        /// <param name="dto">建立交易記錄的資料</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>建立成功的交易記錄</returns>
        Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto, int userId);

        /// <summary>
        /// 更新現有交易記錄
        /// </summary>
        /// <param name="id">交易記錄ID</param>
        /// <param name="dto">更新的資料</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>更新後的交易記錄</returns>
        Task<TransactionDto> UpdateTransactionAsync(int id, UpdateTransactionDto dto, int userId);

        /// <summary>
        /// 刪除交易記錄
        /// </summary>
        /// <param name="id">交易記錄ID</param>
        /// <param name="userId">使用者ID</param>
        Task DeleteTransactionAsync(int id, int userId);

        /// <summary>
        /// 取得指定月份的統計資料
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="year">年份</param>
        /// <param name="month">月份</param>
        /// <returns>統計資料</returns>
        Task<object> GetStatisticsAsync(int userId, int year, int month);

        /// <summary>
        /// 取得使用者的交易記錄總數
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <returns>交易記錄總數</returns>
        Task<int> GetTransactionCountAsync(int userId);

        /// <summary>
        /// 搜尋交易記錄
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="keyword">搜尋關鍵字</param>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>符合條件的交易記錄</returns>
        Task<IEnumerable<TransactionDto>> SearchTransactionsAsync(int userId, string keyword, int page, int pageSize);
    }
}