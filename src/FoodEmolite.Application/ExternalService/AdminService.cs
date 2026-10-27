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
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Helpers;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Enums;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Common;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.ExternalService;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IActivityLogService _activityLogService;

    public AdminService(
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        IActivityLogService activityLogService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _activityLogService = activityLogService;
    }

    public async Task<BaseResponse<string>> CreateStoreAsync(CreateStoreRequestDto request)
    {
        long currentUserId = 0;

        var repoStore = _unitOfWork.GetRepository<Store>();

        string? thumbnailFileRefCode = null;

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadStoreImageAsync(
                request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            thumbnailFileRefCode = uploadResult.Data;
        }

        var store = new Store
        {
            RefCode = Guid.NewGuid().ToString().ToUpper(),
            StoreName = request.StoreName,
            OwnerAccountId = request.OwnerAccountId,
            ThumbnailUrl = thumbnailFileRefCode,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Description = request.Description,
            IsActive = true,
            IsApproved = true,
            IsDeleted = false,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        };

        await repoStore.AddAsync(store);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAsync(
            currentUserId > 0 ? "Agent" : "System",
            currentUserId > 0 ? currentUserId : null,
            null,
            "CREATE_STORE",
            $"Tạo cửa hàng \"{store.StoreName}\"",
            store.RefCode);

        return BaseResponse<string>.Success("Create store successfully");
    }

    public async Task<BaseResponse<string>> UpdateStoreAsync(long id, UpdateStoreRequestDto request)
    {
        long currentUserId = 0;

        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.Id == id &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        var oldStoreName = store.StoreName;
        var changes = new ChangeSummary();

        if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
        {
            var uploadResult = await _cloudinaryService.UploadStoreImageAsync(
                request.ThumbnailFile);

            if (!uploadResult.IsSuccess)
                return BaseResponse<string>.Fail(uploadResult.Message);

            store.ThumbnailUrl = uploadResult.Data;
            changes.Note("Đổi ảnh cửa hàng");
        }

        changes
            .Text("Tên", store.StoreName, request.StoreName)
            .Text("Số điện thoại", store.PhoneNumber, request.PhoneNumber)
            .Text("Địa chỉ", store.Address, request.Address)
            .Text("Mô tả", store.Description, request.Description)
            .Flag("Trạng thái", store.IsActive, request.IsActive, "Đang hoạt động", "Ngừng hoạt động");

        store.StoreName = request.StoreName;
        store.PhoneNumber = request.PhoneNumber;
        store.Address = request.Address;
        store.Description = request.Description;
        store.IsActive = request.IsActive;
        store.UpdatedAt = DateTimeHelper.VnNow;
        store.UpdatedBy = currentUserId;

        repoStore.Update(store);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(currentUserId, null, "UPDATE_STORE", changes.Describe($"Cập nhật thông tin cửa hàng \"{oldStoreName}\""), store.RefCode);

        return BaseResponse<string>.Success("Update store successfully");
    }

    public async Task<BaseResponse<string>> SetStoreApprovalAsync(long id, bool isApproved)
    {
        long currentUserId = 0;

        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        if (store.IsApproved == isApproved)
            return BaseResponse<string>.Fail(isApproved ? "Cửa hàng đã được duyệt" : "Cửa hàng đã bị từ chối");

        store.IsApproved = isApproved;
        store.UpdatedAt = DateTimeHelper.VnNow;
        store.UpdatedBy = currentUserId;

        repoStore.Update(store);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(currentUserId, null, isApproved ? "APPROVE_STORE" : "REJECT_STORE",
            $"{(isApproved ? "Duyệt" : "Từ chối")} cửa hàng \"{store.StoreName}\"", store.RefCode);

        return BaseResponse<string>.Success(isApproved ? "Đã duyệt cửa hàng" : "Đã từ chối cửa hàng");
    }

    public async Task<BaseResponse<string>> DeleteStoreAsync(long id)
    {
        long currentUserId = 0;

        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.Id == id );

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        store.IsDeleted = true;
        store.UpdatedAt = DateTimeHelper.VnNow;
        store.UpdatedBy = currentUserId;

        repoStore.Update(store);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(currentUserId, null, "DELETE_STORE", $"Xoá cửa hàng \"{store.StoreName}\"", store.RefCode);

        return BaseResponse<string>.Success("Delete store successfully");
    }

    public async Task<BaseTableResponse<StoreResponseDto>> SearchStoresAsync(BaseSearchRequest<AdminStoreSearchRequest> request)
    {
        var page = request.Page;
        var pageSize = request.PageSize;
        var keyword = request.SearchParams?.Keyword;
        var isActive = request.SearchParams?.IsActive;

        var repoStore = _unitOfWork.GetRepository<Store>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = repoStore
            .Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        var trimmedKeyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x => x.StoreName.Contains(trimmedKeyword));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                OwnerAccountId = x.OwnerAccountId,
                StoreName = x.StoreName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(x.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(x.ThumbnailUrl)
                    : null,
                PhoneNumber = x.PhoneNumber,
                Address = x.Address,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                IsActive = x.IsActive,
                IsApproved = x.IsApproved
            })
            .ToListAsync();

        return new BaseTableResponse<StoreResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<StoreResponseDto>> GetStoreDetailAsync(long id)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.Id == id);

        if (store is null)
            return BaseResponse<StoreResponseDto>.Fail("Store not found");

        return BaseResponse<StoreResponseDto>.Success(new StoreResponseDto
        {
            Id = store.Id,
            RefCode = store.RefCode,
            OwnerAccountId = store.OwnerAccountId,
            StoreName = store.StoreName,
            ThumbnailUrl = !string.IsNullOrWhiteSpace(store.ThumbnailUrl)
                ? _cloudinaryService.BuildImageUrl(store.ThumbnailUrl)
                : null,
            PhoneNumber = store.PhoneNumber,
            Address = store.Address,
            Description = store.Description,
            IsActive = store.IsActive,
            IsApproved = store.IsApproved,
            CreatedAt = store.CreatedAt
        });
    }

    public async Task<BaseTableResponse<StoreResponseDto>> SearchStoresByOwnerAsync(BaseSearchRequest<AdminStoreByOwnerSearchRequest> request)
    {
        var ownerRefCode = request.SearchParams?.OwnerRefCode ?? string.Empty;
        var page = request.Page;
        var pageSize = request.PageSize;

        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var account = await repoAccount.FirstOrDefaultAsync(x =>
            x.RefCode == ownerRefCode);

        if (account is null)
        {
            return new BaseTableResponse<StoreResponseDto>
            {
                Items = new List<StoreResponseDto>(),
                Page = page,
                PageSize = pageSize,
                TotalRecords = 0
            };
        }

        var query = repoStore
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.OwnerAccountId == account.Id &&
                !x.IsDeleted);

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreResponseDto
            {
                Id = x.Id,
                RefCode = x.RefCode,
                OwnerAccountId = x.OwnerAccountId,
                StoreName = x.StoreName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(x.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(x.ThumbnailUrl)
                    : null,
                PhoneNumber = x.PhoneNumber,
                Address = x.Address,
                Description = x.Description,
                IsActive = x.IsActive,
                IsApproved = x.IsApproved
            })
            .ToListAsync();

        return new BaseTableResponse<StoreResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<UserProfileResponseDto>> SearchUsersAsync(BaseSearchRequest<AdminUserSearchRequest> request)
    {
        var page = request.Page;
        var pageSize = request.PageSize;
        var keyword = request.SearchParams?.Keyword;

        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();

        var query =
            from account in repoAccount.Query().AsNoTracking()
            where !account.IsDeleted && account.Role == "User"
            join profile in repoProfile.Query().AsNoTracking()
                on account.Id equals profile.AccountId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new { account, profile };

        var trimmedKeyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x =>
                x.account.Username.Contains(trimmedKeyword) ||
                x.account.Email.Contains(trimmedKeyword) ||
                (x.profile != null && x.profile.FullName.Contains(trimmedKeyword)));
        }

        var totalRecords = await query.CountAsync();

        var pageItems = await query
            .OrderByDescending(x => x.account.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = pageItems.Select(x =>
        {
            var account = x.account;
            var profile = x.profile;

            return new UserProfileResponseDto
            {
                Account = new AccountDto
                {
                    Id = account.Id,
                    RefCode = account.RefCode,
                    Username = account.Username,
                    Email = account.Email,
                    Role = account.Role,
                    IsActive = account.IsActive
                },

                Profile = profile == null
                    ? null
                    : new AccountProfileDto
                    {
                        Id = profile.Id,
                        RefCode = profile.RefCode,
                        AccountId = profile.AccountId,
                        FullName = profile.FullName,
                        PhoneNumber = profile.PhoneNumber,
                        Gender = profile.Gender,
                        DateOfBirth = profile.DateOfBirth,
                        Address = profile.Address,
                        AvatarUrl = !string.IsNullOrWhiteSpace(profile.AvatarUrl)
                            ? _cloudinaryService.BuildImageUrl(profile.AvatarUrl)
                            : null
                    }
            };
        }).ToList();

        return new BaseTableResponse<UserProfileResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<MyProfileResponseDto>> SearchAgentsAsync(BaseSearchRequest<AdminAgentSearchRequest> request)
    {
        var page = request.Page;
        var pageSize = request.PageSize;
        var keyword = request.SearchParams?.Keyword;
        var isActive = request.SearchParams?.IsActive;

        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoBank = _unitOfWork.GetRepository<BankAccount>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        var query =
            from account in repoAccount.Query().AsNoTracking()
            where !account.IsDeleted && account.Role == "Agent"
            join profile in repoProfile.Query().AsNoTracking()
                on account.Id equals profile.AccountId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new { account, profile };

        var trimmedKeyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x =>
                x.account.Username.Contains(trimmedKeyword) ||
                x.account.Email.Contains(trimmedKeyword) ||
                (x.profile != null && x.profile.FullName.Contains(trimmedKeyword)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.account.IsActive == isActive.Value);
        }

        var totalRecords = await query.CountAsync();

        var pageItems = await query
            .OrderByDescending(x => x.account.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var accounts = pageItems
            .Select(x => x.account)
            .ToList();

        var accountIds = accounts
            .Select(x => x.Id)
            .ToList();

        var profiles = pageItems
            .Where(x => x.profile != null)
            .Select(x => x.profile!)
            .ToList();

        var bankAccounts = await repoBank
            .Query()
            .AsNoTracking()
            .Where(x =>
                accountIds.Contains(x.AccountId) &&
                x.IsActive)
            .ToListAsync();

        var stores = await repoStore
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.OwnerAccountId.HasValue &&
                accountIds.Contains(x.OwnerAccountId.Value) &&
                !x.IsDeleted)
            .ToListAsync();

        var items = accounts.Select(account =>
        {
            var profile = profiles.FirstOrDefault(x => x.AccountId == account.Id);
            var store = stores.FirstOrDefault(x => x.OwnerAccountId == account.Id);

            return new MyProfileResponseDto
            {
                Account = new AccountDto
                {
                    Id = account.Id,
                    RefCode = account.RefCode,
                    Username = account.Username,
                    Email = account.Email,
                    Role = account.Role,
                    IsActive = account.IsActive
                },

                Profile = profile == null
                    ? null
                    : new AccountProfileDto
                    {
                        Id = profile.Id,
                        RefCode = profile.RefCode,
                        AccountId = profile.AccountId,
                        FullName = profile.FullName,
                        PhoneNumber = profile.PhoneNumber,
                        Gender = profile.Gender,
                        DateOfBirth = profile.DateOfBirth,
                        Address = profile.Address,
                        AvatarUrl = !string.IsNullOrWhiteSpace(profile.AvatarUrl)
                            ? _cloudinaryService.BuildImageUrl(profile.AvatarUrl)
                            : null
                    },

                BankAccounts = bankAccounts
                    .Where(x => x.AccountId == account.Id)
                    .Select(x => new BankAccountDto
                    {
                        Id = x.Id,
                        RefCode = x.RefCode,
                        AccountId = x.AccountId,
                        BankName = x.BankName,
                        BankCode = x.BankCode,
                        AccountNumber = x.AccountNumber,
                        AccountHolderName = x.AccountHolderName,
                        IsDefault = x.IsDefault,
                        IsActive = x.IsActive
                    })
                    .ToList(),

                Store = store == null
                    ? null
                    : new StoreResponseDto
                    {
                        Id = store.Id,
                        RefCode = store.RefCode,
                        OwnerAccountId = store.OwnerAccountId,
                        StoreName = store.StoreName,
                        ThumbnailUrl = !string.IsNullOrWhiteSpace(store.ThumbnailUrl)
                            ? _cloudinaryService.BuildImageUrl(store.ThumbnailUrl)
                            : null,
                        PhoneNumber = store.PhoneNumber,
                        Address = store.Address,
                        Description = store.Description,
                        IsActive = store.IsActive,
                        IsApproved = store.IsApproved,
                        CreatedAt = store.CreatedAt
                    }
            };
        }).ToList();

        return new BaseTableResponse<MyProfileResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<string>> CreateAgentAsync(RegisterRequest request)
    {
        var refCode = Guid.NewGuid().ToString().ToUpper();

        var repoAccount = _unitOfWork.GetRepository<Account>();

        var existedEmail = await repoAccount.AnyAsync(x => x.Email == request.Email);

        if (existedEmail)
        {
            return BaseResponse<string>.Fail("Email already exists");
        }

        var existedUsername = await repoAccount
            .AnyAsync(x =>
                x.Username == request.Username);

        if (existedUsername)
        {
            return BaseResponse<string>.Fail("Username already exists");
        }

        var account = new Account
        {
            RefCode = refCode,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Agent"
        };

        await repoAccount.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAsync(
            "System",
            account.Id,
            account.Username,
            "CREATE_AGENT",
            $"Tạo đại lý \"{account.Username}\" ({account.Email})");

        return BaseResponse<string>.Success("Create account successfully");
    }

    public async Task<BaseResponse<AdminRevenueResponseDto>> GetRevenueAsync(RevenueQueryDto request)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoOrder = _unitOfWork.GetRepository<Order>();

        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);
        var groupBy = request.GroupBy?.ToLower() == "month" ? "month" : "day";

        var orderQuery = repoOrder
            .Query()
            .AsNoTracking();

        if (fromDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt <= toDate.Value);
        }

        var paidOrderQuery = orderQuery
            .Where(x => x.PaymentStatus == PaymentStatus.Paid && x.OrderStatus != OrderStatus.Cancelled);

        var totalAgents = await repoAccount
            .Query()
            .AsNoTracking()
            .CountAsync(x =>
                !x.IsDeleted &&
                x.Role == "Agent");

        var totalUsers = await repoAccount
            .Query()
            .AsNoTracking()
            .CountAsync(x =>
                !x.IsDeleted &&
                x.Role == "User");

        var totalOrders = await orderQuery.CountAsync();

        var totalRevenue = await paidOrderQuery
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var paidOrders = await paidOrderQuery
            .Select(x => new
            {
                x.Id,
                x.CreatedAt,
                x.TotalAmount,
                x.OrderStatus
            })
            .ToListAsync();

        var orderCosts = await GetOrderCostsAsync(paidOrderQuery);

        var lineChart = BuildLineChart(
            paidOrders.Select(x => new RevenueRawItem
            {
                CreatedAt = x.CreatedAt,
                Amount = x.TotalAmount,
                Cost = orderCosts.GetValueOrDefault(x.Id)
            }).ToList(),
            groupBy,
            fromDate,
            toDate);

        var pieChart = paidOrders
            .GroupBy(x => EnumCode.ToCode(x.OrderStatus))
            .Select(g => new RevenuePieChartDto
            {
                Label = g.Key,
                Value = g.Sum(x => x.TotalAmount)
            })
            .ToList();

        return BaseResponse<AdminRevenueResponseDto>.Success(
            new AdminRevenueResponseDto
            {
                TotalAgents = totalAgents,
                TotalUsers = totalUsers,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                LineChart = lineChart,
                PieChart = pieChart
            });
    }

    public async Task<BaseResponse<List<TopSellingProductDto>>> GetTopProductsAsync(RevenueQueryDto request, string? storeRefCode, int top = 10)
    {
        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(storeRefCode, fromDate, toDate);

        var topProducts = products
            .OrderByDescending(x => x.QuantitySold)
            .Take(top <= 0 ? 10 : top)
            .ToList();

        return BaseResponse<List<TopSellingProductDto>>.Success(topProducts);
    }

    public async Task<BaseTableResponse<TopSellingProductDto>> SearchProductRevenueAsync(BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var search = request.SearchParams;
        var fromDate = search?.FromDate?.Date;
        var toDate = search?.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(search?.StoreRefCode, fromDate, toDate);

        IEnumerable<TopSellingProductDto> filtered = products;

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            filtered = filtered.Where(x => x.FoodName.ToLower().Contains(keyword));
        }

        filtered = request.SortBy?.ToLower() switch
        {
            "quantitysold" => request.Asc
                ? filtered.OrderBy(x => x.QuantitySold)
                : filtered.OrderByDescending(x => x.QuantitySold),

            "profit" => request.Asc
                ? filtered.OrderBy(x => x.Profit)
                : filtered.OrderByDescending(x => x.Profit),

            "foodname" => request.Asc
                ? filtered.OrderBy(x => x.FoodName)
                : filtered.OrderByDescending(x => x.FoodName),

            "storename" => request.Asc
                ? filtered.OrderBy(x => x.StoreName)
                : filtered.OrderByDescending(x => x.StoreName),

            _ => request.Asc
                ? filtered.OrderBy(x => x.Revenue)
                : filtered.OrderByDescending(x => x.Revenue)
        };

        var materialized = filtered.ToList();
        var totalRecords = materialized.Count;

        var pageItems = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BaseTableResponse<TopSellingProductDto>
        {
            Items = pageItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<CustomerListItemDto>> SearchCustomersAsync(BaseSearchRequest<CustomerSearchRequest> request)
    {
        var customers = await BuildCustomersAsync(request.SearchParams?.StoreRefCode);

        return FilterSortPaginate(customers, request);
    }

    public async Task<BaseTableResponse<StoreFoodResponseDto>> SearchStoreFoodsAsync(BaseSearchRequest<AdminStoreFoodSearchRequest> request)
    {
        var page = request.Page;
        var pageSize = request.PageSize;
        var storeRefCode = request.SearchParams?.StoreRefCode;
        var keyword = request.SearchParams?.Keyword;

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
            CostPrice = x.StoreFood.CostPrice,
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

    public async Task<BaseTableResponse<StoreFoodCategoryResponseDto>> SearchCategoriesAsync(BaseSearchRequest<AdminCategorySearchRequest> request)
    {
        var page = request.Page;
        var pageSize = request.PageSize;
        var keyword = request.SearchParams?.Keyword;
        var storeRefCode = request.SearchParams?.StoreRefCode;
        var sortBy = request.SortBy;
        var asc = request.Asc;

        var repoCategory = _unitOfWork.GetRepository<StoreFoodCategories>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query =
            from category in repoCategory.Query().AsNoTracking()
            join store in repoStore.Query().AsNoTracking()
                on category.StoreRefCode equals store.RefCode
            where !category.IsDelete
            select new { category, store.StoreName };

        if (!string.IsNullOrWhiteSpace(storeRefCode))
        {
            query = query.Where(x => x.category.StoreRefCode == storeRefCode);
        }

        var trimmedKeyword = keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x => x.category.CategoryName.Contains(trimmedKeyword));
        }

        var totalRecords = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "categoryname" => asc
                ? query.OrderBy(x => x.category.CategoryName)
                : query.OrderByDescending(x => x.category.CategoryName),

            _ => asc
                ? query.OrderBy(x => x.category.CreatedAt)
                : query.OrderByDescending(x => x.category.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreFoodCategoryResponseDto
            {
                Id = x.category.Id,
                RefCode = x.category.RefCode,
                CategoryName = x.category.CategoryName,
                Description = x.category.Description,
                CreatedAt = x.category.CreatedAt,
                StoreRefCode = x.category.StoreRefCode,
                StoreName = x.StoreName
            })
            .ToListAsync();

        return new BaseTableResponse<StoreFoodCategoryResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<OrderResponseDto>> SearchOrdersAsync(BaseSearchRequest<OrderSearchRequest> request)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoAccountProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();

        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var search = request.SearchParams;

        var query = repoOrder
            .Query()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search?.StoreRefCode))
        {
            query = query.Where(x => x.StoreRefCode == search.StoreRefCode);
        }

        query = ApplyStatusFilter(query, search?.Status);

        if (EnumCode.TryParse<OrderStatus>(search?.OrderStatus, out var orderStatus))
        {
            query = query.Where(x => x.OrderStatus == orderStatus);
        }

        if (EnumCode.TryParse<PaymentStatus>(search?.PaymentStatus, out var paymentStatus))
        {
            query = query.Where(x => x.PaymentStatus == paymentStatus);
        }

        if (search?.OrderSource != null)
        {
            query = query.Where(x => x.OrderSource == search.OrderSource);
        }

        if (search?.FromDate != null)
        {
            var fromDate = search.FromDate.Value.Date;
            query = query.Where(x => x.CreatedAt >= fromDate);
        }

        if (search?.ToDate != null)
        {
            var toDate = search.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toDate);
        }

        query = ApplyPromotionFilter(
            query,
            search?.PromotionType,
            search?.PromotionKeyword,
            repoOrderItem.Query().AsNoTracking(),
            _unitOfWork.GetRepository<Promotion>().Query().AsNoTracking());

        var projectedQuery =
            from order in query

            join account in repoAccount.Query().AsNoTracking()
                on order.CustomerAccountId equals account.Id into accountGroup
            from account in accountGroup.DefaultIfEmpty()

            join profile in repoAccountProfile.Query().AsNoTracking()
                on account.Id equals profile.AccountId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()

            join customer in repoCustomer.Query().AsNoTracking()
                on order.CustomerId equals customer.Id into customerGroup
            from customer in customerGroup.DefaultIfEmpty()

            select new OrderResponseDto
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                RefCode = order.RefCode,

                CustomerAccountId = order.CustomerAccountId,

                CustomerName =
                    account != null
                        ? (
                            profile != null && !string.IsNullOrEmpty(profile.FullName)
                                ? profile.FullName
                                : account.Username
                        )
                        : (
                            customer != null
                                ? customer.CustomerName
                                : "Khách vãng lai"
                        ),

                StoreRefCode = order.StoreRefCode,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = order.PaymentMethod,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                OrderSource = order.OrderSource,
                OrderType = order.OrderType,
                DeliveryPhone = order.DeliveryPhone,
                DeliveryProvinceCode = order.DeliveryProvinceCode,
                DeliveryProvinceName = order.DeliveryProvinceName,
                DeliveryWardCode = order.DeliveryWardCode,
                DeliveryWardName = order.DeliveryWardName,
                DeliveryStreet = order.DeliveryStreet,
                DeliveryLatitude = order.DeliveryLatitude,
                DeliveryLongitude = order.DeliveryLongitude,
                Items = new List<OrderItemResponseDto>()
            };

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();

            projectedQuery = projectedQuery.Where(x =>
                x.OrderCode.ToLower().Contains(keyword) ||
                x.RefCode.ToLower().Contains(keyword) ||
                x.CustomerName.ToLower().Contains(keyword) ||
                (x.Note != null && x.Note.ToLower().Contains(keyword)) ||
                (x.DeliveryPhone != null && x.DeliveryPhone.Contains(keyword)) ||
                (x.DeliveryStreet != null && x.DeliveryStreet.ToLower().Contains(keyword))
            );
        }

        var totalRecords = await projectedQuery.CountAsync();

        projectedQuery = request.SortBy switch
        {
            "totalAmount" => request.Asc
                ? projectedQuery.OrderBy(x => x.TotalAmount)
                : projectedQuery.OrderByDescending(x => x.TotalAmount),

            "createdAt" => request.Asc
                ? projectedQuery.OrderBy(x => x.CreatedAt)
                : projectedQuery.OrderByDescending(x => x.CreatedAt),

            "orderCode" => request.Asc
                ? projectedQuery.OrderBy(x => x.OrderCode)
                : projectedQuery.OrderByDescending(x => x.OrderCode),

            _ => projectedQuery.OrderByDescending(x => x.Id)
        };

        var items = await projectedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var orderIds = items
            .Select(x => x.Id)
            .ToList();

        var orderItems = await (
            from orderItem in repoOrderItem.Query().AsNoTracking()
            join food in repoFood.Query().AsNoTracking()
                on orderItem.StoreFoodId equals food.Id
            where orderIds.Contains(orderItem.OrderId)
            select new OrderItemResponseDto
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                StoreFoodId = orderItem.StoreFoodId,
                FoodName = food.FoodName,
                ProductCode = food.ProductCode,
                ThumbnailUrl = food.ThumbnailUrl,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.TotalPrice,
                OriginalUnitPrice = orderItem.OriginalUnitPrice,
                PromotionId = orderItem.PromotionId,
                PromotionName = orderItem.PromotionName
            })
            .ToListAsync();

        await FillOrderItemOptionsAsync(orderItems);

        foreach (var item in orderItems)
        {
            item.ThumbnailUrl = BuildFoodThumbnailUrl(item.ThumbnailUrl);
        }

        foreach (var order in items)
        {
            order.Items = orderItems
                .Where(x => x.OrderId == order.Id)
                .ToList();
        }

        return new BaseTableResponse<OrderResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<ActivityLogResponseDto>> SearchActivityLogsAsync(BaseSearchRequest<ActivityLogSearchRequest> request)
    {
        var repo = _unitOfWork.GetRepository<ActivityLog>();

        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var search = request.SearchParams;

        var query = repo.Query().AsNoTracking();

        var trimmedKeyword = search?.Keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmedKeyword))
        {
            query = query.Where(x =>
                x.Description.Contains(trimmedKeyword) ||
                (x.ActorName != null && x.ActorName.Contains(trimmedKeyword)));
        }

        if (!string.IsNullOrWhiteSpace(search?.Action))
        {
            query = query.Where(x => x.Action == search.Action);
        }

        query = search?.ActionGroup switch
        {
            "CREATE" => query.Where(x => x.Action.StartsWith("CREATE_")),
            "UPDATE" => query.Where(x => x.Action.StartsWith("UPDATE_")),
            "DELETE" => query.Where(x => x.Action.StartsWith("DELETE_")),
            "OTHER" => query.Where(x =>
                !x.Action.StartsWith("CREATE_") &&
                !x.Action.StartsWith("UPDATE_") &&
                !x.Action.StartsWith("DELETE_")),
            _ => query
        };

        if (search?.FromDate != null)
        {
            var fromDate = search.FromDate.Value.Date;
            query = query.Where(x => x.CreatedAt >= fromDate);
        }

        if (search?.ToDate != null)
        {
            var toDate = search.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toDate);
        }

        var totalRecords = await query.CountAsync();

        query = request.Asc
            ? query.OrderBy(x => x.CreatedAt)
            : query.OrderByDescending(x => x.CreatedAt);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ActivityLogResponseDto
            {
                Id = x.Id,
                ActorType = x.ActorType,
                ActorId = x.ActorId,
                ActorName = x.ActorName,
                Action = x.Action,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new BaseTableResponse<ActivityLogResponseDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    private async Task<List<TopSellingProductDto>> BuildProductRevenueAsync(string? storeRefCode, DateTime? fromDate, DateTime? toDate)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        var orderQuery = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x => x.PaymentStatus == PaymentStatus.Paid && x.OrderStatus != OrderStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(storeRefCode))
        {
            orderQuery = orderQuery.Where(x => x.StoreRefCode == storeRefCode);
        }

        if (fromDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt <= toDate.Value);
        }

        var items = await (
            from oi in repoOrderItem.Query().AsNoTracking()
            join o in orderQuery on oi.OrderId equals o.Id
            join f in repoStoreFood.Query().AsNoTracking() on oi.StoreFoodId equals f.Id
            join s in repoStore.Query().AsNoTracking() on o.StoreRefCode equals s.RefCode
            select new
            {
                oi.StoreFoodId,
                f.FoodName,
                f.ThumbnailUrl,
                oi.Quantity,
                oi.TotalPrice,
                oi.CostPrice,
                StoreRefCode = o.StoreRefCode,
                s.StoreName
            }
        ).ToListAsync();

        return items
            .GroupBy(x => new { x.StoreFoodId, x.FoodName, x.ThumbnailUrl, x.StoreRefCode, x.StoreName })
            .Select(g => new TopSellingProductDto
            {
                StoreFoodId = g.Key.StoreFoodId,
                FoodName = g.Key.FoodName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(g.Key.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(g.Key.ThumbnailUrl)
                    : null,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice),
                Cost = g.Sum(x => x.Quantity * x.CostPrice),
                Profit = g.Sum(x => x.TotalPrice - x.Quantity * x.CostPrice),
                StoreRefCode = g.Key.StoreRefCode,
                StoreName = g.Key.StoreName
            })
            .ToList();
    }

    private async Task<Dictionary<long, decimal>> GetOrderCostsAsync(IQueryable<Order> orders)
    {
        var orderIds = orders.Select(x => x.Id);

        return await _unitOfWork.GetRepository<OrderItem>()
            .Query()
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .GroupBy(x => x.OrderId)
            .Select(g => new { g.Key, Cost = g.Sum(x => x.Quantity * x.CostPrice) })
            .ToDictionaryAsync(x => x.Key, x => x.Cost);
    }

    private static List<RevenueLineChartDto> BuildLineChart(
        List<RevenueRawItem> items,
        string groupBy,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (items.Count == 0 && (!fromDate.HasValue || !toDate.HasValue))
        {
            return [];
        }

        var start = fromDate?.Date ?? items.Min(x => x.CreatedAt.Date);
        var end = toDate?.Date ?? items.Max(x => x.CreatedAt.Date);

        if (groupBy == "month")
        {
            var byMonth = items
                .GroupBy(x => new DateTime(x.CreatedAt.Year, x.CreatedAt.Month, 1))
                .ToDictionary(g => g.Key, g => (Revenue: g.Sum(x => x.Amount), Cost: g.Sum(x => x.Cost), Count: g.Count()));

            var months = new List<RevenueLineChartDto>();

            for (var m = new DateTime(start.Year, start.Month, 1); m <= end; m = m.AddMonths(1))
            {
                byMonth.TryGetValue(m, out var v);
                months.Add(new RevenueLineChartDto
                {
                    Label = m.ToString("MM/yyyy"),
                    Revenue = v.Revenue,
                    Cost = v.Cost,
                    Profit = v.Revenue - v.Cost,
                    OrderCount = v.Count
                });
            }

            return months;
        }

        var byDay = items
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(x => x.Amount), Cost: g.Sum(x => x.Cost), Count: g.Count()));

        var days = new List<RevenueLineChartDto>();

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            byDay.TryGetValue(d, out var v);
            days.Add(new RevenueLineChartDto
            {
                Label = d.ToString("dd/MM/yyyy"),
                Revenue = v.Revenue,
                Cost = v.Cost,
                Profit = v.Revenue - v.Cost,
                OrderCount = v.Count
            });
        }

        return days;
    }

    private async Task<List<CustomerListItemDto>> BuildCustomersAsync(string? storeRefCode)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();
        var repoCustomer = _unitOfWork.GetRepository<Customer>();

        var orderQuery = repoOrder.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(storeRefCode))
        {
            orderQuery = orderQuery.Where(x => x.StoreRefCode == storeRefCode);
        }

        var orders = await (
            from o in orderQuery
            join s in repoStore.Query().AsNoTracking() on o.StoreRefCode equals s.RefCode
            select new
            {
                o.CustomerAccountId,
                o.CustomerId,
                StoreRefCode = o.StoreRefCode,
                s.StoreName,
                o.TotalAmount,
                o.PaymentStatus,
                o.CreatedAt
            }
        ).ToListAsync();

        var result = new List<CustomerListItemDto>();

        var registeredGroups = orders
            .Where(x => x.CustomerAccountId.HasValue)
            .GroupBy(x => new { AccountId = x.CustomerAccountId!.Value, x.StoreRefCode, x.StoreName })
            .ToList();

        if (registeredGroups.Count > 0)
        {
            var accountIds = registeredGroups.Select(g => g.Key.AccountId).Distinct().ToList();

            var accounts = await repoAccount.Query().AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToListAsync();

            var profiles = await repoProfile.Query().AsNoTracking()
                .Where(x => accountIds.Contains(x.AccountId))
                .ToListAsync();

            foreach (var group in registeredGroups)
            {
                var account = accounts.FirstOrDefault(x => x.Id == group.Key.AccountId);
                var profile = profiles.FirstOrDefault(x => x.AccountId == group.Key.AccountId);

                result.Add(new CustomerListItemDto
                {
                    RefCode = account?.RefCode ?? string.Empty,
                    CustomerName = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile!.FullName : (account?.Username ?? "Khách hàng"),
                    PhoneNumber = profile?.PhoneNumber,
                    Email = account?.Email,
                    AvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl)
                        ? _cloudinaryService.BuildImageUrl(profile!.AvatarUrl!)
                        : null,
                    IsGuest = false,
                    TotalOrders = group.Count(),
                    TotalSpent = group.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.TotalAmount),
                    LastOrderAt = group.Max(x => x.CreatedAt),
                    StoreRefCode = group.Key.StoreRefCode,
                    StoreName = group.Key.StoreName
                });
            }
        }

        var guestGroups = orders
            .Where(x => !x.CustomerAccountId.HasValue && x.CustomerId.HasValue)
            .GroupBy(x => new { CustomerId = x.CustomerId!.Value, x.StoreRefCode, x.StoreName })
            .ToList();

        if (guestGroups.Count > 0)
        {
            var customerIds = guestGroups.Select(g => g.Key.CustomerId).Distinct().ToList();

            var customers = await repoCustomer.Query().AsNoTracking()
                .Where(x => customerIds.Contains(x.Id))
                .ToListAsync();

            foreach (var group in guestGroups)
            {
                var customer = customers.FirstOrDefault(x => x.Id == group.Key.CustomerId);

                result.Add(new CustomerListItemDto
                {
                    RefCode = customer?.RefCode ?? string.Empty,
                    CustomerName = customer?.CustomerName ?? "Khách vãng lai",
                    PhoneNumber = null,
                    Email = null,
                    AvatarUrl = null,
                    IsGuest = true,
                    TotalOrders = group.Count(),
                    TotalSpent = group.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.TotalAmount),
                    LastOrderAt = group.Max(x => x.CreatedAt),
                    StoreRefCode = group.Key.StoreRefCode,
                    StoreName = group.Key.StoreName
                });
            }
        }

        return result;
    }

    private static BaseTableResponse<CustomerListItemDto> FilterSortPaginate(
        List<CustomerListItemDto> customers,
        BaseSearchRequest<CustomerSearchRequest> request)
    {
        IEnumerable<CustomerListItemDto> filtered = customers;

        var keyword = request.SearchParams?.Keyword?.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.ToLower();

            filtered = filtered.Where(x =>
                x.CustomerName.ToLower().Contains(lowerKeyword) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(lowerKeyword)) ||
                (x.Email != null && x.Email.ToLower().Contains(lowerKeyword)));
        }

        filtered = request.SortBy?.ToLower() switch
        {
            "totalorders" => request.Asc
                ? filtered.OrderBy(x => x.TotalOrders)
                : filtered.OrderByDescending(x => x.TotalOrders),

            "customername" => request.Asc
                ? filtered.OrderBy(x => x.CustomerName)
                : filtered.OrderByDescending(x => x.CustomerName),

            "lastorderat" => request.Asc
                ? filtered.OrderBy(x => x.LastOrderAt)
                : filtered.OrderByDescending(x => x.LastOrderAt),

            _ => request.Asc
                ? filtered.OrderBy(x => x.TotalSpent)
                : filtered.OrderByDescending(x => x.TotalSpent)
        };

        var materialized = filtered.ToList();
        var totalRecords = materialized.Count;

        var pageItems = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BaseTableResponse<CustomerListItemDto>
        {
            Items = pageItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    private static IQueryable<Order> ApplyStatusFilter(IQueryable<Order> query, string? status)
    {
        return status switch
        {
            "CANCELLED" => query.Where(x => x.OrderStatus == OrderStatus.Cancelled),
            "PAID" => query.Where(x => x.OrderStatus != OrderStatus.Cancelled && x.PaymentStatus == PaymentStatus.Paid),
            "UNPAID" => query.Where(x => x.OrderStatus != OrderStatus.Cancelled && x.PaymentStatus == PaymentStatus.Unpaid),
            _ => query
        };
    }

    private static IQueryable<Order> ApplyPromotionFilter(
        IQueryable<Order> query,
        string? promotionType,
        string? promotionKeyword,
        IQueryable<OrderItem> orderItems,
        IQueryable<Promotion> promotions)
    {
        if (promotionType == "NONE")
        {
            query = query.Where(o => !orderItems.Any(i => i.OrderId == o.Id && i.PromotionId != null));
        }
        else if (!string.IsNullOrWhiteSpace(promotionType))
        {
            query = query.Where(o => orderItems.Any(i =>
                i.OrderId == o.Id &&
                promotions.Any(p => p.Id == i.PromotionId && p.PromotionType == promotionType)));
        }

        if (!string.IsNullOrWhiteSpace(promotionKeyword))
        {
            var keyword = promotionKeyword.Trim().ToLower();

            query = query.Where(o => orderItems.Any(i =>
                i.OrderId == o.Id &&
                i.PromotionId != null &&
                (
                    (i.PromotionName != null && i.PromotionName.ToLower().Contains(keyword)) ||
                    promotions.Any(p =>
                        p.Id == i.PromotionId &&
                        p.PromotionCode != null &&
                        p.PromotionCode.ToLower().Contains(keyword))
                )));
        }

        return query;
    }

    private async Task FillOrderItemOptionsAsync(List<OrderItemResponseDto> orderItems)
    {
        if (orderItems == null || !orderItems.Any())
            return;

        var repoOrderItemOption = _unitOfWork.GetRepository<OrderItemOption>();

        var orderItemIds = orderItems
            .Select(x => x.Id)
            .ToList();

        var options = await repoOrderItemOption
            .Query()
            .AsNoTracking()
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .Select(x => new OrderItemOptionResponseDto
            {
                Id = x.Id,
                OrderItemId = x.OrderItemId,
                OptionGroupId = x.OptionGroupId,
                OptionGroupName = x.OptionGroupName,
                OptionId = x.OptionId,
                OptionName = x.OptionName,
                AdditionalPrice = x.AdditionalPrice
            })
            .ToListAsync();

        foreach (var item in orderItems)
        {
            item.Options = options
                .Where(x => x.OrderItemId == item.Id)
                .ToList();
        }
    }

    private string? BuildFoodThumbnailUrl(string? thumbnailRefCode) => !string.IsNullOrWhiteSpace(thumbnailRefCode) ? _cloudinaryService.BuildImageUrl(thumbnailRefCode) : null;

    private class RevenueRawItem
    {
        public DateTime CreatedAt { get; set; }
        public decimal Amount { get; set; }
        public decimal Cost { get; set; }
    }
}
