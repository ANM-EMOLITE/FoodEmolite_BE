using FoodEmolite.Shared.Common;
using FoodEmolite.Application.DTOs.StoreFood;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Helpers;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class StoreFoodService : IStoreFoodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IActivityLogService _activityLogService;

    public StoreFoodService(
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        IActivityLogService activityLogService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _activityLogService = activityLogService;
    }

    public async Task<BaseResponse<string>> CreateAsync(string refCode, CreateStoreFoodRequestDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.RefCode == request.StoreRefCode &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        string productCode;

        if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            productCode = request.ProductCode.Trim();

            var codeExisted = await repoStoreFood.AnyAsync(x =>
                x.StoreRefCode == request.StoreRefCode &&
                x.ProductCode == productCode &&
                !x.IsDeleted);

            if (codeExisted)
                return BaseResponse<string>.Fail("Mã sản phẩm đã tồn tại");
        }
        else
        {
            productCode = await GenerateNextProductCodeAsync(repoStoreFood, request.StoreRefCode);
        }

        string? thumbnailFileRefCode = null;

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadProductImageAsync(request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            thumbnailFileRefCode = uploadResult.Data;
        }

        var storeFood = new StoreFood
        {
            RefCode = refCode,
            StoreRefCode = request.StoreRefCode,
            FoodName = request.FoodName,
            ProductCode = productCode,
            ThumbnailUrl = thumbnailFileRefCode,
            Description = request.Description,
            Price = request.Price,
            Quantity = request.Quantity,
            StoreFoodCategoryId = request.StoreFoodCategoryId,
            IsAvailable = true,
            IsDeleted = false,
            CreatedAt = DateTimeHelper.VnNow
        };

        await repoStoreFood.AddAsync(storeFood);
        await _unitOfWork.SaveChangesAsync();

        if (request.OptionGroups != null && request.OptionGroups.Any())
        {
            foreach (var groupRequest in request.OptionGroups)
            {
                var optionGroup = new StoreFoodOptionGroup
                {
                    RefCode = refCode,
                    StoreFoodId = storeFood.Id,
                    GroupName = groupRequest.GroupName,
                    IsRequired = groupRequest.IsRequired,
                    MinSelect = groupRequest.MinSelect,
                    MaxSelect = groupRequest.MaxSelect,
                    SortOrder = groupRequest.SortOrder,
                    IsDeleted = false,
                    CreatedAt = DateTimeHelper.VnNow
                };

                await repoOptionGroup.AddAsync(optionGroup);
                await _unitOfWork.SaveChangesAsync();

                foreach (var optionRequest in groupRequest.Options)
                {
                    var option = new StoreFoodOption
                    {
                        RefCode = refCode,
                        OptionGroupId = optionGroup.Id,
                        OptionName = optionRequest.OptionName,
                        AdditionalPrice = optionRequest.AdditionalPrice,
                        IsAvailable = optionRequest.IsAvailable,
                        SortOrder = optionRequest.SortOrder,
                        IsDeleted = false,
                        CreatedAt = DateTimeHelper.VnNow
                    };

                    await repoOption.AddAsync(option);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        await _activityLogService.LogAgentActionAsync(null, refCode, "CREATE_FOOD", $"Thêm món \"{storeFood.FoodName}\"", storeFood.StoreRefCode);

        return BaseResponse<string>.Success("Create store food successfully");
    }

    public async Task<BaseResponse<string>> UpdateAsync(string refCode, long id, UpdateStoreFoodRequestDto request)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<string>.Fail("Store food not found");

        var oldFoodName = storeFood.FoodName;
        var changes = new ChangeSummary();

        if (string.IsNullOrWhiteSpace(request.ProductCode))
            return BaseResponse<string>.Fail("Mã sản phẩm không được để trống");

        var newProductCode = request.ProductCode.Trim();

        var productCodeExisted = await repoStoreFood.AnyAsync(x =>
            x.StoreRefCode == storeFood.StoreRefCode &&
            x.ProductCode == newProductCode &&
            x.Id != id &&
            !x.IsDeleted);

        if (productCodeExisted)
            return BaseResponse<string>.Fail("Mã sản phẩm đã tồn tại");

        changes.Text("Mã sản phẩm", storeFood.ProductCode, newProductCode);

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadProductImageAsync(request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            storeFood.ThumbnailUrl = uploadResult.Data;
            changes.Note("Đổi ảnh món");
        }

        changes
            .Text("Tên", storeFood.FoodName, request.FoodName)
            .Text("Mô tả", storeFood.Description, request.Description)
            .Money("Giá", storeFood.Price, request.Price)
            .Number("Số lượng", storeFood.Quantity, request.Quantity)
            .Flag("Trạng thái", storeFood.IsAvailable, request.IsAvailable, "Đang bán", "Ngừng bán");

        if (storeFood.StoreFoodCategoryId != request.StoreFoodCategoryId)
        {
            var repoCategory = _unitOfWork.GetRepository<StoreFoodCategories>();
            var oldCategory = await repoCategory.FirstOrDefaultAsync(x => x.Id == storeFood.StoreFoodCategoryId);
            var newCategory = await repoCategory.FirstOrDefaultAsync(x => x.Id == request.StoreFoodCategoryId);

            changes.Text("Danh mục", oldCategory?.CategoryName, newCategory?.CategoryName);
        }

        storeFood.FoodName = request.FoodName;
        storeFood.ProductCode = newProductCode;
        storeFood.Description = request.Description;
        storeFood.Price = request.Price;
        storeFood.Quantity = request.Quantity;
        storeFood.IsAvailable = request.IsAvailable;
        storeFood.StoreFoodCategoryId = request.StoreFoodCategoryId;
        storeFood.UpdatedAt = DateTimeHelper.VnNow;

        repoStoreFood.Update(storeFood);

        if (request.OptionGroups != null && request.OptionGroups.Any())
        {
            var requestGroupIds = request.OptionGroups
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .Distinct()
                .ToList();

            var oldGroups = await repoOptionGroup.Query()
                .Where(x =>
                    x.StoreFoodId == storeFood.Id &&
                    requestGroupIds.Contains(x.Id))
                .ToListAsync();

            var oldGroupIds = oldGroups
                .Select(x => x.Id)
                .ToList();

            var requestOptionIds = request.OptionGroups
                .SelectMany(x => x.Options ?? new List<StoreFoodOptionRequestDto>())
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .Distinct()
                .ToList();

            var oldOptions = await repoOption.Query()
                .Where(x =>
                    oldGroupIds.Contains(x.OptionGroupId) ||
                    requestOptionIds.Contains(x.Id))
                .ToListAsync();

            foreach (var groupRequest in request.OptionGroups)
            {
                StoreFoodOptionGroup? optionGroup = null;

                if (groupRequest.Id.HasValue)
                {
                    optionGroup = oldGroups.FirstOrDefault(x => x.Id == groupRequest.Id.Value);

                    if (optionGroup is null)
                        continue;

                    if (groupRequest.IsDeleted)
                    {
                        if (!optionGroup.IsDeleted)
                            changes.Note($"Xoá nhóm tuỳ chọn \"{optionGroup.GroupName}\"");
                    }
                    else
                    {
                        var groupLabel = $"Nhóm tuỳ chọn \"{optionGroup.GroupName}\"";

                        changes
                            .Text($"{groupLabel} - tên", optionGroup.GroupName, groupRequest.GroupName)
                            .Flag($"{groupLabel} - bắt buộc chọn", optionGroup.IsRequired, groupRequest.IsRequired, "Có", "Không")
                            .Number($"{groupLabel} - chọn tối thiểu", optionGroup.MinSelect, groupRequest.MinSelect)
                            .Number($"{groupLabel} - chọn tối đa", optionGroup.MaxSelect, groupRequest.MaxSelect);
                    }

                    optionGroup.GroupName = groupRequest.GroupName;
                    optionGroup.IsRequired = groupRequest.IsRequired;
                    optionGroup.MinSelect = groupRequest.MinSelect;
                    optionGroup.MaxSelect = groupRequest.MaxSelect;
                    optionGroup.SortOrder = groupRequest.SortOrder;
                    optionGroup.IsDeleted = groupRequest.IsDeleted;
                    optionGroup.UpdatedAt = DateTimeHelper.VnNow;

                    repoOptionGroup.Update(optionGroup);

                    if (groupRequest.IsDeleted)
                    {
                        var optionsOfDeletedGroup = oldOptions
                            .Where(x => x.OptionGroupId == optionGroup.Id)
                            .ToList();

                        foreach (var option in optionsOfDeletedGroup)
                        {
                            option.IsDeleted = true;
                            option.UpdatedAt = DateTimeHelper.VnNow;
                            repoOption.Update(option);
                        }

                        continue;
                    }
                }
                else
                {
                    if (groupRequest.IsDeleted)
                        continue;

                    changes.Note($"Thêm nhóm tuỳ chọn \"{groupRequest.GroupName}\"");

                    optionGroup = new StoreFoodOptionGroup
                    {
                        RefCode = refCode,
                        StoreFoodId = storeFood.Id,
                        GroupName = groupRequest.GroupName,
                        IsRequired = groupRequest.IsRequired,
                        MinSelect = groupRequest.MinSelect,
                        MaxSelect = groupRequest.MaxSelect,
                        SortOrder = groupRequest.SortOrder,
                        IsDeleted = false,
                        CreatedAt = DateTimeHelper.VnNow
                    };

                    await repoOptionGroup.AddAsync(optionGroup);
                    await _unitOfWork.SaveChangesAsync();
                }

                if (groupRequest.Options == null || !groupRequest.Options.Any())
                    continue;

                foreach (var optionRequest in groupRequest.Options)
                {
                    if (optionRequest.Id.HasValue)
                    {
                        var option = oldOptions.FirstOrDefault(x => x.Id == optionRequest.Id.Value);

                        if (option is null)
                            continue;

                        if (optionRequest.IsDeleted)
                        {
                            if (!option.IsDeleted)
                                changes.Note($"Xoá tuỳ chọn \"{option.OptionName}\" khỏi nhóm \"{optionGroup?.GroupName}\"");
                        }
                        else
                        {
                            var optionLabel = $"Tuỳ chọn \"{option.OptionName}\"";

                            changes
                                .Text($"{optionLabel} - tên", option.OptionName, optionRequest.OptionName)
                                .Money($"{optionLabel} - giá cộng thêm", option.AdditionalPrice, optionRequest.AdditionalPrice)
                                .Flag($"{optionLabel} - trạng thái", option.IsAvailable, optionRequest.IsAvailable, "Đang bán", "Ngừng bán");
                        }

                        option.OptionName = optionRequest.OptionName;
                        option.AdditionalPrice = optionRequest.AdditionalPrice;
                        option.IsAvailable = optionRequest.IsAvailable;
                        option.SortOrder = optionRequest.SortOrder;
                        option.IsDeleted = optionRequest.IsDeleted;
                        option.UpdatedAt = DateTimeHelper.VnNow;

                        repoOption.Update(option);
                    }
                    else
                    {
                        if (optionRequest.IsDeleted)
                            continue;

                        changes.Note($"Thêm tuỳ chọn \"{optionRequest.OptionName}\" (+{optionRequest.AdditionalPrice:N0}đ) vào nhóm \"{optionGroup?.GroupName}\"");

                        var option = new StoreFoodOption
                        {
                            RefCode = refCode,
                            OptionGroupId = optionGroup.Id,
                            OptionName = optionRequest.OptionName,
                            AdditionalPrice = optionRequest.AdditionalPrice,
                            IsAvailable = optionRequest.IsAvailable,
                            SortOrder = optionRequest.SortOrder,
                            IsDeleted = false,
                            CreatedAt = DateTimeHelper.VnNow
                        };

                        await repoOption.AddAsync(option);
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(null, refCode, "UPDATE_FOOD", changes.Describe($"Cập nhật món \"{oldFoodName}\""), storeFood.StoreRefCode);

        return BaseResponse<string>.Success("Update store food successfully");
    }

    public async Task<BaseResponse<string>> DeleteAsync(string refCode, long id)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<string>.Fail("Store food not found");

        storeFood.IsDeleted = true;
        storeFood.UpdatedAt = DateTimeHelper.VnNow;

        var groups = await repoOptionGroup.Query()
            .Where(x => x.StoreFoodId == storeFood.Id && !x.IsDeleted)
            .ToListAsync();

        var groupIds = groups.Select(x => x.Id).ToList();

        var options = await repoOption.Query()
            .Where(x => groupIds.Contains(x.OptionGroupId) && !x.IsDeleted)
            .ToListAsync();

        foreach (var option in options)
        {
            option.IsDeleted = true;
            option.UpdatedAt = DateTimeHelper.VnNow;
            repoOption.Update(option);
        }

        foreach (var group in groups)
        {
            group.IsDeleted = true;
            group.UpdatedAt = DateTimeHelper.VnNow;
            repoOptionGroup.Update(group);
        }

        repoStoreFood.Update(storeFood);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(null, refCode, "DELETE_FOOD", $"Xoá món \"{storeFood.FoodName}\"", storeFood.StoreRefCode);

        return BaseResponse<string>.Success("Delete store food successfully");
    }

    public async Task<BaseTableResponse<StoreFoodResponseDto>> GetAllAsync(int page, int pageSize, string? storeRefCode = null, string? keyword = null)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query =
            from storeFood in repoStoreFood.Query().AsNoTracking()
            join store in repoStore.Query().AsNoTracking()
                on storeFood.StoreRefCode equals store.RefCode
            where !storeFood.IsDeleted
            select new
            {
                StoreFood = storeFood,
                StoreName = store.StoreName
            };

        if (!string.IsNullOrWhiteSpace(storeRefCode))
        {
            query = query.Where(x => x.StoreFood.StoreRefCode == storeRefCode);
        }

        var trimmedKeyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x => x.StoreFood.FoodName.Contains(trimmedKeyword));
        }

        var totalRecords = await query.CountAsync();

        var foods = await query
            .OrderByDescending(x => x.StoreFood.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var foodIds = foods
            .Select(x => x.StoreFood.Id)
            .ToList();

        var optionGroups = await repoOptionGroup.Query()
            .AsNoTracking()
            .Where(x =>
                foodIds.Contains(x.StoreFoodId) &&
                !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var groupIds = optionGroups
            .Select(x => x.Id)
            .ToList();

        var options = await repoOption.Query()
            .AsNoTracking()
            .Where(x =>
                groupIds.Contains(x.OptionGroupId) &&
                !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var items = foods.Select(x => new StoreFoodResponseDto
        {
            Id = x.StoreFood.Id,
            RefCode = x.StoreFood.RefCode,
            StoreRefCode = x.StoreFood.StoreRefCode,
            StoreName = x.StoreName,
            StoreFoodCategoryId = x.StoreFood.StoreFoodCategoryId,
            FoodName = x.StoreFood.FoodName,
            ProductCode = x.StoreFood.ProductCode,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(x.StoreFood.ThumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(x.StoreFood.ThumbnailUrl)
                : null,
            Description = x.StoreFood.Description,
            Price = x.StoreFood.Price,
            Quantity = x.StoreFood.Quantity,
            IsAvailable = x.StoreFood.IsAvailable,
            OptionGroups = optionGroups
                .Where(group => group.StoreFoodId == x.StoreFood.Id)
                .Select(group => new StoreFoodOptionGroupResponseDto
                {
                    Id = group.Id,
                    RefCode = group.RefCode,
                    GroupName = group.GroupName,
                    IsRequired = group.IsRequired,
                    MinSelect = group.MinSelect,
                    MaxSelect = group.MaxSelect,
                    SortOrder = group.SortOrder,
                    Options = options
                        .Where(option => option.OptionGroupId == group.Id)
                        .Select(option => new StoreFoodOptionResponseDto
                        {
                            Id = option.Id,
                            RefCode = option.RefCode,
                            OptionName = option.OptionName,
                            AdditionalPrice = option.AdditionalPrice,
                            IsAvailable = option.IsAvailable,
                            SortOrder = option.SortOrder
                        })
                        .ToList()
                })
                .ToList()
        }).ToList();

        return new BaseTableResponse<StoreFoodResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<StoreFoodResponseDto>> GetByStoreRefCodeAsync(BaseSearchRequest<GetStoreFoodsRequest> request)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var searchParams = request.SearchParams;

        if (searchParams is null || string.IsNullOrWhiteSpace(searchParams.StoreRefCode))
            return new BaseTableResponse<StoreFoodResponseDto>
            {
                Items = new List<StoreFoodResponseDto>(),
                Page = 1,
                PageSize = request.PageSize,
                TotalRecords = 0
            };

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var query = repoStoreFood
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.StoreRefCode == searchParams.StoreRefCode &&
                !x.IsDeleted);

        if (searchParams.StoreFoodCategoryId.HasValue)
        {
            query = query.Where(x =>
                x.StoreFoodCategoryId == searchParams.StoreFoodCategoryId.Value);
        }

        if (searchParams.IsAvailable.HasValue)
        {
            query = query.Where(x => x.IsAvailable == searchParams.IsAvailable.Value);
        }

        var totalRecords = await query.CountAsync();

        query = request.SortBy switch
        {
            "foodName" => request.Asc
                ? query.OrderBy(x => x.FoodName)
                : query.OrderByDescending(x => x.FoodName),

            "price" => request.Asc
                ? query.OrderBy(x => x.Price)
                : query.OrderByDescending(x => x.Price),

            "quantity" => request.Asc
                ? query.OrderBy(x => x.Quantity)
                : query.OrderByDescending(x => x.Quantity),

            _ => query.OrderByDescending(x => x.Id)
        };

        var foods = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var foodIds = foods
            .Select(x => x.Id)
            .ToList();

        var optionGroups = await repoOptionGroup.Query()
            .AsNoTracking()
            .Where(x =>
                foodIds.Contains(x.StoreFoodId) &&
                !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var groupIds = optionGroups
            .Select(x => x.Id)
            .ToList();

        var options = await repoOption.Query()
            .AsNoTracking()
            .Where(x =>
                groupIds.Contains(x.OptionGroupId) &&
                !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var items = foods.Select(food => new StoreFoodResponseDto
        {
            Id = food.Id,
            RefCode = food.RefCode,
            StoreRefCode = food.StoreRefCode,
            StoreFoodCategoryId = food.StoreFoodCategoryId,
            FoodName = food.FoodName,
            ProductCode = food.ProductCode,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(food.ThumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(food.ThumbnailUrl)
                : null,
            Description = food.Description,
            Price = food.Price,
            Quantity = food.Quantity,
            IsAvailable = food.IsAvailable,
            OptionGroups = optionGroups
                .Where(group => group.StoreFoodId == food.Id)
                .Select(group => new StoreFoodOptionGroupResponseDto
                {
                    Id = group.Id,
                    RefCode = group.RefCode,
                    GroupName = group.GroupName,
                    IsRequired = group.IsRequired,
                    MinSelect = group.MinSelect,
                    MaxSelect = group.MaxSelect,
                    SortOrder = group.SortOrder,
                    Options = options
                        .Where(option => option.OptionGroupId == group.Id)
                        .Select(option => new StoreFoodOptionResponseDto
                        {
                            Id = option.Id,
                            RefCode = option.RefCode,
                            OptionName = option.OptionName,
                            AdditionalPrice = option.AdditionalPrice,
                            IsAvailable = option.IsAvailable,
                            SortOrder = option.SortOrder
                        })
                        .ToList()
                })
                .ToList()
        }).ToList();

        return new BaseTableResponse<StoreFoodResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<StoreFoodResponseDto>> GetDetailAsync(long id)
    {
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoOptionGroup = _unitOfWork.GetRepository<StoreFoodOptionGroup>();
        var repoOption = _unitOfWork.GetRepository<StoreFoodOption>();

        var storeFood = await repoStoreFood.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (storeFood is null)
            return BaseResponse<StoreFoodResponseDto>.Fail("Store food not found");

        var optionGroups = await repoOptionGroup.Query()
            .AsNoTracking()
            .Where(x => x.StoreFoodId == storeFood.Id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var groupIds = optionGroups.Select(x => x.Id).ToList();

        var options = await repoOption.Query()
            .AsNoTracking()
            .Where(x => groupIds.Contains(x.OptionGroupId) && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var response = new StoreFoodResponseDto
        {
            Id = storeFood.Id,
            RefCode = storeFood.RefCode,
            StoreRefCode = storeFood.StoreRefCode,
            FoodName = storeFood.FoodName,
            ProductCode = storeFood.ProductCode,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(storeFood.ThumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(storeFood.ThumbnailUrl)
                : null,
            Description = storeFood.Description,
            Price = storeFood.Price,
            Quantity = storeFood.Quantity,
            IsAvailable = storeFood.IsAvailable,
            StoreFoodCategoryId = storeFood.StoreFoodCategoryId,
            OptionGroups = optionGroups.Select(group => new StoreFoodOptionGroupResponseDto
            {
                Id = group.Id,
                RefCode = group.RefCode,
                GroupName = group.GroupName,
                IsRequired = group.IsRequired,
                MinSelect = group.MinSelect,
                MaxSelect = group.MaxSelect,
                SortOrder = group.SortOrder,
                Options = options
                    .Where(option => option.OptionGroupId == group.Id)
                    .Select(option => new StoreFoodOptionResponseDto
                    {
                        Id = option.Id,
                        RefCode = option.RefCode,
                        OptionName = option.OptionName,
                        AdditionalPrice = option.AdditionalPrice,
                        IsAvailable = option.IsAvailable,
                        SortOrder = option.SortOrder
                    })
                    .ToList()
            }).ToList()
        };

        return BaseResponse<StoreFoodResponseDto>.Success(response);
    }

    private static async Task<string> GenerateNextProductCodeAsync(IRepository<StoreFood> repoStoreFood, string storeRefCode)
    {
        const string prefix = "SP";

        var existingCodes = await repoStoreFood.Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == storeRefCode && x.ProductCode != null && x.ProductCode.StartsWith(prefix))
            .Select(x => x.ProductCode)
            .ToListAsync();

        var maxNumber = existingCodes
            .Select(code => int.TryParse(code.Substring(prefix.Length), out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxNumber + 1):D5}";
    }
}   