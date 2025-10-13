using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 用户DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// </summary>
    public static class UserDtoExtensions
    {
        /// <summary>
        /// 将UserCreateDto转换为UserDto（用于创建预览）
        /// </summary>
        public static UserDto ToDto(this UserCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new UserDto
            {
                // 用户名映射：UserName → UserName
                UserName = dto.UserName,

                // 基本信息
                RealName = dto.RealName,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Role = dto.Role,

                // 系统字段（新建时的默认值）
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                FailedLoginCount = 0
                // Id, PinYinCode, LastLoginTime 保持默认null/0值
            };
        }

        /// <summary>
        /// 将UserUpdateDto的字段应用到现有UserDto（用于更新）
        /// 注意：UserUpdateDto中RealName和Role是可选字段
        /// </summary>
        public static void ApplyUpdate(this UserDto existing, UserUpdateDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // 条件更新：仅在不为空时更新
            if (!string.IsNullOrEmpty(dto.RealName))
                existing.RealName = dto.RealName;

            if (dto.Role.HasValue)
                existing.Role = dto.Role.Value;

            // 始终更新
            existing.PhoneNumber = dto.PhoneNumber;
            existing.Email = dto.Email;
            existing.Status = dto.Status;

            // 更新时间戳
            existing.UpdatedAt = DateTime.UtcNow;

            // 不更新：Id, UserName（用户名不允许修改）, PinYinCode, LastLoginTime, FailedLoginCount, CreatedAt
        }
    }
}
