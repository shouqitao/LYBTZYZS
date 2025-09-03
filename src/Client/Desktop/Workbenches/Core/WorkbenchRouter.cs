using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Workbench.Core;

/// <summary>
/// 工作台路由器 - 统一工作台导航和权限管理
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 提供基于角色的工作台视图路由、模块权限控制和导航项生成
/// 支持UserRole枚举映射和字符串角色向后兼容
/// 集成权限验证、缓存优化，适配小型诊所多角色工作需求
/// </summary>
public class WorkbenchRouter : IWorkbenchRouter
{
    private readonly Dictionary<string, WorkbenchConfig> _workbenchConfigs = [];
    private readonly Dictionary<string, List<NavigationItem>> _navigationCache = [];

    /// <summary>
    /// 初始化工作台路由器
    /// 配置默认工作台和角色权限映射
    /// </summary>
    public WorkbenchRouter()
    {
        InitializeDefaultWorkbenches();
    }

    /// <summary>
    /// 初始化默认工作台配置
    /// 使用C# 12集合表达式提升代码简洁性
    /// </summary>
    private void InitializeDefaultWorkbenches()
    {
        // 管理员工作台 - 8个核心业务模块访问权限
        RegisterWorkbench("管理员", "SystemWorkbenchMainView", [
            "Users", "Patients", "MedicalCase", "Consultation", 
            "Herbs", "Formula", "Prescriptions"
        ]);

        // 医生工作台 - 医生角色别名
        RegisterWorkbench("用户", "ConsultationWorkbenchMainView", [
            "Patients", "Consultation", "Prescriptions", "Formula", "MedicalCase"
        ]);

        // 医生角色别名
        RegisterWorkbench("医生", "ConsultationWorkbenchMainView", [
            "Patients", "Consultation", "Prescriptions", "Formula", "MedicalCase"
        ]);
    }

    /// <summary>
    /// 根据用户角色获取对应的工作台视图
    /// UltraThink现代化：优先使用UserRole枚举映射，向后兼容字符串角色
    /// </summary>
    /// <param name="role">用户角色字符串</param>
    /// <returns>工作台视图名称，默认为诊疗工作台</returns>
    public string GetWorkbenchForRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
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

    /// <summary>
    /// 检查用户角色是否可以访问指定模块
    /// 企业级权限控制：支持UserRole枚举和字符串角色双重验证
    /// </summary>
    /// <param name="role">用户角色字符串</param>
    /// <param name="module">业务模块名称</param>
    /// <returns>是否具有访问权限</returns>
    public bool CanAccessModule(string role, string module)
    {
        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(module))
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

    /// <summary>
    /// 获取用户角色对应的导航项集合
    /// 智能缓存机制：首次生成后缓存，提升导航性能
    /// </summary>
    /// <param name="role">用户角色字符串</param>
    /// <returns>导航项集合，包含图标、徽章、排序信息</returns>
    public IEnumerable<NavigationItem> GetNavigationItems(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return [];

        // 检查缓存 - 使用TryGetValue提升性能
        if (_navigationCache.TryGetValue(role, out var cachedItems))
            return cachedItems;

        // 生成导航项
        var navigationItems = GenerateNavigationItems(role);
        _navigationCache[role] = navigationItems;

        return navigationItems;
    }

    /// <summary>
    /// 根据角色生成对应的导航项列表
    /// C# 12模式匹配优化：简化角色判断逻辑
    /// </summary>
    /// <param name="role">用户角色字符串</param>
    /// <returns>导航项列表</returns>
    private List<NavigationItem> GenerateNavigationItems(string role)
    {
        return role switch
        {
            "管理员" => [.. GetAdminNavigationItems()],
            "用户" or "医生" => [.. GetDoctorNavigationItems()],
            "前台" => [.. GetReceptionNavigationItems()],
            _ => []
        };
    }

