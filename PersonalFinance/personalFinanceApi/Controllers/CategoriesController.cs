using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PersonalFinanceApi.DTOs;
using PersonalFinanceApi.Services;

namespace PersonalFinanceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// 取得所有分類
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var userId = GetUserId();
                var categories = await _categoryService.GetCategoriesAsync(userId);
                return Ok(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "取得分類清單失敗" });
            }
        }

        /// <summary>
        /// 取得單一分類
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var userId = GetUserId();
                var category = await _categoryService.GetCategoryByIdAsync(id, userId);
                return Ok(new { success = true, data = category });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "取得分類失敗" });
            }
        }

        /// <summary>
        /// 建立新分類
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            try
            {
                var userId = GetUserId();
                var category = await _categoryService.CreateCategoryAsync(dto, userId);

                return CreatedAtAction(
                    nameof(GetCategory),
                    new { id = category.Id },
                    new { success = true, data = category });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "建立分類失敗" });
            }
        }

        /// <summary>
        /// 更新分類
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            try
            {
                var userId = GetUserId();
                var category = await _categoryService.UpdateCategoryAsync(id, dto, userId);
                return Ok(new { success = true, data = category });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "更新分類失敗" });
            }
        }

        /// <summary>
        /// 刪除分類
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var userId = GetUserId();
                await _categoryService.DeleteCategoryAsync(id, userId);
                return Ok(new { success = true, message = "分類已刪除" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "刪除分類失敗" });
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