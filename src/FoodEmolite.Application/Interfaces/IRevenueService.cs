using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.Interfaces
{
    public interface IRevenueService
    {
        Task<BaseResponse<AgentRevenueResponseDto>> GetAgentRevenueAsync(long currentUserId, RevenueQueryDto request);

        /// <summary>Top N món bán chạy nhất (theo số lượng bán) trong khoảng ngày, chỉ tính đơn đã thanh toán.</summary>
        Task<BaseResponse<List<TopSellingProductDto>>> GetAgentTopSellingProductsAsync(long currentUserId, RevenueQueryDto request, int top = 10);

        /// <summary>Doanh thu theo từng món (phân trang/tìm kiếm/sắp xếp), chỉ tính đơn đã thanh toán.</summary>
        Task<BaseTableResponse<TopSellingProductDto>> GetAgentProductRevenueAsync(long currentUserId, BaseSearchRequest<ProductRevenueSearchRequest> request);
    }
}
