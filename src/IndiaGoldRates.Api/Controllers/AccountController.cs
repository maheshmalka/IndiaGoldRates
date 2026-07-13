using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndiaGoldRates.Api.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            email = User.FindFirstValue(ClaimTypes.Email),
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        });
    }
}
