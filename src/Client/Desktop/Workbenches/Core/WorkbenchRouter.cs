using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Workbench.Core
{
    /// <summary>
    /// 工作台路由实现
    /// </summary>
    public class WorkbenchRouter : IWorkbenchRouter
    {
        private readonly Dictionary<string, WorkbenchConfig> _workbenchConfigs;
        private readonly Dictionary<string, List<NavigationItem>> _navigationCache;

        public WorkbenchRouter()
        {
            _workbenchConfigs = new Dictionary<string, WorkbenchConfig>();
            _navigationCache = new Dictionary<string, List<NavigationItem>>();
            InitializeDefaultWorkbenches();
        }

        /// <summary>
        /// 初始化默认工作台配置
        /// </summary>
        private void InitializeDefaultWorkbenches()
        {
            // 管理员工作台 - 8个核心业务模块访问权限
            RegisterWorkbench("管理员", "SystemWorkbenchMainView", new List<string>
            {
                "Users", "Patients", "MedicalCase", "Consultation", 
                "Herbs", "Formula", "Prescriptions"
            });

            // 医生工作台 - 医生角色别名
            RegisterWorkbench("用户", "ConsultationWorkbenchMainView", new List<string>
            {
                "Patients", "Consultation", "Prescriptions", "Formula", "MedicalCase"
            });

            // 医生角色别名
            RegisterWorkbench("医生", "ConsultationWorkbenchMainView", new List<string>
            {
                "Patients", "Consultation", "Prescriptions", "Formula", "MedicalCase"
            });
        }

        public string GetWorkbenchForRole(string role)
        {
            if (string.IsNullOrEmpty(role))
                return "ConsultationWorkbenchMainView"; // 默认视图

            // UltraThink Phase 4.1: 优先使用UserRole枚举映射
            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                return WorkbenchPermissionMapper.GetWorkbenchForRole(userRole);
            }

            // 向后兼容：处理字符串角色
            var convertedRole = WorkbenchPermissionMapper.ConvertFromLegacyRoleString(role);
            return WorkbenchPermissionMapper.GetWorkbenchForRole(convertedRole);
        }

        public bool CanAccessModule(string role, string module)
        {
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(module))
                return false;

            // UltraThink Phase 4.1: 优先使用UserRole枚举映射
            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                return WorkbenchPermissionMapper.CanAccessModule(userRole, module);
            }

            // 向后兼容：处理字符串角色
            var convertedRole = WorkbenchPermissionMapper.ConvertFromLegacyRoleString(role);
            return WorkbenchPermissionMapper.CanAccessModule(convertedRole, module);
        }

        public IEnumerable<NavigationItem> GetNavigationItems(string role)
        {
            if (string.IsNullOrEmpty(role))
                return Enumerable.Empty<NavigationItem>();

            // 检查缓存
            if (_navigationCache.ContainsKey(role))
                return _navigationCache[role];

            // 生成导航项
            var navigationItems = GenerateNavigationItems(role);
            _navigationCache[role] = navigationItems;

            return navigationItems;
        }

        private List<NavigationItem> GenerateNavigationItems(string role)
        {
            var items = new List<NavigationItem>();

            switch (role)
            {
                case "管理员":
                    items.AddRange(GetAdminNavigationItems());
                    break;
                case "用户":
                case "医生":
                    items.AddRange(GetDoctorNavigationItems());
                    break;
                case "前台":
                    items.AddRange(GetReceptionNavigationItems());
                    break;
            }

            return items;
        }

        private IEnumerable<NavigationItem> GetAdminNavigationItems()
        {
            return new List<NavigationItem>
            {
                // 用户和患者管理
                new NavigationItem
                {
                    Id = "users",
                    DisplayName = "用户管理",
                    Icon = "\uE77B", // 用户图标
                    ViewName = "UserManagementView",
                    Module = "Users",
                    Order = 1
                },
                new NavigationItem
                {
                    Id = "patients",
                    DisplayName = "患者管理",
                    Icon = "\uE716", // 联系人图标
                    ViewName = "PatientManagementView",
                    Module = "Patients",
                    Order = 2
                },
                NavigationItem.CreateSeparator(),
                
                // 诊疗管理 - 修复View名称
                new NavigationItem
                {
                    Id = "medical-cases",
                    DisplayName = "医疗案例",
                    Icon = "\uE8C8", // 文档图标
                    ViewName = "MedicalCaseManagementView", // 修复：使用统一管理模块视图
                    Module = "MedicalCase",
                    Order = 3
                },
                new NavigationItem
                {
                    Id = "consultation",
                    DisplayName = "看诊记录",
                    Icon = "\uE8D4", // 检查图标
                    ViewName = "ConsultationManagementView", // 修复：使用管理模块视图
                    Module = "Consultation",
                    Order = 4
                },
                NavigationItem.CreateSeparator(),
                
                // 药材和处方管理
                new NavigationItem
                {
                    Id = "herbs",
                    DisplayName = "中药材管理",
                    Icon = "\uEB42", // 药品图标
                    ViewName = "HerbManagementView",
                    Module = "Herbs",
                    Order = 5
                },
                new NavigationItem
                {
                    Id = "formulas",
                    DisplayName = "验方模板",
                    Icon = "\uE8C7", // 方案图标
                    ViewName = "FormulaManagementView",
                    Module = "Formula",
                    Order = 6
                },
                new NavigationItem
                {
                    Id = "prescriptions",
                    DisplayName = "处方管理",
                    Icon = "\uE8D5", // 处方图标
                    ViewName = "PrescriptionManagementView",
                    Module = "Prescriptions",
                    Order = 7
                }
            };
        }

        private IEnumerable<NavigationItem> GetDoctorNavigationItems()
        {
            return new List<NavigationItem>
            {
                // 核心诊疗功能
                new NavigationItem
                {
                    Id = "consultation",
                    DisplayName = "看诊",
                    Icon = "Consultation",
                    ViewName = "ConsultationMainView",
                    Module = "Consultation",
                    Order = 1,
                    BadgeText = "3",
                    BadgeType = "info"
                },
                NavigationItem.CreateSeparator(),
                
                // 患者管理
                new NavigationItem
                {
                    Id = "patients",
                    DisplayName = "患者档案",
                    Icon = "Patients",
                    ViewName = "PatientManagementView",
                    Module = "Patients",
                    Order = 2
                },
                NavigationItem.CreateSeparator(),
                
                // 处方和验方
                new NavigationItem
                {
                    Id = "prescriptions",
                    DisplayName = "我的处方",
                    Icon = "MyPrescriptions",
                    ViewName = "PrescriptionManagementView",
                    Module = "Prescriptions",
                    Order = 3
                },
                new NavigationItem
                {
                    Id = "formulas",
                    DisplayName = "常用验方",
                    Icon = "Formulas",
                    ViewName = "FormulaManagementView",
                    Module = "Formula",
                    Order = 4
                },
                NavigationItem.CreateSeparator(),
                
                // 病历查询
                new NavigationItem
                {
                    Id = "medical-cases",
                    DisplayName = "病历查询",
                    Icon = "MedicalCase",
                    ViewName = "MedicalCaseListView",
                    Module = "MedicalCase",
                    Order = 5
                }
            };
        }

        private IEnumerable<NavigationItem> GetReceptionNavigationItems()
        {
            return new List<NavigationItem>
            {
                // 患者接待核心功能
                new NavigationItem
                {
                    Id = "patients",
                    DisplayName = "患者建档",
                    Icon = "NewPatient",
                    ViewName = "PatientManagementView",
                    Module = "Patients",
                    Order = 1
                },
                NavigationItem.CreateSeparator(),
                
                // 医疗案例查看
                new NavigationItem
                {
                    Id = "medical-cases",
                    DisplayName = "就诊记录",
                    Icon = "MedicalCase",
                    ViewName = "MedicalCaseListView",
                    Module = "MedicalCase",
                    Order = 2
                }
            };
        }

        public IEnumerable<string> GetAccessibleModules(string role)
        {
            if (string.IsNullOrEmpty(role))
                return Enumerable.Empty<string>();

            // UltraThink Phase 4.1: 优先使用UserRole枚举映射
            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                return WorkbenchPermissionMapper.GetAccessibleModules(userRole);
            }

            // 向后兼容：处理字符串角色
            var convertedRole = WorkbenchPermissionMapper.ConvertFromLegacyRoleString(role);
            return WorkbenchPermissionMapper.GetAccessibleModules(convertedRole);
        }

        public string GetDefaultView(string workbench)
        {
            switch (workbench)
            {
                case "SystemWorkbenchMainView":
                    return "UserManagementView"; // 管理员默认进入用户管理
                case "ConsultationWorkbenchMainView":
                    return "ConsultationMainView"; // 医生默认进入看诊界面
                case "ReceptionWorkbenchMainView":
                    return "PatientManagementView"; // 前台默认进入患者管理
                default:
                    return "ConsultationMainView"; // 默认看诊界面
            }
        }

        public void RegisterWorkbench(string role, string workbench, List<string> modules)
        {
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(workbench))
                return;

            _workbenchConfigs[role] = new WorkbenchConfig
            {
                Role = role,
                WorkbenchView = workbench,
                AccessibleModules = modules ?? new List<string>()
            };

            // 清除导航缓存
            if (_navigationCache.ContainsKey(role))
                _navigationCache.Remove(role);
        }

        public Dictionary<string, string> GetAllWorkbenches()
        {
            return _workbenchConfigs.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.WorkbenchView);
        }

        public bool IsWorkbenchRegistered(string workbench)
        {
            return _workbenchConfigs.Values.Any(c => c.WorkbenchView == workbench);
        }

        public string GetWelcomeMessage(string role, string userName)
        {
            // UltraThink Phase 4.1: 优先使用UserRole枚举映射
            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                return WorkbenchPermissionMapper.GetWelcomeMessage(userRole, userName);
            }

            // 向后兼容：处理字符串角色
            var convertedRole = WorkbenchPermissionMapper.ConvertFromLegacyRoleString(role);
            return WorkbenchPermissionMapper.GetWelcomeMessage(convertedRole, userName);
        }

        public string GetRoleDisplayName(string role)
        {
            // UltraThink Phase 4.1: 优先使用UserRole枚举映射
            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                return WorkbenchPermissionMapper.GetRoleDisplayName(userRole);
            }

            // 向后兼容：处理字符串角色
            var convertedRole = WorkbenchPermissionMapper.ConvertFromLegacyRoleString(role);
            return WorkbenchPermissionMapper.GetRoleDisplayName(convertedRole);
        }

        /// <summary>
        /// 内部配置类
        /// </summary>
        private class WorkbenchConfig
        {
            public string Role { get; set; } = string.Empty;
            public string WorkbenchView { get; set; } = string.Empty;
            public List<string> AccessibleModules { get; set; } = new List<string>();
        }
    }
}

