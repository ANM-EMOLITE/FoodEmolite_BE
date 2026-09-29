using FoodEmolite.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Application.DTOs.Order
{
    public class OrderSearchRequest
    {
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StoreRefCode { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }

        /// <summary>Trạng thái gộp hiển thị cho đại lý: UNPAID | PAID | CANCELLED (đơn đã huỷ luôn tính là CANCELLED).</summary>
        public string? Status { get; set; }

        /// <summary>Loại khuyến mãi đã áp trong đơn: FIXED_PRICE | PRODUCT_DISCOUNT | BUY_X_GET_Y | NONE (đơn không có khuyến mãi).</summary>
        public string? PromotionType { get; set; }

        /// <summary>Từ khoá lọc theo tên / mã khuyến mãi đã áp trong đơn.</summary>
        public string? PromotionKeyword { get; set; }

        /// <summary>Nguồn đơn: POS | WEB_USER | WEB_GUEST.</summary>
        public OrderSource? OrderSource { get; set; }
    }
}
