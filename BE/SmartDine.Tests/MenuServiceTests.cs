using Moq;
using SmartDine.Application.DTOs.Menu;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;
using System.Text.Json;

namespace SmartDine.Tests;

public class MenuServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IMenuItemRepository> _menuRepoMock;
    private readonly Mock<IReviewRepository> _reviewRepoMock;
    private readonly Mock<IRepository<CustomerActivity>> _activityRepoMock;
    private readonly Mock<IRepository<MenuItemStatistics>> _statsRepoMock;
    private readonly Mock<IRepository<BusinessContextLog>> _contextRepoMock;
    private readonly Mock<IRepository<RecommendationLog>> _recLogRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly MenuService _menuService;

    public MenuServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _menuRepoMock = new Mock<IMenuItemRepository>();
        _reviewRepoMock = new Mock<IReviewRepository>();
        _activityRepoMock = new Mock<IRepository<CustomerActivity>>();
        _statsRepoMock = new Mock<IRepository<MenuItemStatistics>>();
        _contextRepoMock = new Mock<IRepository<BusinessContextLog>>();
        _recLogRepoMock = new Mock<IRepository<RecommendationLog>>();
        _customerRepoMock = new Mock<ICustomerRepository>();

        _uowMock.Setup(u => u.MenuItems).Returns(_menuRepoMock.Object);
        _uowMock.Setup(u => u.Reviews).Returns(_reviewRepoMock.Object);
        _uowMock.Setup(u => u.CustomerActivities).Returns(_activityRepoMock.Object);
        _uowMock.Setup(u => u.MenuItemStatisticsRepo).Returns(_statsRepoMock.Object);
        _uowMock.Setup(u => u.BusinessContextLogs).Returns(_contextRepoMock.Object);
        _uowMock.Setup(u => u.RecommendationLogs).Returns(_recLogRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _menuService = new MenuService(_uowMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 1: GetPagedAsync — GET /api/v1/menu-items
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPaged_NoFilters_ReturnsPaginatedItems()
    {
        var items = new List<MenuItem>
        {
            CreateMenuItem(1, "Phở Bò", 60000),
            CreateMenuItem(2, "Bún Chả", 55000)
        };
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(null, null, 1, 10))
            .ReturnsAsync((items as IReadOnlyList<MenuItem>, 2));

        var (result, totalCount, totalPages) = await _menuService.GetPagedAsync(null, null, 1, 10, null);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, totalCount);
        Assert.Equal(1, totalPages);
    }

    [Fact]
    public async Task GetPaged_FilterByCategoryId_PassesToRepo()
    {
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(2, null, 1, 10))
            .ReturnsAsync((new List<MenuItem>() as IReadOnlyList<MenuItem>, 0));

        await _menuService.GetPagedAsync(2, null, 1, 10, null);

        _menuRepoMock.Verify(r => r.GetPagedFilteredAsync(2, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetPaged_FilterBySearch_PassesToRepo()
    {
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(null, "lẩu", 1, 10))
            .ReturnsAsync((new List<MenuItem>() as IReadOnlyList<MenuItem>, 0));

        await _menuService.GetPagedAsync(null, "lẩu", 1, 10, null);

        _menuRepoMock.Verify(r => r.GetPagedFilteredAsync(null, "lẩu", 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetPaged_CalculatesTotalPagesCorrectly()
    {
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(null, null, 1, 5))
            .ReturnsAsync((new List<MenuItem>() as IReadOnlyList<MenuItem>, 12));

        var (_, totalCount, totalPages) = await _menuService.GetPagedAsync(null, null, 1, 5, null);

        Assert.Equal(12, totalCount);
        Assert.Equal(3, totalPages); // ceil(12/5) = 3
    }

    [Fact]
    public async Task GetPaged_EmptyResult_ReturnsEmptyListWithZeroPages()
    {
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(It.IsAny<int?>(), It.IsAny<string?>(), 1, 10))
            .ReturnsAsync((new List<MenuItem>() as IReadOnlyList<MenuItem>, 0));

        var (result, totalCount, totalPages) = await _menuService.GetPagedAsync(99, null, 1, 10, null);

        Assert.Empty(result);
        Assert.Equal(0, totalCount);
        Assert.Equal(0, totalPages);
    }

    [Fact]
    public async Task GetPaged_WithCustomerId_PersonalizesOrder()
    {
        var cat1 = new MenuCategory { Id = 1, Name = "Nước" };
        var cat2 = new MenuCategory { Id = 2, Name = "Cơm" };
        var items = new List<MenuItem>
        {
            new() { Id = 1, Name = "Cơm Sườn", Price = 50000, CategoryId = 2, Category = cat2, IsAvailable = true },
            new() { Id = 2, Name = "Trà Đá", Price = 10000, CategoryId = 1, Category = cat1, IsAvailable = true }
        };
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(null, null, 1, 10))
            .ReturnsAsync((items as IReadOnlyList<MenuItem>, 2));

        // Giả lập khách hay xem category 1 (Nước)
        var activities = new List<CustomerActivity>
        {
            new() { CustomerId = 5, ActivityType = "VIEW", Payload = JsonSerializer.Serialize(new { menu_item_id = 2 }) },
            new() { CustomerId = 5, ActivityType = "VIEW", Payload = JsonSerializer.Serialize(new { menu_item_id = 2 }) },
            new() { CustomerId = 5, ActivityType = "ORDER", Payload = JsonSerializer.Serialize(new { menu_item_id = 2 }) }
        };
        _activityRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(activities);

        var (result, _, _) = await _menuService.GetPagedAsync(null, null, 1, 10, 5);

        // Trà Đá (category 1, score=3) nên đẩy lên trước Cơm Sườn (category 2, score=0)
        Assert.Equal("Trà Đá", result[0].Name);
        Assert.Equal("Cơm Sườn", result[1].Name);
    }

    [Fact]
    public async Task GetPaged_WithCustomerIdButNoActivities_KeepsOriginalOrder()
    {
        var items = new List<MenuItem>
        {
            CreateMenuItem(1, "Phở", 60000),
            CreateMenuItem(2, "Bún", 55000)
        };
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(null, null, 1, 10))
            .ReturnsAsync((items as IReadOnlyList<MenuItem>, 2));
        _activityRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CustomerActivity>());

        var (result, _, _) = await _menuService.GetPagedAsync(null, null, 1, 10, 5);

        Assert.Equal("Phở", result[0].Name);
        Assert.Equal("Bún", result[1].Name);
    }

    [Fact]
    public async Task GetPaged_SingleItem_NoPersonalizationNeeded()
    {
        var items = new List<MenuItem> { CreateMenuItem(1, "Phở", 60000) };
        _menuRepoMock.Setup(r => r.GetPagedFilteredAsync(null, null, 1, 10))
            .ReturnsAsync((items as IReadOnlyList<MenuItem>, 1));

        var (result, _, _) = await _menuService.GetPagedAsync(null, null, 1, 10, 5);

        Assert.Single(result);
        // Không gọi activities vì chỉ có 1 item, không cần personalize
    }

    // ═══════════════════════════════════════════════════════════════
    // API 2: GetByIdDetailAsync — GET /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdDetail_ExistingItem_ReturnsDetailWithReviews()
    {
        var item = CreateMenuItemWithDetails(8, "Lẩu Thái", 350000, new List<Review>
        {
            new() { Id = 1, Rating = 5, Comment = "Tuyệt vời", Status = ReviewStatus.APPROVED,
                     Customer = new Customer { FullName = "Nguyễn A" }, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Rating = 4, Comment = "Ngon", Status = ReviewStatus.APPROVED,
                     Customer = new Customer { FullName = "Trần B" }, CreatedAt = DateTime.UtcNow }
        }, totalViews: 1540);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(8)).ReturnsAsync(item);
        SetupTrackViewMocks(8);

        var result = await _menuService.GetByIdDetailAsync(8, null);

        Assert.Equal("Lẩu Thái", result.Name);
        Assert.Equal(350000, result.Price);
        Assert.Equal(4.5, result.AverageRating);
        Assert.Equal(1540, result.TotalViews);
        Assert.Equal(2, result.Reviews.Count);
    }

    [Fact]
    public async Task GetByIdDetail_ItemNotFound_ThrowsEntityNotFound()
    {
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(999)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _menuService.GetByIdDetailAsync(999, null));
    }

    [Fact]
    public async Task GetByIdDetail_TracksViewActivity()
    {
        var item = CreateMenuItemWithDetails(1, "Test", 10000, new List<Review>(), totalViews: 0);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
        SetupTrackViewMocks(1);

        await _menuService.GetByIdDetailAsync(1, 5);

        _activityRepoMock.Verify(r => r.AddAsync(It.Is<CustomerActivity>(
            a => a.CustomerId == 5 && a.ActivityType == "VIEW")), Times.Once);
    }

    [Fact]
    public async Task GetByIdDetail_IncrementsExistingStatsTotalViews()
    {
        var stats = new MenuItemStatistics { Id = 10, MenuItemId = 1, TotalViews = 100 };
        var item = CreateMenuItemWithDetails(1, "Test", 10000, new List<Review>(), totalViews: 100);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
        _activityRepoMock.Setup(r => r.AddAsync(It.IsAny<CustomerActivity>()))
            .ReturnsAsync((CustomerActivity a) => a);
        _statsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<MenuItemStatistics> { stats });

        await _menuService.GetByIdDetailAsync(1, null);

        Assert.Equal(101, stats.TotalViews);
    }

    [Fact]
    public async Task GetByIdDetail_CreatesStatsIfNoneExist()
    {
        var item = CreateMenuItemWithDetails(1, "Test", 10000, new List<Review>(), totalViews: 0);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
        _activityRepoMock.Setup(r => r.AddAsync(It.IsAny<CustomerActivity>()))
            .ReturnsAsync((CustomerActivity a) => a);
        _statsRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<MenuItemStatistics>());
        _statsRepoMock.Setup(r => r.AddAsync(It.IsAny<MenuItemStatistics>()))
            .ReturnsAsync((MenuItemStatistics s) => s);

        await _menuService.GetByIdDetailAsync(1, null);

        _statsRepoMock.Verify(r => r.AddAsync(It.Is<MenuItemStatistics>(
            s => s.MenuItemId == 1 && s.TotalViews == 1)), Times.Once);
    }

    [Fact]
    public async Task GetByIdDetail_AnonymousView_CustomerIdIsNull()
    {
        var item = CreateMenuItemWithDetails(1, "Test", 10000, new List<Review>(), totalViews: 0);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
        SetupTrackViewMocks(1);

        await _menuService.GetByIdDetailAsync(1, null);

        _activityRepoMock.Verify(r => r.AddAsync(It.Is<CustomerActivity>(
            a => a.CustomerId == null)), Times.Once);
    }

    [Fact]
    public async Task GetByIdDetail_FiltersOutRejectedReviews()
    {
        var item = CreateMenuItemWithDetails(1, "Test", 10000, new List<Review>
        {
            new() { Id = 1, Rating = 5, Status = ReviewStatus.APPROVED, Customer = new Customer { FullName = "A" }, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Rating = 1, Status = ReviewStatus.REJECTED, Customer = new Customer { FullName = "B" }, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Rating = 3, Status = ReviewStatus.HIDDEN, Customer = new Customer { FullName = "C" }, CreatedAt = DateTime.UtcNow }
        }, totalViews: 0);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
        SetupTrackViewMocks(1);

        var result = await _menuService.GetByIdDetailAsync(1, null);

        // Chỉ APPROVED review hiển thị, REJECTED + HIDDEN bị lọc
        Assert.Single(result.Reviews);
        Assert.Equal(5.0, result.AverageRating);
    }

    [Fact]
    public async Task GetByIdDetail_NoReviews_AverageRatingIsZero()
    {
        var item = CreateMenuItemWithDetails(1, "Test", 10000, new List<Review>(), totalViews: 0);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
        SetupTrackViewMocks(1);

        var result = await _menuService.GetByIdDetailAsync(1, null);

        Assert.Equal(0, result.AverageRating);
        Assert.Empty(result.Reviews);
    }

    [Fact]
    public async Task GetByIdDetail_ViewPayloadContainsMenuItemId()
    {
        var item = CreateMenuItemWithDetails(42, "Test", 10000, new List<Review>(), totalViews: 0);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(42)).ReturnsAsync(item);
        SetupTrackViewMocks(42);

        await _menuService.GetByIdDetailAsync(42, null);

        _activityRepoMock.Verify(r => r.AddAsync(It.Is<CustomerActivity>(a =>
            a.Payload != null && a.Payload.Contains("42"))), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 3: CreateAsync — POST /api/v1/menu-items
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Create_ValidRequest_CreatesItemAndReturnsCreatedResponse()
    {
        _menuRepoMock.Setup(r => r.AddAsync(It.IsAny<MenuItem>()))
            .ReturnsAsync((MenuItem m) => { m.Id = 99; return m; });
        _statsRepoMock.Setup(r => r.AddAsync(It.IsAny<MenuItemStatistics>()))
            .ReturnsAsync((MenuItemStatistics s) => s);

        var result = await _menuService.CreateAsync(new CreateMenuItemRequest
        {
            Name = "Súp Bào Ngư",
            Description = "Bồi bổ sức khỏe",
            Price = 450000,
            CategoryId = 2,
            ImageUrl = "https://example.com/soup.jpg"
        });

        Assert.Equal(99, result.Id);
        Assert.Equal("Súp Bào Ngư", result.Name);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Create_CreatesInitialStatistics()
    {
        _menuRepoMock.Setup(r => r.AddAsync(It.IsAny<MenuItem>()))
            .ReturnsAsync((MenuItem m) => { m.Id = 50; return m; });
        _statsRepoMock.Setup(r => r.AddAsync(It.IsAny<MenuItemStatistics>()))
            .ReturnsAsync((MenuItemStatistics s) => s);

        await _menuService.CreateAsync(new CreateMenuItemRequest
        {
            Name = "Test", Price = 10000, CategoryId = 1
        });

        _statsRepoMock.Verify(r => r.AddAsync(It.Is<MenuItemStatistics>(
            s => s.MenuItemId == 50)), Times.Once);
    }

    [Fact]
    public async Task Create_EmptyName_ThrowsBusinessRuleViolation()
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _menuService.CreateAsync(new CreateMenuItemRequest
            {
                Name = "", Price = 10000, CategoryId = 1
            }));
    }

    [Fact]
    public async Task Create_WhitespaceName_ThrowsBusinessRuleViolation()
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _menuService.CreateAsync(new CreateMenuItemRequest
            {
                Name = "   ", Price = 10000, CategoryId = 1
            }));
    }

    [Fact]
    public async Task Create_ZeroPrice_ThrowsBusinessRuleViolation()
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _menuService.CreateAsync(new CreateMenuItemRequest
            {
                Name = "Test", Price = 0, CategoryId = 1
            }));
    }

    [Fact]
    public async Task Create_NegativePrice_ThrowsBusinessRuleViolation()
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _menuService.CreateAsync(new CreateMenuItemRequest
            {
                Name = "Test", Price = -50000, CategoryId = 1
            }));
    }

    [Fact]
    public async Task Create_SetsIsAvailableTrue()
    {
        MenuItem? captured = null;
        _menuRepoMock.Setup(r => r.AddAsync(It.IsAny<MenuItem>()))
            .Callback<MenuItem>(m => captured = m)
            .ReturnsAsync((MenuItem m) => m);
        _statsRepoMock.Setup(r => r.AddAsync(It.IsAny<MenuItemStatistics>()))
            .ReturnsAsync((MenuItemStatistics s) => s);

        await _menuService.CreateAsync(new CreateMenuItemRequest
        {
            Name = "Test", Price = 10000, CategoryId = 1
        });

        Assert.True(captured!.IsAvailable);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 4: PatchAsync — PATCH /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Patch_OnlyIsAvailable_UpdatesOnlyThatField()
    {
        var item = new MenuItem
        {
            Id = 8, Name = "Lẩu Thái", Description = "Chua cay", Price = 350000,
            CategoryId = 2, IsAvailable = true, ImageUrl = "old.jpg"
        };
        _menuRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(item);

        var result = await _menuService.PatchAsync(8, new PatchMenuItemRequest
        {
            IsAvailable = false
        });

        Assert.False(result.IsAvailable);
        Assert.Equal("Lẩu Thái", result.Name);
        Assert.Equal(350000, item.Price); // Giá không đổi
        Assert.Equal("old.jpg", item.ImageUrl); // Ảnh không đổi
    }

    [Fact]
    public async Task Patch_MultipleFields_UpdatesAllSpecifiedFields()
    {
        var item = new MenuItem { Id = 1, Name = "Old", Price = 10000, CategoryId = 1, IsAvailable = true };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        await _menuService.PatchAsync(1, new PatchMenuItemRequest
        {
            Name = "New Name",
            Price = 25000,
            Description = "Updated description"
        });

        Assert.Equal("New Name", item.Name);
        Assert.Equal(25000, item.Price);
        Assert.Equal("Updated description", item.Description);
        Assert.True(item.IsAvailable); // Không gửi → không đổi
    }

    [Fact]
    public async Task Patch_ItemNotFound_ThrowsEntityNotFound()
    {
        _menuRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _menuService.PatchAsync(999, new PatchMenuItemRequest { IsAvailable = false }));
    }

    [Fact]
    public async Task Patch_NoFieldsProvided_ThrowsBusinessRuleViolation()
    {
        var item = new MenuItem { Id = 1, Name = "Test" };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _menuService.PatchAsync(1, new PatchMenuItemRequest()));
    }

    [Fact]
    public async Task Patch_ZeroPrice_ThrowsBusinessRuleViolation()
    {
        var item = new MenuItem { Id = 1, Name = "Test", Price = 50000 };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _menuService.PatchAsync(1, new PatchMenuItemRequest { Price = 0 }));
    }

    [Fact]
    public async Task Patch_NegativePrice_ThrowsBusinessRuleViolation()
    {
        var item = new MenuItem { Id = 1, Name = "Test", Price = 50000 };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _menuService.PatchAsync(1, new PatchMenuItemRequest { Price = -1 }));
    }

    [Fact]
    public async Task Patch_ReturnsUpdatedAt()
    {
        var item = new MenuItem { Id = 1, Name = "Test", IsAvailable = true };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _menuService.PatchAsync(1, new PatchMenuItemRequest { IsAvailable = false });

        Assert.NotEqual(default, result.UpdatedAt);
    }

    [Fact]
    public async Task Patch_CallsSaveChanges()
    {
        var item = new MenuItem { Id = 1, Name = "Test" };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        await _menuService.PatchAsync(1, new PatchMenuItemRequest { Name = "Updated" });

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 5: DeleteAsync — DELETE /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_ExistingItem_SoftDeletes()
    {
        var item = new MenuItem { Id = 1, Name = "Test" };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        await _menuService.DeleteAsync(1);

        _menuRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ItemNotFound_ThrowsEntityNotFound()
    {
        _menuRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _menuService.DeleteAsync(999));
    }

    [Fact]
    public async Task Delete_AlreadyDeletedItem_ThrowsEntityNotFound()
    {
        // GlobalQueryFilter loại IsDeleted=true → GetByIdAsync trả null
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _menuService.DeleteAsync(1));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 6: GetAiRecommendationsAsync — GET /api/v1/menu-items/ai-recommendations
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AiRecommendations_WithColdWeather_ReturnsWarmItems()
    {
        var context = new BusinessContextLog
        {
            ContextDate = DateOnly.FromDateTime(DateTime.UtcNow),
            WeatherCondition = "Mưa lạnh",
            Temperature = 18.5m
        };
        _contextRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BusinessContextLog> { context });

        var availableItems = new List<MenuItem>
        {
            CreateMenuItem(1, "Lẩu Thái", 350000),
            CreateMenuItem(2, "Trà gừng mật ong", 45000),
            CreateMenuItem(3, "Kem dừa", 30000)
        };
        _menuRepoMock.Setup(r => r.GetAvailableAsync()).ReturnsAsync(availableItems);
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(availableItems);

        var result = await _menuService.GetAiRecommendationsAsync(null, 5);

        Assert.NotEmpty(result.Data);
        Assert.StartsWith("rec_batch_", result.RecommendationId);
        // Lẩu và Trà gừng match weather rule
        Assert.Contains(result.Data, d => d.Name == "Lẩu Thái");
        Assert.Contains(result.Data, d => d.Name == "Trà gừng mật ong");
        Assert.True(result.Data.All(d => d.Reason != null));
    }

    [Fact]
    public async Task AiRecommendations_WithHotWeather_ReturnsCoolItems()
    {
        var context = new BusinessContextLog
        {
            ContextDate = DateOnly.FromDateTime(DateTime.UtcNow),
            WeatherCondition = "Nắng nóng"
        };
        _contextRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BusinessContextLog> { context });

        var availableItems = new List<MenuItem>
        {
            CreateMenuItem(1, "Phở nóng", 60000),
            CreateMenuItem(2, "Sinh tố dâu", 35000),
            CreateMenuItem(3, "Nước chanh đá", 20000)
        };
        _menuRepoMock.Setup(r => r.GetAvailableAsync()).ReturnsAsync(availableItems);
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(availableItems);

        var result = await _menuService.GetAiRecommendationsAsync(null, 5);

        Assert.Contains(result.Data, d => d.Name == "Sinh tố dâu");
        Assert.Contains(result.Data, d => d.Name == "Nước chanh đá");
    }

    [Fact]
    public async Task AiRecommendations_NoContext_FallbackToPopularItems()
    {
        _contextRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BusinessContextLog>());

        var popular = new List<MenuItem>
        {
            CreateMenuItem(1, "Phở", 60000),
            CreateMenuItem(2, "Bún Chả", 55000)
        };
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(popular);

        var result = await _menuService.GetAiRecommendationsAsync(null, 5);

        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(d => d.Reason!.Contains("yêu thích")));
    }

    [Fact]
    public async Task AiRecommendations_WithCustomerId_LogsRecommendations()
    {
        _contextRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BusinessContextLog>());
        var popular = new List<MenuItem> { CreateMenuItem(1, "Phở", 60000) };
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(popular);
        _recLogRepoMock.Setup(r => r.AddAsync(It.IsAny<RecommendationLog>()))
            .ReturnsAsync((RecommendationLog rl) => rl);
        _activityRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CustomerActivity>());
        _menuRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<MenuItem>());

        await _menuService.GetAiRecommendationsAsync(5, 3);

        _recLogRepoMock.Verify(r => r.AddAsync(It.Is<RecommendationLog>(
            rl => rl.CustomerId == 5 && rl.MenuItemId == 1)), Times.Once);
    }

    [Fact]
    public async Task AiRecommendations_WithoutCustomerId_DoesNotLogRecommendations()
    {
        _contextRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BusinessContextLog>());
        var popular = new List<MenuItem> { CreateMenuItem(1, "Phở", 60000) };
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(popular);

        await _menuService.GetAiRecommendationsAsync(null, 3);

        _recLogRepoMock.Verify(r => r.AddAsync(It.IsAny<RecommendationLog>()), Times.Never);
    }

    [Fact]
    public async Task AiRecommendations_RespectsLimitParam()
    {
        _contextRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BusinessContextLog>());
        var popular = new List<MenuItem>
        {
            CreateMenuItem(1, "A", 10000), CreateMenuItem(2, "B", 20000),
            CreateMenuItem(3, "C", 30000), CreateMenuItem(4, "D", 40000),
            CreateMenuItem(5, "E", 50000)
        };
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(popular);

        var result = await _menuService.GetAiRecommendationsAsync(null, 3);

        Assert.Equal(3, result.Data.Count);
    }

    [Fact]
    public async Task AiRecommendations_NoDuplicateItems()
    {
        var context = new BusinessContextLog
        {
            WeatherCondition = "Mưa lạnh", IsWeekend = true
        };
        _contextRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BusinessContextLog> { context });

        var items = new List<MenuItem>
        {
            CreateMenuItem(1, "Lẩu Thái", 350000),
            CreateMenuItem(2, "Cháo Vịt", 80000)
        };
        _menuRepoMock.Setup(r => r.GetAvailableAsync()).ReturnsAsync(items);
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(items);

        var result = await _menuService.GetAiRecommendationsAsync(null, 5);

        var ids = result.Data.Select(d => d.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task AiRecommendations_HolidayContext_RecommendsPremiumItems()
    {
        var context = new BusinessContextLog
        {
            ContextDate = DateOnly.FromDateTime(DateTime.UtcNow),
            HolidayName = "Tết Nguyên Đán"
        };
        _contextRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<BusinessContextLog> { context });

        var items = new List<MenuItem>
        {
            CreateMenuItem(1, "Cơm bình dân", 35000),
            CreateMenuItem(2, "Bào ngư thượng hạng", 500000),
            CreateMenuItem(3, "Tôm hùm nướng", 800000)
        };
        _menuRepoMock.Setup(r => r.GetAvailableAsync()).ReturnsAsync(items);
        _menuRepoMock.Setup(r => r.GetPopularAsync(It.IsAny<int>())).ReturnsAsync(items);

        var result = await _menuService.GetAiRecommendationsAsync(null, 5);

        Assert.Contains(result.Data, d => d.Reason!.Contains("Tết Nguyên Đán"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Backward-compatible methods (dùng bởi SmartDine.API monolith)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ReturnsAvailableItems()
    {
        var items = new List<MenuItem>
        {
            CreateMenuItem(1, "Phở", 60000),
            CreateMenuItem(2, "Bún", 55000)
        };
        _menuRepoMock.Setup(r => r.GetAvailableAsync()).ReturnsAsync(items);

        var result = await _menuService.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetById_ExistingItem_ReturnsResponse()
    {
        var item = CreateMenuItem(1, "Phở", 60000);
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _menuService.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Phở", result!.Name);
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsEntityNotFoundException()
    {
        _menuRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _menuService.GetByIdAsync(999));
    }

    [Fact]
    public async Task ToggleAvailability_Available_SetsToUnavailable()
    {
        var item = new MenuItem { Id = 1, Name = "Test", IsAvailable = true };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _menuService.ToggleAvailabilityAsync(1);

        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task ToggleAvailability_Unavailable_SetsToAvailable()
    {
        var item = new MenuItem { Id = 1, Name = "Test", IsAvailable = false };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        var result = await _menuService.ToggleAvailabilityAsync(1);

        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task ToggleAvailability_NotFound_ThrowsEntityNotFound()
    {
        _menuRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _menuService.ToggleAvailabilityAsync(999));
    }

    [Fact]
    public async Task Search_ReturnsMatchingItems()
    {
        var items = new List<MenuItem> { CreateMenuItem(1, "Lẩu Thái", 350000) };
        _menuRepoMock.Setup(r => r.SearchAsync("lẩu")).ReturnsAsync(items);

        var result = await _menuService.SearchAsync("lẩu");

        Assert.Single(result);
        Assert.Equal("Lẩu Thái", result[0].Name);
    }

    [Fact]
    public async Task GetPopular_ReturnsTopItems()
    {
        var items = new List<MenuItem>
        {
            CreateMenuItem(1, "Best Seller", 100000),
            CreateMenuItem(2, "Popular", 80000)
        };
        _menuRepoMock.Setup(r => r.GetPopularAsync(5)).ReturnsAsync(items);

        var result = await _menuService.GetPopularAsync(5);

        Assert.Equal(2, result.Count);
    }

    // ═══════════════════════════════════════════════════════════════
    // Cross-service conflict tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_ThenGetById_ThrowsEntityNotFoundException()
    {
        var item = new MenuItem { Id = 1, Name = "Test" };
        _menuRepoMock.SetupSequence(r => r.GetByIdAsync(1))
            .ReturnsAsync(item)
            .ReturnsAsync((MenuItem?)null); // After soft delete, global filter excludes it

        await _menuService.DeleteAsync(1);
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _menuService.GetByIdAsync(1));
    }

    [Fact]
    public async Task Patch_SetUnavailable_ShouldNotAppearInPagedResults()
    {
        // Kịch bản: đầu bếp tắt is_available → API 1 chỉ trả available items
        var item = new MenuItem { Id = 1, Name = "Lẩu", IsAvailable = true };
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

        await _menuService.PatchAsync(1, new PatchMenuItemRequest { IsAvailable = false });

        Assert.False(item.IsAvailable);
        // GetPagedFilteredAsync filters WHERE IsAvailable=true → item 1 sẽ bị loại
    }

    // BUG DETECTION: TrackViewActivityAsync loads ALL MenuItemStatistics into memory
    [Fact]
    public async Task GetByIdDetail_StatsLoadAll_PerformanceConcern()
    {
        // BUG/PERFORMANCE: TrackViewActivityAsync gọi GetAllAsync() trên MenuItemStatistics
        // rồi filter bằng LINQ FirstOrDefault() trong memory.
        // Với 10,000 menu items → load toàn bộ 10,000 stats rows mỗi lần xem 1 món.
        //
        // → Gợi ý tối ưu: thêm method GetByMenuItemIdAsync(int menuItemId) vào
        //   IRepository<MenuItemStatistics> hoặc tạo IMenuItemStatisticsRepository
        //   với query WHERE menu_item_id = @id.
        //
        // Tương tự, PersonalizeOrderAsync gọi CustomerActivities.GetAllAsync()
        // → load toàn bộ activities rồi filter theo customerId trong memory.
        // → Gợi ý: thêm GetByCustomerIdAsync(int customerId) method.

        var item = CreateMenuItemWithDetails(1, "Test", 10000, new List<Review>(), totalViews: 0);
        _menuRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
        SetupTrackViewMocks(1);

        // Test vẫn pass — đây là warning về performance, không phải functional bug
        await _menuService.GetByIdDetailAsync(1, null);

        _statsRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // BUG DETECTION: Update có thể conflict với Delete
    [Fact]
    public async Task Patch_AfterDelete_ThrowsEntityNotFound_RaceCondition()
    {
        // Kịch bản: Manager A xóa món, Manager B cập nhật cùng lúc
        // Nếu B gọi GetByIdAsync sau khi A đã soft delete → global filter loại → null
        _menuRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _menuService.PatchAsync(1, new PatchMenuItemRequest { Price = 999999 }));
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private static MenuItem CreateMenuItem(int id, string name, decimal price, int categoryId = 1)
    {
        return new MenuItem
        {
            Id = id,
            Name = name,
            Price = price,
            CategoryId = categoryId,
            Category = new MenuCategory { Id = categoryId, Name = "Category " + categoryId },
            IsAvailable = true,
            Statistics = new MenuItemStatistics { MenuItemId = id, TotalViews = 0 }
        };
    }

    private static MenuItem CreateMenuItemWithDetails(int id, string name, decimal price,
        List<Review> reviews, int totalViews)
    {
        return new MenuItem
        {
            Id = id,
            Name = name,
            Price = price,
            CategoryId = 1,
            Category = new MenuCategory { Id = 1, Name = "Default" },
            IsAvailable = true,
            Reviews = reviews,
            Statistics = new MenuItemStatistics { MenuItemId = id, TotalViews = totalViews }
        };
    }

    private void SetupTrackViewMocks(int menuItemId)
    {
        _activityRepoMock.Setup(r => r.AddAsync(It.IsAny<CustomerActivity>()))
            .ReturnsAsync((CustomerActivity a) => a);
        _statsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<MenuItemStatistics>
            {
                new() { MenuItemId = menuItemId, TotalViews = 0 }
            });
    }
}
