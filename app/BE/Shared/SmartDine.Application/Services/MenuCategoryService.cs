using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Menu;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ quản lý danh mục món ăn (menu categories).
///
/// Chịu trách nhiệm:
///   - Danh sách danh mục kèm số lượng món ăn (ItemCount).
///   - Tạo/sửa danh mục (Manager only), chặn trùng tên (case-insensitive).
///   - Xóa danh mục — chặn nếu vẫn còn món ăn thuộc danh mục đó.
///
/// Dependency: IUnitOfWork (MenuCategories, MenuItems).
/// </summary>
public class MenuCategoryService
{
    private readonly IUnitOfWork _uow;

    public MenuCategoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<MenuCategoryResponse>> GetAllAsync()
    {
        var categories = await _uow.MenuCategories.GetAllAsync();
        var result = new List<MenuCategoryResponse>();

        foreach (var category in categories)
        {
            var items = await _uow.MenuItems.GetByCategoryIdAsync(category.Id);
            result.Add(MapToResponse(category, items.Count));
        }

        return result;
    }

    public async Task<MenuCategoryResponse> GetByIdAsync(int id)
    {
        var category = await _uow.MenuCategories.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuCategory", id);

        var items = await _uow.MenuItems.GetByCategoryIdAsync(id);
        return MapToResponse(category, items.Count);
    }

    public async Task<MenuCategoryResponse> CreateAsync(CreateMenuCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleViolationException(ValidationMessages.CATEGORY_NAME_REQUIRED);

        var all = await _uow.MenuCategories.GetAllAsync();
        if (all.Any(c => c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.CATEGORY_NAME_ALREADY_EXISTS, request.Name));

        var category = new MenuCategory
        {
            Name = request.Name,
            Description = request.Description
        };

        await _uow.MenuCategories.AddAsync(category);
        await _uow.SaveChangesAsync();

        return MapToResponse(category, 0);
    }

    public async Task<MenuCategoryResponse> PatchAsync(int id, PatchMenuCategoryRequest request)
    {
        var category = await _uow.MenuCategories.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuCategory", id);

        if (!string.IsNullOrWhiteSpace(request.Name) && !request.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase))
        {
            var all = await _uow.MenuCategories.GetAllAsync();
            if (all.Any(c => c.Id != id && c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleViolationException(
                    string.Format(ValidationMessages.CATEGORY_NAME_ALREADY_EXISTS, request.Name));

            category.Name = request.Name;
        }

        if (request.Description != null)
            category.Description = request.Description;

        await _uow.SaveChangesAsync();

        var items = await _uow.MenuItems.GetByCategoryIdAsync(id);
        return MapToResponse(category, items.Count);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _uow.MenuCategories.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuCategory", id);

        var items = await _uow.MenuItems.GetByCategoryIdAsync(id);
        if (items.Count > 0)
            throw new BusinessRuleViolationException(ValidationMessages.CATEGORY_HAS_MENU_ITEMS);

        await _uow.MenuCategories.DeleteAsync(id);
        await _uow.SaveChangesAsync();
    }

    private static MenuCategoryResponse MapToResponse(MenuCategory category, int itemCount) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        ItemCount = itemCount
    };
}