    /// <summary>
    /// 获取管理员角色导航项
    /// 8个核心业务模块完整权限：用户、患者、医案、诊断、药材、验方、处方
    /// </summary>
    /// <returns>管理员导航项集合</returns>
    private IEnumerable<NavigationItem> GetAdminNavigationItems()
    {
        return
        [
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
        ];
    }

    /// <summary>
    /// 获取医生角色导航项
    /// 核心诊疗功能：看诊、患者档案、处方管理、病历查询
    /// </summary>
    /// <returns>医生导航项集合</returns>
    private IEnumerable<NavigationItem> GetDoctorNavigationItems()
    {
        return
        [
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
        ];
    }

    /// <summary>
    /// 获取前台接待角色导航项
    /// 基础接待功能：患者建档、就诊记录查看
    /// </summary>
    /// <returns>前台接待导航项集合</returns>
    private IEnumerable<NavigationItem> GetReceptionNavigationItems()
    {
        return
        [
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
        ];
    }

    /// <summary>
    /// 获取用户角色可访问的模块列表
    /// 权限控制核心：基于角色返回可操作的业务模块集合
    /// </summary>
    /// <param name="role">用户角色字符串</param>
    /// <returns>可访问模块名称集合</returns>
    public IEnumerable<string> GetAccessibleModules(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return [];

        // UltraThink Phase 4.1: 优先使用UserRole枚举映射
        if (Enum.TryParse<UserRole>(role, out var userRole))
        {
            return WorkbenchPermissionMapper.GetAccessibleModules(userRole);
        }

        // 向后兼容：处理字符串角色
        var convertedRole = WorkbenchPermissionMapper.ConvertFromLegacyRoleString(role);
        return WorkbenchPermissionMapper.GetAccessibleModules(convertedRole);
    }

    /// <summary>
    /// 获取工作台的默认视图
    /// 角色导航优化：根据工作台类型返回最合适的默认视图
    /// </summary>
    /// <param name="workbench">工作台名称</param>
    /// <returns>默认视图名称</returns>
    public string GetDefaultView(string workbench)
    {
        return workbench switch
        {
            "SystemWorkbenchMainView" => "UserManagementView", // 管理员默认进入用户管理
            "ConsultationWorkbenchMainView" => "ConsultationMainView", // 医生默认进入看诊界面
            "ReceptionWorkbenchMainView" => "PatientManagementView", // 前台默认进入患者管理
            _ => "ConsultationMainView" // 默认看诊界面
        };
    }

    /// <summary>
    /// 注册工作台配置
    /// 动态工作台管理：运行时注册新的角色-工作台映射关系
    /// </summary>
    /// <param name="role">用户角色名称</param>
    /// <param name="workbench">工作台视图名称</param>
    /// <param name="modules">可访问模块列表</param>
    public void RegisterWorkbench(string role, string workbench, List<string> modules)
    {
        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(workbench))
            return;

        _workbenchConfigs[role] = new WorkbenchConfig
        {
            Role = role,
            WorkbenchView = workbench,
            AccessibleModules = modules ?? []
        };

