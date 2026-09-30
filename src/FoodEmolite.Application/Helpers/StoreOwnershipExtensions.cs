using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;

namespace FoodEmolite.Application.Helpers;

public static class StoreOwnershipExtensions
{
    public static Task<Store?> GetOwnedStoreAsync(this IUnitOfWork unitOfWork, long accountId)
        => unitOfWork.GetRepository<Store>().FirstOrDefaultAsync(x =>
            x.OwnerAccountId == accountId &&
            !x.IsDeleted);

    public static async Task<Store?> GetOwnedStoreByRefCodeAsync(this IUnitOfWork unitOfWork, string? accountRefCode)
    {
        if (string.IsNullOrWhiteSpace(accountRefCode))
            return null;

        var account = await unitOfWork.GetRepository<Account>().FirstOrDefaultAsync(x => x.RefCode == accountRefCode);

        return account is null ? null : await unitOfWork.GetOwnedStoreAsync(account.Id);
    }
}
