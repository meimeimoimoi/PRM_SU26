using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Menu;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;

namespace SmartDine.Menu.API.Controllers;

/// <summary>
/// Controller quản lý danh mục món ăn (categories) — dùng cho manager dashboard.
///
/// Endpoints:
///   GET    /api/v1/menu-categories       → Danh sách danh mục (public, giống menu-items).
///   GET    /api/v1/menu-categories/{id}  → Chi tiết 1 danh mục (public).
///   POST   /api/v1/menu-categories       → Tạo danh mục mới (MANAGER).
///   PATCH  /api/v1/menu-categories/{id}  → Cập nhật danh mục (MANAGER).
///   DELETE /api/v1/menu-categories/{id}  → Xóa danh mục, chặn nếu còn món ăn (MANAGER).
/// </summary>
[ApiController]
[Route("api/v1/menu-categories")]
public class MenuCategoriesController : ControllerBase
{
    private readonly MenuCategoryService _categoryService;

    public MenuCategoriesController(MenuCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllAsync();
        return Ok(ApiResponse<List<MenuCategoryResponse>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _categoryService.GetByIdAsync(id);
        return Ok(ApiResponse<MenuCategoryResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Create([FromBody] CreateMenuCategoryRequest request)
    {
        var result = await _categoryService.CreateAsync(request);
        return Created("", ApiResponse<MenuCategoryResponse>.Ok(result, ValidationMessages.CATEGORY_CREATED_SUCCESS));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchMenuCategoryRequest request)
    {
        var result = await _categoryService.PatchAsync(id, request);
        return Ok(ApiResponse<MenuCategoryResponse>.Ok(result, ValidationMessages.CATEGORY_UPDATED_SUCCESS));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        await _categoryService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, ValidationMessages.CATEGORY_DELETED_SUCCESS));
    }
}
