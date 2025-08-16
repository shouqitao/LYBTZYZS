using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Configuration
{
    /// <summary>
    /// 角色导航配置 - 简化版（只有管理员和普通用户）
    /// </summary>
    public static class RoleNavigationConfig
    {
        /// <summary>
        /// 获取角色对应的主界面视图名称
        /// </summary>
        /// <param name="role">用户角色（管理员或用户）</param>
        /// <returns>主界面视图名称</returns>
        public static string GetMainViewName(string role)
        {
            return role switch
            {
                "管理员" => "AdminMainView",
                "用户" => "ConsultationMainView",
                _ => "ConsultationMainView"  // 默认显示看诊界面
            };
        }

        /// <summary>
        /// 获取角色对应的欢迎消息
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="userName">用户姓名</param>
        /// <returns>欢迎消息</returns>
        public static string GetWelcomeMessage(string role, string userName)
        {
            return role switch
            {
                "管理员" => $"欢迎您，{userName}！\n\n系统管理模块正在加载...",
                "用户" => $"欢迎您，{userName}医生！\n\n诊疗工作台正在准备...",
                _ => $"欢迎您，{userName}！\n\n工作台正在加载..."
            };
        }

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>角色显示名称</returns>
        public static string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "管理员" => "系统管理员",
                "用户" => "医生",
                _ => "用户"
            };
        }

        /// <summary>
        /// 检查角色是否有管理权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有管理权限</returns>
        public static bool HasManagementAccess(string role)
        {
            return role == "管理员";
        }

        /// <summary>
        /// 检查角色是否有医疗权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有医疗权限</returns>
        public static bool HasMedicalAccess(string role)
        {
            return role == "用户" || role == "管理员";  // 管理员也可以看诊
        }
    }
}