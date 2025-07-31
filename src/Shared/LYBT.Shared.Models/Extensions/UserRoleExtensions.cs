using System;
using System.ComponentModel;
using System.Reflection;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 用户角色扩展方法
    /// </summary>
    public static class UserRoleExtensions
    {
        /// <summary>
        /// 获取用户角色的显示名称
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(this UserRole role)
        {
            var field = role.GetType().GetField(role.ToString());
            if (field != null)
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (attribute != null)
                {
                    return attribute.Description;
                }
            }
            return role.ToString();
        }

        /// <summary>
        /// 获取所有用户角色及其显示名称
        /// </summary>
        /// <returns>角色和显示名称的键值对数组</returns>
        public static (UserRole Role, string DisplayName)[] GetAllRolesWithDisplayNames()
        {
            var roles = Enum.GetValues<UserRole>();
            var result = new (UserRole, string)[roles.Length];
            
            for (int i = 0; i < roles.Length; i++)
            {
                result[i] = (roles[i], roles[i].GetDisplayName());
            }
            
            return result;
        }

        /// <summary>
        /// 从显示名称获取用户角色
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <returns>用户角色，如果未找到则返回null</returns>
        public static UserRole? GetRoleFromDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            var roles = Enum.GetValues<UserRole>();
            foreach (var role in roles)
            {
                if (role.GetDisplayName().Equals(displayName, StringComparison.OrdinalIgnoreCase))
                {
                    return role;
                }
            }
            
            return null;
        }

    }
}