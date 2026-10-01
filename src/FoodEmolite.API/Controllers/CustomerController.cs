using FoodEmolite.Application.DTOs.Customer;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomerController : BaseApiController
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost("agent/search")]
    public async Task<IActionResult> SearchAgentCustomers([FromBody] BaseSearchRequest<CustomerSearchRequest> request)
    {
        var result = await _customerService.GetAgentCustomersAsync(CurrentUserId!.Value, request);

        return Ok(result);
    }

    [HttpGet("agent/{refCode}")]
    public async Task<IActionResult> GetAgentCustomerDetail(string refCode, [FromQuery] bool isGuest = false)
        => Ok(await _customerService.GetAgentCustomerDetailAsync(CurrentUserId!.Value, refCode, isGuest));
}
