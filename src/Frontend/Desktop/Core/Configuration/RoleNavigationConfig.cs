using LYBT.WPF.Client.Core.Enums;

namespace LYBT.WPF.Client.Core.Configuration
{
    /// <summary>
    /// 角色导航配置
    /// </summary>
    public static class RoleNavigationConfig
    {
        /// <summary>
        /// 获取角色对应的主界面视图名称
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>主界面视图名称</returns>
        public static string GetMainViewName(UserRole role)
        {
            return role switch
            {
                UserRole.SuperAdmin => "SystemManagementView",
                UserRole.Admin => "SystemManagementView",
                UserRole.DiagnosingDoctor => "ConsultationView",
                UserRole.InternDoctor => "ConsultationView",
                UserRole.FrontDesk => "FrontDeskMainView",
                UserRole.Cashier => "CashierMainView",
                UserRole.Pharmacist => "PharmacyMainView",
                UserRole.Nurse => "NursingMainView",
                UserRole.Vendor => "VendorPortalView",
                UserRole.Guest => "GuestView",
                _ => "DefaultView"
            };
        }

        /// <summary>
        /// 获取角色对应的欢迎消息
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="userName">用户姓名</param>
        /// <returns>欢迎消息</returns>
        public static string GetWelcomeMessage(UserRole role, string userName)
        {
            var roleDisplay = GetRoleDisplayName(role);
            return role switch
            {
                UserRole.SuperAdmin => $"欢迎您，{userName}！\n\n超级管理员系统管理模块正在加载...",
                UserRole.Admin => $"欢迎您，{userName}！\n\n管理员系统管理模块正在加载...",
                UserRole.DiagnosingDoctor => $"欢迎您，{userName}医生！\n\n诊疗工作台正在准备...",
                UserRole.InternDoctor => $"欢迎您，{userName}医师！\n\n实习诊疗工作台正在准备...",
                UserRole.FrontDesk => $"欢迎您，{userName}！\n\n前台工作台正在准备...",
                UserRole.Cashier => $"欢迎您，{userName}！\n\n收银系统正在启动...",
                UserRole.Pharmacist => $"欢迎您，{userName}！\n\n药房管理系统正在加载...",
                UserRole.Nurse => $"欢迎您，{userName}！\n\n护理工作台正在准备...",
                _ => $"欢迎您，{userName}！\n\n{roleDisplay}工作台正在加载..."
            };
        }

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>角色显示名称</returns>
        public static string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.SuperAdmin => "超级管理员",
                UserRole.Admin => "管理员",
                UserRole.DiagnosingDoctor => "医生",
                UserRole.FrontDesk => "前台",
                UserRole.Cashier => "收银员",
                UserRole.Pharmacist => "药剂师",
                UserRole.Nurse => "护士",
                UserRole.InternDoctor => "实习医师",
                UserRole.Vendor => "供应商",
                UserRole.Guest => "访客",
                _ => "未知角色"
            };
        }

        /// <summary>
        /// 检查角色是否有管理权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有管理权限</returns>
        public static bool HasManagementAccess(UserRole role)
        {
            return role == UserRole.SuperAdmin || role == UserRole.Admin;
        }

        /// <summary>
        /// 检查角色是否有医疗权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有医疗权限</returns>
        public static bool HasMedicalAccess(UserRole role)
        {
            return role == UserRole.DiagnosingDoctor || 
                   role == UserRole.InternDoctor || 
                   role == UserRole.Nurse ||
                   role == UserRole.Pharmacist;
        }

        /// <summary>
        /// 检查角色是否有前台权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有前台权限</returns>
        public static bool HasFrontDeskAccess(UserRole role)
        {
            return role == UserRole.FrontDesk || HasManagementAccess(role);
        }

        /// <summary>
        /// 检查角色是否有财务权限
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>是否有财务权限</returns>
        public static bool HasFinanceAccess(UserRole role)
        {
            return role == UserRole.Cashier || role == UserRole.SuperAdmin;
        }
    }
}