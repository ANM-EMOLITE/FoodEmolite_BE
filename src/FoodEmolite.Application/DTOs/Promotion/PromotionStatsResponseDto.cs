namespace FoodEmolite.Application.DTOs.Promotion;

/// <summary>Thống kê hiệu quả của 1 chương trình khuyến mãi — chỉ tính các đơn chưa huỷ.</summary>
public class PromotionStatsResponseDto
{
    /// <summary>Số đơn có ít nhất 1 món được áp chương trình.</summary>
    public int OrderCount { get; set; }

    /// <summary>Số lượt sử dụng = tổng số lượng món được áp chương trình (quà tặng cũng tính).</summary>
    public int UsageCount { get; set; }

    /// <summary>Tổng doanh thu (TotalAmount) của các đơn có sử dụng chương trình.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Doanh thu của các đơn sử dụng chương trình đã thanh toán.</summary>
    public decimal PaidRevenue { get; set; }

    /// <summary>Tổng số tiền đã giảm cho khách: Σ (giá gốc − giá bán) × số lượng trên các món được áp.</summary>
    public decimal DiscountAmount { get; set; }
}