/// <summary>
/// UltraThink Phase 4.1: UserRole到工作台权限映射器
/// 支持UserRole枚举到工作台的正确映射
/// </summary>
namespace LYBT.Desktop.Workbench.Core
{
    public static class WorkbenchPermissionMapper
    {
    /// <summary>
    /// UserRole到工作台的映射关系
    /// </summary>
    private static readonly Dictionary<UserRole, WorkbenchPermission> UserRoleWorkbenchMap = new()
    {
        // 管理员 - 系统管理工作台，拥有8个核心业务模块的完整权限
        {
            UserRole.Admin, 
            new WorkbenchPermission 
            {
                WorkbenchView = "SystemWorkbenchMainView",
                AccessibleModules = new List<string> 
                { 
                    "Users", "Patients", "MedicalCase", "Consultation", 
                    "Herbs", "Formula", "Prescriptions"
                },
                DisplayName = "系统管理员",
                WelcomeTemplate = "欢迎您，{0}！\n\n系统管理工作台正在加载..."
            }
        },
        
        // 医生 - 诊疗工作台
        {
            UserRole.Doctor, 
            new WorkbenchPermission 
            {
                WorkbenchView = "ConsultationWorkbenchMainView",
                AccessibleModules = new List<string> 
                { 
                    "Patients", "Consultation", "Prescriptions", "Formula", "MedicalCase"
                },
                DisplayName = "医生",
                WelcomeTemplate = "欢迎您，{0}医生！\n\n诊疗工作台正在准备..."
            }
        },
        
        // 前台接待 - 患者管理为主
        {
            UserRole.Receptionist, 
            new WorkbenchPermission 
            {
                WorkbenchView = "ReceptionWorkbenchMainView",
                AccessibleModules = new List<string> 
                { 
                    "Patients", "MedicalCase"
                },
                DisplayName = "前台接待",
                WelcomeTemplate = "欢迎您，{0}！\n\n接待工作台正在启动..."
            }
        },
        
        // 药剂师 - 药房工作台
        {
            UserRole.Pharmacist, 
            new WorkbenchPermission 
            {
                WorkbenchView = "PharmacistWorkbenchMainView",
                AccessibleModules = new List<string> 
                { 
                    "Herbs", "Prescriptions", "Formula", "Patients"
                },
                DisplayName = "药剂师",
                WelcomeTemplate = "欢迎您，{0}！\n\n药房工作台正在启动..."
            }
        }
    };

