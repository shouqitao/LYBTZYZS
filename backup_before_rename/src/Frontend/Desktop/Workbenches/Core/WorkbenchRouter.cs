using System;
using System.Collections.Generic;
using System.Linq;

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
            // 管理员工作台 - 完整的管理功能访问权限
            RegisterWorkbench("管理员", "SystemWorkbenchMainView", new List<string>
            {
                "Dashboard", "Users", "Patients", "MedicalCase", "Consultation", 
                "Herbs", "Formula", "Prescriptions", "Reports", "SystemSettings"
            });

            // 医生工作台
            RegisterWorkbench("用户", "ConsultationWorkbenchMainView", new List<string>
            {
                "Patients", "Consultation", "Prescriptions", "Formula", 
                "MedicalCase", "PatientHistory"
            });

            // 医生角色别名
            RegisterWorkbench("医生", "ConsultationWorkbenchMainView", new List<string>
            {
                "Patients", "Consultation", "Prescriptions", "Formula", 
                "MedicalCase", "PatientHistory"
            });

            // 预留：前台工作台
            RegisterWorkbench("前台", "ReceptionWorkbenchMainView", new List<string>
            {
                "Patients", "Registration", "Appointment", "Queue", "Billing"
            });
        }

        public string GetWorkbenchForRole(string role)
        {
            if (string.IsNullOrEmpty(role))
                return "ConsultationWorkbenchMainView"; // 默认视图

            // 特殊处理sysadmin
            if (role.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                role = "管理员";

            return _workbenchConfigs.ContainsKey(role) 
                ? _workbenchConfigs[role].WorkbenchView 
                : "ConsultationWorkbenchMainView";
        }

        public bool CanAccessModule(string role, string module)
        {
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(module))
                return false;

            if (!_workbenchConfigs.ContainsKey(role))
                return false;

            return _workbenchConfigs[role].AccessibleModules.Contains(module);
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
                new NavigationItem
                {
                    Id = "dashboard",
                    DisplayName = "仪表板",
                    Icon = "Dashboard",
                    ViewName = "DashboardView",
                    Module = "Dashboard",
                    Order = 1
                },
                NavigationItem.CreateSeparator(),
                // 用户和权限管理
                new NavigationItem
                {
                    Id = "users",
                    DisplayName = "用户管理",
                    Icon = "\uE77B", // 用户图标
                    ViewName = "UserManagementView",
                    Module = "Users",
                    Order = 2
                },
                new NavigationItem
                {
                    Id = "patients",
                    DisplayName = "患者管理",
                    Icon = "\uE716", // 联系人图标
                    ViewName = "PatientManagementView",
                    Module = "Patients",
                    Order = 3
                },
                new NavigationItem
                {
                    Id = "patient-reception",
                    DisplayName = "患者接待",
                    Icon = "\uE8D7", // 医院图标
                    ViewName = "PatientReceptionView",
                    Module = "Patients",
                    Order = 4
                },
                NavigationItem.CreateSeparator(),
                // 诊疗管理
                new NavigationItem
                {
                    Id = "medical-cases",
                    DisplayName = "医疗案例",
                    Icon = "\uE8C8", // 文档图标
                    ViewName = "MedicalCaseManagementView",
                    Module = "MedicalCase",
                    Order = 6
                },
                new NavigationItem
                {
                    Id = "consultation",
                    DisplayName = "看诊记录",
                    Icon = "\uE8D4", // 检查图标
                    ViewName = "ConsultationManagementView",
                    Module = "Consultation",
                    Order = 7
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
                    Order = 9
                },
                new NavigationItem
                {
                    Id = "formulas",
                    DisplayName = "验方模板",
                    Icon = "\uE8C7", // 方案图标
                    ViewName = "FormulaManagementView",
                    Module = "Formula",
                    Order = 10
                },
                new NavigationItem
                {
                    Id = "prescriptions",
                    DisplayName = "处方管理",
                    Icon = "\uE8D5", // 处方图标
                    ViewName = "PrescriptionManagementView",
                    Module = "Prescriptions",
                    Order = 11
                },
                NavigationItem.CreateSeparator(),
                // 系统管理
                new NavigationItem
                {
                    Id = "reports",
                    DisplayName = "报表统计",
                    Icon = "\uE9A9", // 图表图标
                    ViewName = "ReportsView",
                    Module = "Reports",
                    Order = 13
                },
                new NavigationItem
                {
                    Id = "settings",
                    DisplayName = "系统设置",
                    Icon = "\uE713", // 设置图标
                    ViewName = "SystemSettingsView",
                    Module = "SystemSettings",
                    Order = 14
                }
            };
        }

        private IEnumerable<NavigationItem> GetDoctorNavigationItems()
        {
            return new List<NavigationItem>
            {
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
                new NavigationItem
                {
                    Id = "patient-quick",
                    DisplayName = "快速建档",
                    Icon = "QuickAdd",
                    ViewName = "QuickPatientCreateView",
                    Module = "Patients",
                    Order = 2
                },
                NavigationItem.CreateSeparator(),
                new NavigationItem
                {
                    Id = "prescriptions",
                    DisplayName = "我的处方",
                    Icon = "MyPrescriptions",
                    ViewName = "MyPrescriptionsView",
                    Module = "Prescriptions",
                    Order = 4
                },
                new NavigationItem
                {
                    Id = "formulas",
                    DisplayName = "常用验方",
                    Icon = "Formulas",
                    ViewName = "FrequentFormulasView",
                    Module = "Formula",
                    Order = 5
                },
                NavigationItem.CreateSeparator(),
                new NavigationItem
                {
                    Id = "medical-cases",
                    DisplayName = "病历查询",
                    Icon = "MedicalCase",
                    ViewName = "MedicalCaseSearchView",
                    Module = "MedicalCase",
                    Order = 7
                },
                new NavigationItem
                {
                    Id = "patient-history",
                    DisplayName = "患者历史",
                    Icon = "History",
                    ViewName = "PatientHistoryView",
                    Module = "PatientHistory",
                    Order = 8
                }
            };
        }

        private IEnumerable<NavigationItem> GetReceptionNavigationItems()
        {
            return new List<NavigationItem>
            {
                new NavigationItem
                {
                    Id = "registration",
                    DisplayName = "挂号登记",
                    Icon = "Registration",
                    ViewName = "RegistrationView",
                    Module = "Registration",
                    Order = 1
                },
                new NavigationItem
                {
                    Id = "patient-create",
                    DisplayName = "患者建档",
                    Icon = "NewPatient",
                    ViewName = "PatientCreateView",
                    Module = "Patients",
                    Order = 2
                },
                new NavigationItem
                {
                    Id = "queue",
                    DisplayName = "排队管理",
                    Icon = "Queue",
                    ViewName = "QueueManagementView",
                    Module = "Queue",
                    Order = 3
                },
                NavigationItem.CreateSeparator(),
                new NavigationItem
                {
                    Id = "appointment",
                    DisplayName = "预约管理",
                    Icon = "Appointment",
                    ViewName = "AppointmentView",
                    Module = "Appointment",
                    Order = 5
                },
                new NavigationItem
                {
                    Id = "billing",
                    DisplayName = "收费管理",
                    Icon = "Billing",
                    ViewName = "BillingView",
                    Module = "Billing",
                    Order = 6
                }
            };
        }

        public IEnumerable<string> GetAccessibleModules(string role)
        {
            if (string.IsNullOrEmpty(role) || !_workbenchConfigs.ContainsKey(role))
                return Enumerable.Empty<string>();

            return _workbenchConfigs[role].AccessibleModules;
        }

        public string GetDefaultView(string workbench)
        {
            switch (workbench)
            {
                case "SystemWorkbenchMainView":
                    return "DashboardView";
                case "ConsultationWorkbenchMainView":
                    return "ConsultationMainView";
                case "ReceptionWorkbenchMainView":
                    return "RegistrationView";
                default:
                    return "HomeView";
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
            return role switch
            {
                "管理员" => $"欢迎您，{userName}！\n\n系统管理工作台正在加载...",
                "用户" or "医生" => $"欢迎您，{userName}医生！\n\n诊疗工作台正在准备...",
                "前台" => $"欢迎您，{userName}！\n\n接待工作台正在启动...",
                _ => $"欢迎您，{userName}！\n\n工作台正在加载..."
            };
        }

        public string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "管理员" => "系统管理员",
                "用户" or "医生" => "医生",
                "前台" => "前台接待",
                _ => "用户"
            };
        }

        /// <summary>
        /// 内部配置类
        /// </summary>
        private class WorkbenchConfig
        {
            public string Role { get; set; }
            public string WorkbenchView { get; set; }
            public List<string> AccessibleModules { get; set; }
        }
    }
}