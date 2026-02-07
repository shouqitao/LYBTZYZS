namespace LYBT.Desktop.Infrastructure.Constants;

/// <summary>
/// 视图名称常量类 - 提供编译时类型安全的视图名称
/// OpenSpec: unify-navigation-architecture (ADR-2, ADR-6)
/// 架构决策: 放在Infrastructure层使所有上层（Shell、Roles、Modules）可引用
/// </summary>
public static class ViewNames
{
    #region 主页视图

    /// <summary>管理员主页</summary>
    public const string AdminHome = "AdminHomeView";

    /// <summary>诊疗主页</summary>
    public const string ClinicalHome = "ClinicalHomeView";

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

    #endregion

    #region 工作台/选择视图

    /// <summary>患者选择</summary>
    public const string PatientSelection = "PatientSelectionView";

    /// <summary>医案工作台</summary>
    public const string MedicalCaseWorkspace = "MedicalCaseWorkspaceView";

    #endregion

    #region MasterDetail视图

    /// <summary>患者主从视图</summary>
    public const string PatientMasterDetail = "PatientMasterDetailView";

    /// <summary>医案主从视图</summary>
    public const string MedicalCaseMasterDetail = "MedicalCaseMasterDetailView";

    #endregion

    #region 列表视图

    /// <summary>医案列表</summary>
    public const string MedicalCaseList = "MedicalCaseListView";

    #endregion

    #region 设置视图

    /// <summary>系统设置</summary>
    public const string SystemSettings = "SystemSettingsView";

    /// <summary>账户设置 (合并了个人资料和修改密码功能)</summary>
    public const string AccountSettings = "AccountSettingsView";

    #endregion

    #region 同步视图

    /// <summary>数据同步</summary>
    public const string Sync = "SyncView";

    #endregion

    #region 认证视图

    /// <summary>登录页面</summary>
    public const string Login = "LoginView";

    #endregion

    #region 开发工具

    /// <summary>控件示例</summary>
    public const string ControlExamples = "ControlExamplesView";

    #endregion
}
