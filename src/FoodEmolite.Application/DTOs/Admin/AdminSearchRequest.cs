namespace FoodEmolite.Application.DTOs.Admin;

public class AdminStoreSearchRequest
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
}

public class AdminStoreByOwnerSearchRequest
{
    public string OwnerRefCode { get; set; } = string.Empty;
}

public class AdminUserSearchRequest
{
    public string? Keyword { get; set; }
}

public class AdminAgentSearchRequest
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
}

public class AdminStoreFoodSearchRequest
{
    public string? Keyword { get; set; }
    public string? StoreRefCode { get; set; }
}

public class AdminCategorySearchRequest
{
    public string? Keyword { get; set; }
    public string? StoreRefCode { get; set; }
}
