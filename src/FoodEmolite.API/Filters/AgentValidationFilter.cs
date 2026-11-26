using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodEmolite.API.Filters;

public class AgentValidationAttribute : TypeFilterAttribute
{
    public AgentValidationAttribute() : base(typeof(AgentValidationFilter))
    {
    }
}

public static class AgentValidationCode
{
    public const string Unauthorized = "UNAUTHORIZED";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string StoreNotFound = "STORE_NOT_FOUND";
    public const string StoreNotApproved = "STORE_NOT_APPROVED";
}

public class AgentValidationFilter : IAsyncActionFilter
{
    private readonly IUnitOfWork _unitOfWork;

    public AgentValidationFilter(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            await next();
            return;
        }

        var error = await ValidateAsync(context.HttpContext.User);

        if (error is not null)
        {
            context.Result = new ObjectResult(new BaseResponse<string>
            {
                IsSuccess = false,
                Message = error.Value.Message,
                Data = error.Value.Code
            })
            {
                StatusCode = error.Value.StatusCode
            };
            return;
        }

        await next();
    }

    private async Task<(int StatusCode, string Code, string Message)?> ValidateAsync(ClaimsPrincipal user)
    {
        var queryAccount = _unitOfWork.GetRepository<Account>().Query().AsNoTracking();
        var queryStore = _unitOfWork.GetRepository<Store>().Query().AsNoTracking();

        if (user.Identity?.IsAuthenticated != true
            || !long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
            return (StatusCodes.Status401Unauthorized, AgentValidationCode.Unauthorized, "Bạn chưa đăng nhập");

        if (long.TryParse(user.FindFirstValue("exp"), out var exp)
            && DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow)
            return (StatusCodes.Status401Unauthorized, AgentValidationCode.TokenExpired, "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại");

        var accountValid = await queryAccount.AnyAsync(x => x.Id == accountId && x.IsActive && !x.IsDeleted);

        if (!accountValid)
            return (StatusCodes.Status401Unauthorized, AgentValidationCode.Unauthorized, "Tài khoản không tồn tại hoặc đã bị khoá");

        var store = await queryStore
            .Where(x => x.OwnerAccountId == accountId && !x.IsDeleted)
            .Select(x => new { x.IsApproved })
            .FirstOrDefaultAsync();

        if (store is null)
            return (StatusCodes.Status403Forbidden, AgentValidationCode.StoreNotFound, "Bạn chưa có thông tin cửa hàng");

        if (!store.IsApproved)
            return (StatusCodes.Status403Forbidden, AgentValidationCode.StoreNotApproved, "Cửa hàng của bạn chưa được duyệt");

        return null;
    }
}
