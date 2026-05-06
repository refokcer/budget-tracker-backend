namespace budget_tracker_backend.Controllers;

using System.Security.Claims;
using budget_tracker_backend.Data;
using budget_tracker_backend.Dto.UserProfile;
using budget_tracker_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized();

        return Ok(await BuildProfileAsync(user, cancellationToken));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserProfileDto dto, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized();

        var requestedEmail = NormalizeNullable(dto.Email);
        if (!string.IsNullOrWhiteSpace(requestedEmail)
            && !string.Equals(user.Email, requestedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existingEmailUser = await _userManager.FindByEmailAsync(requestedEmail);
            if (existingEmailUser != null && existingEmailUser.Id != user.Id)
                return BadRequest(new[] { "Email is already used by another account." });

            var emailResult = await _userManager.SetEmailAsync(user, requestedEmail);
            if (!emailResult.Succeeded)
                return BadRequest(emailResult.Errors);

            user.EmailConfirmed = false;
        }

        var requestedUserName = NormalizeNullable(dto.UserName) ?? requestedEmail;
        if (!string.IsNullOrWhiteSpace(requestedUserName)
            && !string.Equals(user.UserName, requestedUserName, StringComparison.OrdinalIgnoreCase))
        {
            var existingNameUser = await _userManager.FindByNameAsync(requestedUserName);
            if (existingNameUser != null && existingNameUser.Id != user.Id)
                return BadRequest(new[] { "Username is already used by another account." });

            var userNameResult = await _userManager.SetUserNameAsync(user, requestedUserName);
            if (!userNameResult.Succeeded)
                return BadRequest(userNameResult.Errors);
        }

        user.FullName = NormalizeNullable(dto.FullName);
        var phoneResult = await _userManager.SetPhoneNumberAsync(user, NormalizeNullable(dto.PhoneNumber));
        if (!phoneResult.Succeeded)
            return BadRequest(phoneResult.Errors);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(await BuildProfileAsync(user, cancellationToken));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new[] { "Current password and new password are required." });

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { changed = true });
    }

    private async Task<UserProfileDto> BuildProfileAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var stats = new UserProfileStatsDto
        {
            AccountsCount = await _context.Accounts.CountAsync(cancellationToken),
            CategoriesCount = await _context.Categories.CountAsync(cancellationToken),
            TransactionsCount = await _context.Transactions.CountAsync(cancellationToken),
            BudgetPlansCount = await _context.BudgetPlans.CountAsync(cancellationToken),
            FinancialGoalsCount = await _context.FinancialGoals.CountAsync(cancellationToken),
            TotalBalance = await _context.Accounts.SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m
        };

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount,
            Roles = roles.ToList(),
            Stats = stats
        };
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await _userManager.FindByIdAsync(userId);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
