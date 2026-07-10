using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Settings;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý cấu hình chung của nhà hàng (restaurant_settings) — bản ghi singleton.
///
/// Chịu trách nhiệm:
///   - Lấy cấu hình hiện tại (tự tạo mặc định nếu bảng rỗng — xem SettingsRepository.GetSingletonAsync).
///   - Cập nhật partial: chỉ field nào client gửi mới bị thay đổi.
///
/// Dependency: IUnitOfWork (Settings).
/// </summary>
public class SettingsService
{
    private readonly IUnitOfWork _uow;

    public SettingsService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SettingsResponse> GetAsync()
    {
        var settings = await _uow.Settings.GetSingletonAsync();
        return MapToResponse(settings);
    }

    public async Task<SettingsResponse> UpdateAsync(UpdateSettingsRequest request)
    {
        var settings = await _uow.Settings.GetSingletonAsync();

        if (!string.IsNullOrWhiteSpace(request.RestaurantName))
            settings.RestaurantName = request.RestaurantName;
        else if (request.RestaurantName != null)
            throw new BusinessRuleViolationException(ValidationMessages.SETTINGS_NAME_REQUIRED);

        if (request.Address != null)
            settings.Address = request.Address;

        if (request.Phone != null)
            settings.Phone = request.Phone;

        if (request.OpeningTime != null)
            settings.OpeningTime = ParseTime(request.OpeningTime);

        if (request.ClosingTime != null)
            settings.ClosingTime = ParseTime(request.ClosingTime);

        if (request.TaxRate.HasValue)
            settings.TaxRate = ValidateRate(request.TaxRate.Value);

        if (request.ServiceChargeRate.HasValue)
            settings.ServiceChargeRate = ValidateRate(request.ServiceChargeRate.Value);

        await _uow.SaveChangesAsync();

        return MapToResponse(settings);
    }

    private static TimeSpan ParseTime(string value)
    {
        if (!TimeSpan.TryParse(value, out var time))
            throw new BusinessRuleViolationException(ValidationMessages.SETTINGS_TIME_INVALID);
        return time;
    }

    private static decimal ValidateRate(decimal rate)
    {
        if (rate < 0 || rate > 100)
            throw new BusinessRuleViolationException(ValidationMessages.SETTINGS_RATE_INVALID);
        return rate;
    }

    private static SettingsResponse MapToResponse(RestaurantSettings settings) => new()
    {
        Id = settings.Id,
        RestaurantName = settings.RestaurantName,
        Address = settings.Address,
        Phone = settings.Phone,
        OpeningTime = settings.OpeningTime.ToString(@"hh\:mm"),
        ClosingTime = settings.ClosingTime.ToString(@"hh\:mm"),
        TaxRate = settings.TaxRate,
        ServiceChargeRate = settings.ServiceChargeRate,
        UpdatedAt = settings.UpdatedAt
    };
}
