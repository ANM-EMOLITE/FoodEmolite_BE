namespace FoodEmolite.Application.DTOs.Order
{
    public class UpdatePayLaterPriceRequestDto
    {
        public List<PayLaterItemPriceDto> Items { get; set; } = new();
    }

    public class PayLaterItemPriceDto
    {
        public long OrderItemId { get; set; }

        public decimal UnitPrice { get; set; }
    }
}
