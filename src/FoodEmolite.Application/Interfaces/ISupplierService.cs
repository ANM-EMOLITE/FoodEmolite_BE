using FoodEmolite.Application.DTOs.Supplier;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface ISupplierService
{
    Task<BaseTableResponse<SupplierResponseDto>> SearchAsync(long currentUserId, BaseSearchRequest<SupplierSearchRequest> request);

    Task<BaseResponse<SupplierResponseDto>> GetDetailAsync(long currentUserId, long id);

    Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, SaveSupplierRequestDto request);

    Task<BaseResponse<string>> UpdateAsync(long currentUserId, long id, SaveSupplierRequestDto request);

    Task<BaseResponse<string>> DeleteAsync(long currentUserId, long id);
}
