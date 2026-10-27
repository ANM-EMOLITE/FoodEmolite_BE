using FoodEmolite.Application.DTOs.ActivityLog;
using FoodEmolite.Application.DTOs.Admin;
using FoodEmolite.Application.DTOs.Auth;
using FoodEmolite.Application.DTOs.Customer;
using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Application.DTOs.Profile;
using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.DTOs.Store;
using FoodEmolite.Application.DTOs.StoreFood;
using FoodEmolite.Application.DTOs.StoreFoodCategories;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.ExternalService.Interfaces;

public interface IAdminService
{
    Task<BaseResponse<string>> CreateStoreAsync(CreateStoreRequestDto request);

    Task<BaseResponse<string>> UpdateStoreAsync(long id, UpdateStoreRequestDto request);

    Task<BaseResponse<string>> SetStoreApprovalAsync(long id, bool isApproved);

    Task<BaseResponse<string>> DeleteStoreAsync(long id);

    Task<BaseTableResponse<StoreResponseDto>> SearchStoresAsync(BaseSearchRequest<AdminStoreSearchRequest> request);

    Task<BaseResponse<StoreResponseDto>> GetStoreDetailAsync(long id);

    Task<BaseTableResponse<StoreResponseDto>> SearchStoresByOwnerAsync(BaseSearchRequest<AdminStoreByOwnerSearchRequest> request);

    Task<BaseTableResponse<UserProfileResponseDto>> SearchUsersAsync(BaseSearchRequest<AdminUserSearchRequest> request);

    Task<BaseTableResponse<MyProfileResponseDto>> SearchAgentsAsync(BaseSearchRequest<AdminAgentSearchRequest> request);

    Task<BaseResponse<string>> CreateAgentAsync(RegisterRequest request);

    Task<BaseResponse<AdminRevenueResponseDto>> GetRevenueAsync(RevenueQueryDto request);

    Task<BaseResponse<List<TopSellingProductDto>>> GetTopProductsAsync(RevenueQueryDto request, string? storeRefCode, int top = 10);

    Task<BaseTableResponse<TopSellingProductDto>> SearchProductRevenueAsync(BaseSearchRequest<ProductRevenueSearchRequest> request);

    Task<BaseTableResponse<CustomerListItemDto>> SearchCustomersAsync(BaseSearchRequest<CustomerSearchRequest> request);

    Task<BaseTableResponse<StoreFoodResponseDto>> SearchStoreFoodsAsync(BaseSearchRequest<AdminStoreFoodSearchRequest> request);

    Task<BaseTableResponse<StoreFoodCategoryResponseDto>> SearchCategoriesAsync(BaseSearchRequest<AdminCategorySearchRequest> request);

    Task<BaseTableResponse<OrderResponseDto>> SearchOrdersAsync(BaseSearchRequest<OrderSearchRequest> request);

    Task<BaseTableResponse<ActivityLogResponseDto>> SearchActivityLogsAsync(BaseSearchRequest<ActivityLogSearchRequest> request);
}
