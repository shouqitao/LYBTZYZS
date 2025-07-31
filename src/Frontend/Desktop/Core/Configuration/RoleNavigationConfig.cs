using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Configuration {
    /// <summary>
    /// 角色导航配置
    /// </summary>
    public static class RoleNavigationConfig {
        /// <summary>
        /// 获取角色对应的主界面视图名称
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>主界面视图名称</returns>
        public static string GetMainViewName(UserRole role) {
            return role switch {
                UserRole.Admin => "SystemManagementView",
                UserRole.DiagnosingDoctor => "ConsultationView",
                UserRole.CashierStaff => "CashierMainView",
                UserRole.PharmacyStaff => "PharmacyMainView",
                UserRole.PhysiotherapyStaff => "PhysiotherapyMainView",
                UserRole.Staff => "StaffMainView",
                _ => "DefaultView"
            };
        }

        /// <summary>
        /// 获取角色对应的欢迎消息
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="userName">用户姓名</param>
        /// <returns>欢迎消息</returns>
        public static string GetWelcomeMessage(UserRole role, string userName) {
            var roleDisplay = GetRoleDisplayName(role);
            return role switch {
                UserRole.Admin => $"欢迎您，{userName}！\n\n管理员系统管理模块正在加载...",
                UserRole.DiagnosingDoctor => $"欢迎您，{userName}医生！\n\n诊疗工作台正在准备...",
                UserRole.CashierStaff => $"欢迎您，{userName}！\n\n收银系统正在启动...",
                UserRole.PharmacyStaff => $"欢迎您，{userName}！\n\n药房管理系统正在加载...",
                UserRole.PhysiotherapyStaff => $"欢迎您，{userName}！\n\n理疗工作台正在准备...",
                UserRole.Staff => $"欢迎您，{userName}！\n\n员工工作台正在加载...",
                _ => $"欢迎您，{userName}！\n\n{roleDisplay}工作台正在加载..."
            };
        }

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>角色显示名称</returns>
        public static string GetRoleDisplayName(UserRole role) {
            return role switch {
                UserRole.Admin => "管理员",
                UserRole.DiagnosingDoctor => "诊疗医生",
                UserRole.CashierStaff => "收银员",
                UserRole.PharmacyStaff => "药房人员",
                UserRole.PhysiotherapyStaff => "理疗师",
                UserRole.Staff => "员工",
                _ => "未知角色"
            };
        }

        /// <summary>
        /// 检查角色是否有管理权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有管理权限</returns>
        public static bool HasManagementAccess(UserRole role) {
            return role == UserRole.Admin;
        }

        /// <summary>
        /// 检查角色是否有医疗权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有医疗权限</returns>
        public static bool HasMedicalAccess(UserRole role) {
            return role == UserRole.DiagnosingDoctor ||
                   role == UserRole.PharmacyStaff ||
                   role == UserRole.PhysiotherapyStaff;
        }

        /// <summary>
        /// 检查角色是否有前台权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有前台权限</returns>
        public static bool HasFrontDeskAccess(UserRole role) {
            return role == UserRole.Staff || HasManagementAccess(role);
        }

        /// <summary>
        /// 检查角色是否有财务权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有财务权限</returns>
        public static bool HasFinanceAccess(UserRole role) {
            return role == UserRole.CashierStaff || role == UserRole.Admin;
        }
    }
}