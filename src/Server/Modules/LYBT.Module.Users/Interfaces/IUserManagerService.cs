using LYBT.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Interfaces;

public interface IUserManagerService
{
    Task<ApplicationUser?> FindByNameAsync(string userName);
    Task<ApplicationUser?> FindByIdAsync(Guid id);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
    Task<IdentityResult> UpdateAsync(ApplicationUser user);
    Task<IdentityResult> DeleteAsync(ApplicationUser user);
    Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role);
    Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role);
    Task<bool> IsInRoleAsync(ApplicationUser user, string role);
    Task<IQueryable<ApplicationUser>> GetUsersAsync();
    Task<int> GetUsersCountAsync();
    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
    Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);
    Task<IdentityResult> SetLockoutEnabledAsync(ApplicationUser user, bool enabled);
    Task<IdentityResult> SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd);
    Task<bool> GetLockoutEnabledAsync(ApplicationUser user);
    Task<int> GetAccessFailedCountAsync(ApplicationUser user);
}
