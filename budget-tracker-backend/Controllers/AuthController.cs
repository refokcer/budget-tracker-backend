using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using budget_tracker_backend.Dto.Auth;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Auth;

namespace budget_tracker_backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var refreshToken = _tokenService.CreateRefreshToken();
        user.RefreshTokens.Add(refreshToken);
        await _userManager.UpdateAsync(user);
        return Ok(new AuthResponseDto { AccessToken = accessToken, RefreshToken = refreshToken.Token });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var refreshToken = _tokenService.CreateRefreshToken();
        user.RefreshTokens.Add(refreshToken);
        await _userManager.UpdateAsync(user);
        return Ok(new AuthResponseDto { AccessToken = accessToken, RefreshToken = refreshToken.Token });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal?.Identity?.Name == null)
            return BadRequest();
        var user = await _userManager.FindByNameAsync(principal.Identity.Name);
        if (user == null)
            return BadRequest();

        var refreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == request.RefreshToken && x.IsActive);
        if (refreshToken == null)
            return Unauthorized();

        refreshToken.Revoked = DateTime.UtcNow;
        var newRefreshToken = _tokenService.CreateRefreshToken();
        user.RefreshTokens.Add(newRefreshToken);
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.CreateAccessToken(user, roles);
        await _userManager.UpdateAsync(user);
        return Ok(new AuthResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken.Token });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
    {
        var user = await _userManager.FindByNameAsync(User.Identity!.Name);
        if (user == null)
            return BadRequest();
        var refreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == request.RefreshToken && x.IsActive);
        if (refreshToken != null)
        {
            refreshToken.Revoked = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
        return Ok();
    }
}