        // 清除导航缓存 - 使用Remove替代ContainsKey+Remove组合
        _navigationCache.Remove(role);
    }

    /// <summary>
    /// 获取所有已注册的工作台映射
    /// 工作台枚举：返回角色到工作台视图的完整映射关系
    /// </summary>
    /// <returns>角色名称到工作台视图的映射字典</returns>
    public Dictionary<string, string> GetAllWorkbenches()
    {
        return _workbenchConfigs.ToDictionary(
            kvp => kvp.Key, 
            kvp => kvp.Value.WorkbenchView);
    }

    /// <summary>
    /// 检查工作台是否已注册
    /// 工作台验证：确认指定工作台视图是否在系统中已配置
    /// </summary>
    /// <param name="workbench">工作台视图名称</param>
    /// <returns>是否已注册</returns>
    public bool IsWorkbenchRegistered(string workbench)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workbench, nameof(workbench));
        return _workbenchConfigs.Values.Any(c => c.WorkbenchView == workbench);
    }

    /// <summary>
    /// 获取用户欢迎消息
    /// 个性化体验：根据用户角色和姓名生成定制化欢迎信息
    /// </summary>
    /// <param name="role">用户角色字符串</param>
    /// <param name="userName">用户姓名</param>
    /// <returns>个性化欢迎消息</returns>
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

    /// <summary>
    /// 获取角色显示名称
    /// 本地化支持：将内部角色标识转换为用户友好的中文显示名称
    /// </summary>
    /// <param name="role">用户角色字符串</param>
    /// <returns>角色的中文显示名称</returns>
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
    /// 工作台配置内部类
    /// UltraThink架构：封装角色-工作台-模块的映射配置信息
    /// </summary>
    private sealed class WorkbenchConfig
    {
        /// <summary>
        /// 用户角色名称
        /// </summary>
        public required string Role { get; init; } = string.Empty;

        /// <summary>
        /// 工作台视图名称
        /// </summary>
        public required string WorkbenchView { get; init; } = string.Empty;

        /// <summary>
        /// 可访问的业务模块列表
        /// </summary>
        public List<string> AccessibleModules { get; init; } = [];
    }
}

/// <summary>
/// UltraThink Phase 4.1: UserRole到工作台权限映射器
/// 支持UserRole枚举到工作台的正确映射
/// </summary>
public static class WorkbenchPermissionMapper
{
    /// <summary>
    /// UserRole到工作台的映射关系
    /// C# 12集合表达式：简化配置定义，提升可读性
    /// </summary>
    private static readonly Dictionary<UserRole, WorkbenchPermission> UserRoleWorkbenchMap = new()
    {
        // 管理员 - 系统管理工作台，拥有8个核心业务模块的完整权限
        [UserRole.Admin] = new()
        {
            WorkbenchView = "SystemWorkbenchMainView",
            AccessibleModules = [
                "Users", "Patients", "MedicalCase", "Consultation", 
                "Herbs", "Formula", "Prescriptions"
            ],
            DisplayName = "系统管理员",
            WelcomeTemplate = "欢迎您，{0}！\n\n系统管理工作台正在加载..."
        },
        
        // 医生 - 诊疗工作台
        [UserRole.Doctor] = new()
        {
            WorkbenchView = "ConsultationWorkbenchMainView",
            AccessibleModules = [
                "Patients", "Consultation", "Prescriptions", "Formula", "MedicalCase"
            ],
            DisplayName = "医生",
            WelcomeTemplate = "欢迎您，{0}医生！\n\n诊疗工作台正在准备..."
        },
        
        // 前台接待 - 患者管理为主
        [UserRole.Receptionist] = new()
        {
            WorkbenchView = "ReceptionWorkbenchMainView",
            AccessibleModules = [
                "Patients", "MedicalCase"
            ],
            DisplayName = "前台接待",
            WelcomeTemplate = "欢迎您，{0}！\n\n接待工作台正在启动..."
        },
        
        // 药剂师 - 药房工作台
        [UserRole.Pharmacist] = new()
        {
            WorkbenchView = "PharmacistWorkbenchMainView",
            AccessibleModules = [
                "Herbs", "Prescriptions", "Formula", "Patients"
            ],
            DisplayName = "药剂师",
            WelcomeTemplate = "欢迎您，{0}！\n\n药房工作台正在启动..."
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
            : [];
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
/// 工作台权限配置类
/// UltraThink企业级权限管理：封装角色权限和工作台配置信息
/// </summary>
public sealed class WorkbenchPermission
{
    /// <summary>
    /// 工作台视图名称
    /// </summary>
    public required string WorkbenchView { get; init; } = string.Empty;

    /// <summary>
    /// 可访问的模块列表
    /// C# 12集合表达式：简化列表初始化
    /// </summary>
    public List<string> AccessibleModules { get; init; } = [];

    /// <summary>
    /// 角色显示名称
    /// </summary>
    public required string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 欢迎消息模板（使用{0}占位符表示用户名）
    /// </summary>
    public required string WelcomeTemplate { get; init; } = string.Empty;
}