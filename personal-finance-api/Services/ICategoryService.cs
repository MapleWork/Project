using PersonalFinanceApi.DTOs;

namespace PersonalFinanceApi.Services
{
    /// <summary>
    /// 分類服務介面，定義分類管理相關操作
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// 取得使用者的所有分類
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <returns>分類列表</returns>
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync(int userId);

        /// <summary>
        /// 根據ID取得特定分類
        /// </summary>
        /// <param name="id">分類ID</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>分類資料</returns>
        Task<CategoryDto> GetCategoryByIdAsync(int id, int userId);

        /// <summary>
        /// 建立新分類
        /// </summary>
        /// <param name="dto">建立分類的資料</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>建立成功的分類</returns>
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, int userId);

        /// <summary>
        /// 更新現有分類
        /// </summary>
        /// <param name="id">分類ID</param>
        /// <param name="dto">更新的資料</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>更新後的分類</returns>
        Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto dto, int userId);

        /// <summary>
        /// 刪除分類
        /// </summary>
        /// <param name="id">分類ID</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>刪除結果</returns>
        Task DeleteCategoryAsync(int id, int userId);

        /// <summary>
        /// 取得指定類型的分類
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="type">交易類型 (Income, Expense)</param>
        /// <returns>指定類型的分類清單</returns>
        Task<IEnumerable<CategoryDto>> GetCategoriesByTypeAsync(int userId, string type);

        /// <summary>
        /// 檢查分類是否可以刪除
        /// </summary>
        /// <param name="id">分類ID</param>
        /// <param name="userId">使用者ID</param>
        /// <returns>是否可以刪除的檢查結果</returns>
        Task<bool> CanDeleteCategoryAsync(int categoryId, int userId);

        /// <summary>
        /// 取得分類的使用統計
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <returns>分類使用統計資料</returns>
        Task<object> GetCategoryUsageStatisticsAsync(int categoryId, int userId);
    }
}