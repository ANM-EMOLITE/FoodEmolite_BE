using FoodEmolite.Application.DTOs.Supplier;
using FoodEmolite.Application.Helpers;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Common;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityLogService _activityLogService;

    public SupplierService(IUnitOfWork unitOfWork, IActivityLogService activityLogService)
    {
        _unitOfWork = unitOfWork;
        _activityLogService = activityLogService;
    }

    public async Task<BaseTableResponse<SupplierResponseDto>> SearchAsync(long currentUserId, BaseSearchRequest<SupplierSearchRequest> request)
    {
        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 1000);

        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return new BaseTableResponse<SupplierResponseDto> { Items = [], Page = request.Page, PageSize = request.PageSize };

        var search = request.SearchParams;

        var query = _unitOfWork.GetRepository<Supplier>()
            .Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == store.RefCode && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            query = query.Where(x =>
                x.SupplierCode.ToLower().Contains(keyword) ||
                x.SupplierName.ToLower().Contains(keyword) ||
                (x.ContactName != null && x.ContactName.ToLower().Contains(keyword)) ||
                (x.Phone != null && x.Phone.Contains(keyword)));
        }

        if (search?.IsActive.HasValue == true)
            query = query.Where(x => x.IsActive == search.IsActive.Value);

        var totalRecords = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "suppliername" => request.Asc ? query.OrderBy(x => x.SupplierName) : query.OrderByDescending(x => x.SupplierName),
            _ => request.Asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt)
        };

        var suppliers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var stats = await GetReceiptStatsAsync(store.RefCode!, suppliers.Select(x => x.Id).ToList());

        return new BaseTableResponse<SupplierResponseDto>
        {
            Items = suppliers.Select(x => ToResponse(x, stats.GetValueOrDefault(x.Id))).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<SupplierResponseDto>> GetDetailAsync(long currentUserId, long id)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<SupplierResponseDto>.Fail("Store not found");

        var supplier = await FindAsync(store.RefCode!, id);

        if (supplier is null)
            return BaseResponse<SupplierResponseDto>.Fail("Không tìm thấy nhà cung cấp");

        var stats = await GetReceiptStatsAsync(store.RefCode!, [supplier.Id]);

        return BaseResponse<SupplierResponseDto>.Success(ToResponse(supplier, stats.GetValueOrDefault(supplier.Id)));
    }

    public async Task<BaseResponse<string>> CreateAsync(long currentUserId, string refCode, SaveSupplierRequestDto request)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        var error = await ValidateAsync(store.RefCode!, null, request);

        if (error is not null)
            return BaseResponse<string>.Fail(error);

        var repo = _unitOfWork.GetRepository<Supplier>();
        var count = await repo.Query().CountAsync(x => x.StoreRefCode == store.RefCode);

        var supplier = new Supplier
        {
            RefCode = refCode,
            StoreRefCode = store.RefCode!,
            SupplierCode = $"NCC{count + 1:D5}",
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        };

        Apply(supplier, request);

        await repo.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(currentUserId, null, "CREATE_SUPPLIER",
            $"Tạo nhà cung cấp \"{supplier.SupplierName}\" ({supplier.SupplierCode})", store.RefCode);

        return BaseResponse<string>.Success(supplier.SupplierCode);
    }

    public async Task<BaseResponse<string>> UpdateAsync(long currentUserId, long id, SaveSupplierRequestDto request)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        var supplier = await FindAsync(store.RefCode!, id);

        if (supplier is null)
            return BaseResponse<string>.Fail("Không tìm thấy nhà cung cấp");

        var error = await ValidateAsync(store.RefCode!, id, request);

        if (error is not null)
            return BaseResponse<string>.Fail(error);

        var oldName = supplier.SupplierName;

        var changes = new ChangeSummary()
            .Text("Tên", supplier.SupplierName, request.SupplierName?.Trim())
            .Text("Người liên hệ", supplier.ContactName, request.ContactName?.Trim())
            .Text("SĐT", supplier.Phone, request.Phone?.Trim())
            .Text("Email", supplier.Email, request.Email?.Trim())
            .Text("Địa chỉ", supplier.Address, request.Address?.Trim())
            .Text("MST", supplier.TaxCode, request.TaxCode?.Trim())
            .Text("Ghi chú", supplier.Note, request.Note?.Trim())
            .Flag("Trạng thái", supplier.IsActive, request.IsActive, "Đang giao dịch", "Ngừng giao dịch");

        Apply(supplier, request);
        supplier.UpdatedAt = DateTimeHelper.VnNow;
        supplier.UpdatedBy = currentUserId;

        _unitOfWork.GetRepository<Supplier>().Update(supplier);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(currentUserId, null, "UPDATE_SUPPLIER",
            changes.Describe($"Cập nhật nhà cung cấp \"{oldName}\""), store.RefCode);

        return BaseResponse<string>.Success("Cập nhật nhà cung cấp thành công");
    }

    public async Task<BaseResponse<string>> DeleteAsync(long currentUserId, long id)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        var supplier = await FindAsync(store.RefCode!, id);

        if (supplier is null)
            return BaseResponse<string>.Fail("Không tìm thấy nhà cung cấp");

        supplier.IsDeleted = true;
        supplier.UpdatedAt = DateTimeHelper.VnNow;
        supplier.UpdatedBy = currentUserId;

        _unitOfWork.GetRepository<Supplier>().Update(supplier);
        await _unitOfWork.SaveChangesAsync();

        await _activityLogService.LogAgentActionAsync(currentUserId, null, "DELETE_SUPPLIER",
            $"Xoá nhà cung cấp \"{supplier.SupplierName}\" ({supplier.SupplierCode})", store.RefCode);

        return BaseResponse<string>.Success("Xoá nhà cung cấp thành công");
    }

    private Task<Supplier?> FindAsync(string storeRefCode, long id)
        => _unitOfWork.GetRepository<Supplier>().FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.StoreRefCode == storeRefCode &&
            !x.IsDeleted);

    private async Task<string?> ValidateAsync(string storeRefCode, long? id, SaveSupplierRequestDto request)
    {
        var name = request.SupplierName?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return "Tên nhà cung cấp không được để trống";

        var lowerName = name.ToLower();

        var existed = await _unitOfWork.GetRepository<Supplier>().AnyAsync(x =>
            x.StoreRefCode == storeRefCode &&
            !x.IsDeleted &&
            x.Id != id &&
            x.SupplierName.ToLower() == lowerName);

        return existed ? "Tên nhà cung cấp đã tồn tại" : null;
    }

    private static void Apply(Supplier supplier, SaveSupplierRequestDto request)
    {
        supplier.SupplierName = request.SupplierName.Trim();
        supplier.ContactName = NullIfEmpty(request.ContactName);
        supplier.Phone = NullIfEmpty(request.Phone);
        supplier.Email = NullIfEmpty(request.Email);
        supplier.Address = NullIfEmpty(request.Address);
        supplier.TaxCode = NullIfEmpty(request.TaxCode);
        supplier.Note = NullIfEmpty(request.Note);
        supplier.IsActive = request.IsActive;
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Dictionary<long, ReceiptStats>> GetReceiptStatsAsync(string storeRefCode, List<long> supplierIds)
    {
        if (supplierIds.Count == 0)
            return [];

        return await _unitOfWork.GetRepository<InventoryReceipt>()
            .Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == storeRefCode && x.SupplierId != null && supplierIds.Contains(x.SupplierId.Value))
            .GroupBy(x => x.SupplierId!.Value)
            .Select(g => new ReceiptStats
            {
                SupplierId = g.Key,
                TotalReceipts = g.Count(),
                TotalQuantity = g.Sum(x => x.TotalQuantity),
                TotalAmount = g.Sum(x => x.TotalAmount),
                LastReceiptAt = g.Max(x => (DateTime?)x.CreatedAt)
            })
            .ToDictionaryAsync(x => x.SupplierId);
    }

    private static SupplierResponseDto ToResponse(Supplier x, ReceiptStats? stats) => new()
    {
        Id = x.Id,
        SupplierCode = x.SupplierCode,
        SupplierName = x.SupplierName,
        ContactName = x.ContactName,
        Phone = x.Phone,
        Email = x.Email,
        Address = x.Address,
        TaxCode = x.TaxCode,
        Note = x.Note,
        IsActive = x.IsActive,
        TotalReceipts = stats?.TotalReceipts ?? 0,
        TotalQuantity = stats?.TotalQuantity ?? 0,
        TotalAmount = stats?.TotalAmount ?? 0,
        LastReceiptAt = stats?.LastReceiptAt,
        CreatedAt = x.CreatedAt
    };

    private sealed class ReceiptStats
    {
        public long SupplierId { get; init; }
        public int TotalReceipts { get; init; }
        public int TotalQuantity { get; init; }
        public decimal TotalAmount { get; init; }
        public DateTime? LastReceiptAt { get; init; }
    }
}
