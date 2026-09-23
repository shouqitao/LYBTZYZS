namespace LYBT.Desktop.Infrastructure.Constants;

/// <summary>
/// 视图名称常量类 - 提供编译时类型安全的视图名称
/// 架构决策: 放在Infrastructure层使所有上层（Shell、Roles、Modules）可引用
/// </summary>
public static class ViewNames
{
    #region 主页视图

    /// <summary>管理员主页</summary>
    public const string AdminHome = "AdminHomeView";

    /// <summary>诊疗主页</summary>
    public const string ClinicalHome = "ClinicalHomeView";

    /// <summary>前台工作台主页</summary>
    public const string ReceptionistHome = "ReceptionistHomeView";

    /// <summary>系统运维设置主页</summary>
    public const string SysadminHome = "SysadminHomeView";

    #endregion

    #region 管理视图

    /// <summary>患者管理</summary>
    public const string PatientManagement = "PatientManagementView";

    /// <summary>医案管理</summary>
    public const string MedicalCaseManagement = "MedicalCaseManagementView";

    /// <summary>药材管理</summary>
    public const string HerbManagement = "HerbManagementView";

    /// <summary>验方管理</summary>
    public const string FormulaManagement = "FormulaManagementView";

    /// <summary>用户管理</summary>
    public const string UserManagement = "UserManagementView";

    /// <summary>统计报表</summary>
    public const string ReportsHome = "ReportsHomeView";

    #endregion

    #region 工作台/选择视图

    /// <summary>患者选择</summary>
    public const string PatientSelection = "PatientSelectionView";

    /// <summary>医案工作台</summary>
    public const string MedicalCaseWorkspace = "MedicalCaseWorkspaceView";

    /// <summary>临床工作台（患者列表+看诊工作区一体化）</summary>
    public const string ClinicalWorkspace = "ClinicalWorkspaceView";

    #endregion

    #region MasterDetail视图

    /// <summary>医案主从视图</summary>
    public const string MedicalCaseMasterDetail = "MedicalCaseMasterDetailView";

    /// <summary>医案审计日志</summary>
    public const string AuditLog = "AuditLogView";

    #endregion

    #region 设置视图

    /// <summary>诊所设置</summary>
    public const string SystemSettings = "SystemSettingsView";

    /// <summary>个人资料 (合并了个人资料和修改密码功能)</summary>
    public const string AccountSettings = "AccountSettingsView";

    #endregion

    #region 挂号视图

    /// <summary>挂号队列</summary>
    public const string RegistrationList = "RegistrationListView";

    #endregion

    #region 认证视图

    /// <summary>登录视图</summary>
    public const string Login = "LoginView";

    /// <summary>初始化向导（B-07 / US-SHELL-011：首次运行 + 系统管理手动重跑；同一视图可对话框/导航两种方式打开）</summary>
    public const string InitializationWizard = "InitializationWizardView";

    #endregion

    #region 诊断视图

    /// <summary>日志级别控制</summary>
    public const string LogLevelControl = "LogLevelControlView";

    /// <summary>部署管理</summary>
    public const string Deployment = "DeploymentView";

    /// <summary>备份恢复管理（T7-2: US-SHELL-013）</summary>
    public const string BackupManagement = "BackupManagementView";

    /// <summary>安全审计日志查看（US-SHELL-014；非医案 AuditLog）</summary>
    public const string SecurityAuditLog = "SecurityAuditLogView";

    /// <summary>配置导入导出（US-SHELL-016；仅系统管理员）</summary>
    public const string ConfigExportImport = "ConfigExportImportView";

    #endregion

}
