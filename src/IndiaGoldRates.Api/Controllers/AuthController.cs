using System.Security.Claims;
using IndiaGoldRates.Core.Entities;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IndiaGoldRates.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    private string FrontendBaseUrl => configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";

    /// <summary>Kicks off the external OAuth challenge. provider is "google" or "microsoft".</summary>
    [HttpGet("login/{provider}")]
    public IActionResult Login(string provider, [FromQuery] string? returnUrl = null)
    {
        var scheme = provider.ToLowerInvariant() switch
        {
            "google" => GoogleDefaults.AuthenticationScheme,
            "microsoft" => MicrosoftAccountDefaults.AuthenticationScheme,
            _ => null
        };

        if (scheme is null)
        {
            return BadRequest(new { message = $"Unknown provider '{provider}'. Use 'google' or 'microsoft'." });
        }

        var redirectUrl = Url.Action(nameof(Callback), values: new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(scheme, redirectUrl);
        return Challenge(properties, scheme);
    }

    /// <summary>Shared callback for both providers — signs the user in, creating an account on first login.</summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? returnUrl = null)
    {
        var errorRedirect = $"{FrontendBaseUrl}/login?error=external_auth_failed";

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            logger.LogWarning("External login callback reached with no external login info.");
            return Redirect(errorRedirect);
        }

        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);

        if (!signInResult.Succeeded)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email is null)
            {
                logger.LogWarning("External login from {Provider} did not include an email claim.", info.LoginProvider);
                return Redirect(errorRedirect);
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    logger.LogWarning("Failed to create user for {Email}: {Errors}",
                        email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Redirect(errorRedirect);
                }
            }

            var addLoginResult = await userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
            {
                logger.LogWarning("Failed to associate {Provider} login with {Email}: {Errors}",
                    info.LoginProvider, email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                return Redirect(errorRedirect);
            }

            await signInManager.SignInAsync(user, isPersistent: true);
        }

        var target = string.IsNullOrEmpty(returnUrl) ? FrontendBaseUrl : $"{FrontendBaseUrl}{returnUrl}";
        return Redirect(target);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok();
    }
}
