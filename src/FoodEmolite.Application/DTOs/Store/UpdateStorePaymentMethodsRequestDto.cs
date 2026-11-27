namespace FoodEmolite.Application.DTOs.Store;

public class UpdateStorePaymentMethodsRequestDto
{
    public bool AllowCash { get; set; }
    public bool AllowBankTransfer { get; set; }
}