    /// <summary>
    /// 根据UserRole获取工作台视图名称
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <returns>工作台视图名称</returns>
    public static string GetWorkbenchForRole(UserRole role)
    {
        return UserRoleWorkbenchMap.TryGetValue(role, out var permission) 
            ? permission.WorkbenchView 
            : "ConsultationWorkbenchMainView"; // 默认诊疗工作台
    }

    /// <summary>
    /// 检查用户角色是否可以访问指定模块
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <param name="module">模块名称</param>
    /// <returns>是否有访问权限</returns>
    public static bool CanAccessModule(UserRole role, string module)
    {
        if (!UserRoleWorkbenchMap.TryGetValue(role, out var permission))
            return false;

        return permission.AccessibleModules.Contains(module);
    }

    /// <summary>
    /// 获取用户角色可访问的所有模块
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <returns>可访问的模块列表</returns>
    public static IEnumerable<string> GetAccessibleModules(UserRole role)
    {
        return UserRoleWorkbenchMap.TryGetValue(role, out var permission) 
            ? permission.AccessibleModules 
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// 获取角色显示名称
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <returns>角色显示名称</returns>
    public static string GetRoleDisplayName(UserRole role)
    {
        return UserRoleWorkbenchMap.TryGetValue(role, out var permission) 
            ? permission.DisplayName 
            : "用户";
    }

    /// <summary>
    /// 获取欢迎消息
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <param name="userName">用户姓名</param>
    /// <returns>欢迎消息</returns>
    public static string GetWelcomeMessage(UserRole role, string userName)
    {
        if (UserRoleWorkbenchMap.TryGetValue(role, out var permission))
        {
            return string.Format(permission.WelcomeTemplate, userName);
        }
        
        return $"欢迎您，{userName}！\n\n工作台正在加载...";
    }

    /// <summary>
    /// 获取所有支持的角色工作台映射
    /// </summary>
    /// <returns>角色到工作台的映射字典</returns>
    public static Dictionary<UserRole, string> GetAllWorkbenchMappings()
    {
        return UserRoleWorkbenchMap.ToDictionary(
            kvp => kvp.Key, 
            kvp => kvp.Value.WorkbenchView
        );
    }

    /// <summary>
    /// 检查角色是否有管理权限
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <returns>是否有管理权限</returns>
    public static bool HasManagementAccess(UserRole role)
    {
        return role == UserRole.Admin;
    }

    /// <summary>
    /// 检查角色是否有医疗权限
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <returns>是否有医疗权限</returns>
    public static bool HasMedicalAccess(UserRole role)
    {
        return role == UserRole.Doctor || role == UserRole.Admin;
    }

    /// <summary>
    /// 从UserRole枚举转换为旧版字符串角色（向后兼容）
    /// </summary>
    /// <param name="role">UserRole枚举</param>
    /// <returns>字符串角色名称</returns>
    public static string ConvertToLegacyRoleString(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "管理员",
            UserRole.Doctor => "医生",
            UserRole.Receptionist => "前台",
            UserRole.Cashier => "收银员",
            UserRole.Pharmacist => "药剂师",
            UserRole.Therapist => "理疗师",
            _ => "用户"
        };
    }

    /// <summary>
    /// 从字符串角色转换为UserRole枚举
    /// </summary>
    /// <param name="roleString">字符串角色名称</param>
    /// <returns>UserRole枚举</returns>
    public static UserRole ConvertFromLegacyRoleString(string roleString)
    {
        return roleString switch
        {
            "管理员" => UserRole.Admin,
            "医生" or "用户" => UserRole.Doctor, // "用户"映射为医生角色
            "前台" => UserRole.Receptionist,
            "收银员" => UserRole.Cashier,
            "药剂师" => UserRole.Pharmacist,
            "理疗师" => UserRole.Therapist,
            _ => UserRole.Doctor // 默认为医生角色
        };
    }
}

/// <summary>
/// 工作台权限配置
/// </summary>
public class WorkbenchPermission
{
    /// <summary>
    /// 工作台视图名称
    /// </summary>
    public string WorkbenchView { get; set; } = string.Empty;

    /// <summary>
    /// 可访问的模块列表
    /// </summary>
    public List<string> AccessibleModules { get; set; } = new();

    /// <summary>
    /// 角色显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 欢迎消息模板（使用{0}占位符表示用户名）
    /// </summary>
    public string WelcomeTemplate { get; set; } = string.Empty;
}
}