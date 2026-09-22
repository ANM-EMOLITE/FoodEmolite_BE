using FoodEmolite.Shared.Common;
using FoodEmolite.Application.DTOs.ActivityLog;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivityLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(string actorType, long? actorId, string? actorName, string action, string description, string? storeRefCode = null)
    {
        var repo = _unitOfWork.GetRepository<ActivityLog>();

        await repo.AddAsync(new ActivityLog
        {
            ActorType = actorType,
            ActorId = actorId,
            ActorName = actorName,
            Action = action,
            Description = description,
            StoreRefCode = storeRefCode,
            CreatedAt = DateTimeHelper.VnNow
        });

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task LogAgentActionAsync(long? accountId, string? accountRefCode, string action, string description, string? storeRefCode)
    {
        var repoAccount = _unitOfWork.GetRepository<Account>();
        var repoProfile = _unitOfWork.GetRepository<AccountProfile>();

        Account? account = null;

        if (accountId.HasValue)
        {
            account = await repoAccount.FirstOrDefaultAsync(x => x.Id == accountId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(accountRefCode))
        {
            account = await repoAccount.FirstOrDefaultAsync(x => x.RefCode == accountRefCode);
        }

        string? name = account?.Username;

        if (account != null)
        {
            var profile = await repoProfile.FirstOrDefaultAsync(x => x.AccountId == account.Id);

            if (!string.IsNullOrEmpty(profile?.FullName))
            {
                name = profile.FullName;
            }
        }

        await LogAsync("Agent", account?.Id, name, action, description, storeRefCode);
    }

    public Task<BaseTableResponse<ActivityLogResponseDto>> SearchAsync(BaseSearchRequest<ActivityLogSearchRequest> request) => SearchCoreAsync(request, null);

    public async Task<BaseTableResponse<ActivityLogResponseDto>> SearchForAgentStoreAsync(long agentAccountId, BaseSearchRequest<ActivityLogSearchRequest> request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == agentAccountId &&
            !x.IsDeleted);

        if (store is null || string.IsNullOrWhiteSpace(store.RefCode))
        {
            return new BaseTableResponse<ActivityLogResponseDto>
            {
                Items = [],
                Page = request.Page <= 0 ? 1 : request.Page,
                PageSize = request.PageSize <= 0 ? 10 : request.PageSize,
                TotalRecords = 0
            };
        }

        return await SearchCoreAsync(request, store.RefCode);
    }

    private async Task<BaseTableResponse<ActivityLogResponseDto>> SearchCoreAsync(BaseSearchRequest<ActivityLogSearchRequest> request, string? storeRefCode)
    {
        var repo = _unitOfWork.GetRepository<ActivityLog>();

        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var search = request.SearchParams;

        var query = repo.Query().AsNoTracking();
        if (storeRefCode != null)
        {
            query = query.Where(x => x.StoreRefCode == storeRefCode && x.Action != "CREATE_ORDER");
        }

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
}
