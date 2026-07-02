using LYBT.Shared.Models.Contracts.Users;
using LYBT.Entities.Users;

namespace LYBT.Module.Users.Application.Mappers;

public static class UserMapper
{
    public static UserListDto ToListDto(ApplicationUser entity) => new()
    {
        Id = entity.Id,
        UserName = entity.UserName,
        RealName = entity.RealName,
        PhoneNumber = entity.PhoneNumber,
        Role = entity.Role,
        Status = entity.Status,
        LastLoginTime = entity.LastLoginAt,
        CreatedAt = entity.CreatedAt
    };

    public static UserDetailDto ToDetailDto(ApplicationUser entity) => new()
    {
        Id = entity.Id,
        UserName = entity.UserName,
        RealName = entity.RealName,
        Role = entity.Role,
        Status = entity.Status,
        PhoneNumber = entity.PhoneNumber,
        Email = entity.Email,
        PinYinCode = entity.PinYinCode,
        LastLoginTime = entity.LastLoginAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Remark = entity.Remark
    };
}


