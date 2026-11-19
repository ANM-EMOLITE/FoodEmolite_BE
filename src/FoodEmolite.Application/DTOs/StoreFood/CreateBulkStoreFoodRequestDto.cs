namespace FoodEmolite.Application.DTOs.StoreFood
{
    public class CreateBulkStoreFoodRequestDto
    {
        public long StoreFoodCategoryId { get; set; }

        public List<string> FoodNames { get; set; } = new();
    }
}
