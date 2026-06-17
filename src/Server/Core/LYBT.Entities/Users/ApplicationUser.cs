using Microsoft.AspNetCore.Identity;

namespace LYBT.Entities.Users;

public class ApplicationUser : IdentityUser<Guid>
{
    public string RealName { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
}
