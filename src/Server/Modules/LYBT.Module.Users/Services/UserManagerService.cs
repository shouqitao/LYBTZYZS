using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Services;

public class UserManagerService : IUserManagerService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagerService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<ApplicationUser?> FindByNameAsync(string userName)
        => _userManager.FindByNameAsync(userName);

    public Task<ApplicationUser?> FindByIdAsync(Guid id)
        => _userManager.FindByIdAsync(id.ToString());

    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
        => _userManager.CheckPasswordAsync(user, password);

    public Task<IList<string>> GetRolesAsync(ApplicationUser user)
        => _userManager.GetRolesAsync(user);

    public Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        => _userManager.CreateAsync(user, password);

    public Task<IdentityResult> UpdateAsync(ApplicationUser user)
        => _userManager.UpdateAsync(user);

    public Task<IdentityResult> DeleteAsync(ApplicationUser user)
        => _userManager.DeleteAsync(user);

    public Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
        => _userManager.AddToRoleAsync(user, role);

    public Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role)
        => _userManager.RemoveFromRoleAsync(user, role);

    public Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        => _userManager.IsInRoleAsync(user, role);

    public Task<IQueryable<ApplicationUser>> GetUsersAsync()
        => Task.FromResult(_userManager.Users.AsQueryable());

    public Task<int> GetUsersCountAsync()
        => _userManager.Users.CountAsync();

    public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        => _userManager.GeneratePasswordResetTokenAsync(user);

    public Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        => _userManager.ResetPasswordAsync(user, token, newPassword);

    public Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        => _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

    public Task<IdentityResult> SetLockoutEnabledAsync(ApplicationUser user, bool enabled)
        => _userManager.SetLockoutEnabledAsync(user, enabled);

    public Task<IdentityResult> SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd)
        => _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user)
        => _userManager.GetLockoutEnabledAsync(user);

    public Task<int> GetAccessFailedCountAsync(ApplicationUser user)
        => _userManager.GetAccessFailedCountAsync(user);
}
