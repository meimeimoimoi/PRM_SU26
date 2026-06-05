using SmartDine.Application.DTOs.Menu;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý CRUD cho menu items và categories.
/// </summary>
public class MenuService
{
    private readonly IUnitOfWork _uow;
    private readonly IMenuItemRepository _menuItemRepo;

    public MenuService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<MenuItemResponse>> GetAllAsync()
    {
        var items = await _uow.MenuItems.GetAvailableAsync();
        return items.Select(MapToResponse).ToList();
    }

    public async Task<MenuItemResponse?> GetByIdAsync(int id)
    {
        var item = await _uow.MenuItems.GetByIdAsync(id);
        return item == null ? null : MapToResponse(item);
    }

    public async Task<MenuItemResponse> CreateAsync(CreateMenuItemRequest request)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên món ăn không được để trống.");

        if (request.Price <= 0)
            throw new ArgumentException("Giá món ăn phải lớn hơn 0.");

        // Kiểm tra category tồn tại
        var category = await _menuItemRepo.GetByCategoryIdAsync(request.CategoryId);
    
        if (category == null)
            throw new KeyNotFoundException("Danh mục không tồn tại.");

        var item = new MenuItem
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            ImageUrl = ,
            CategoryId = request.CategoryId,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.MenuItems.AddAsync(item);
        await _uow.SaveChangesAsync();

        return MapToResponse(item);
    }

    public async Task<MenuItemResponse> UpdateAsync(int id, UpdateMenuItemRequest request)
    {
        var item = await _uow.MenuItems.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuItem", id);

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.ImageUrl = request.ImageUrl;
        item.CategoryId = request.CategoryId;
        item.IsAvailable = request.IsAvailable;

        await _uow.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task DeleteAsync(int id)
    {
        await _uow.MenuItems.DeleteAsync(id);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<MenuItemResponse>> SearchAsync(string query)
    {
        var items = await _uow.MenuItems.SearchAsync(query);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<List<MenuItemResponse>> GetPopularAsync(int count = 10)
    {
        var items = await _uow.MenuItems.GetPopularAsync(count);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<MenuItemResponse> ToggleAvailabilityAsync(int id)
    {
        var item = await _uow.MenuItems.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuItem", id);

        item.IsAvailable = !item.IsAvailable;
        await _uow.SaveChangesAsync();
        return MapToResponse(item);
    }

    private static MenuItemResponse MapToResponse(MenuItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Price = item.Price,
        ImageUrl = item.ImageUrl,
        CategoryId = item.CategoryId,
        CategoryName = item.Category?.Name,
        IsAvailable = item.IsAvailable
    };
}
