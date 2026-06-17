using Microsoft.AspNetCore.Identity;

namespace LYBT.Entities.Users;

public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
}
