namespace LYBT.Desktop.Core.Services.Navigation;

/// <summary>
/// 强类型导航服务接口
/// 提供类型安全的导航方法，替代字典式参数传递
/// Phase 1重构：简化导航架构，提升类型安全性
/// </summary>
public interface ITypedNavigationService : INavigationService
{
    /// <summary>
    /// 使用强类型上下文导航到指定视图
    /// </summary>
    /// <typeparam name="TContext">导航上下文类型</typeparam>
    /// <param name="viewName">视图名称</param>
    /// <param name="context">导航上下文</param>
    void NavigateTo<TContext>(string viewName, TContext context) where TContext : NavigationRequest;
    
    /// <summary>
    /// 使用强类型上下文导航到指定区域的指定视图
    /// </summary>
    /// <typeparam name="TContext">导航上下文类型</typeparam>
    /// <param name="regionName">区域名称</param>
    /// <param name="viewName">视图名称</param>
    /// <param name="context">导航上下文</param>
    void NavigateTo<TContext>(string regionName, string viewName, TContext context) where TContext : NavigationRequest;
    
    /// <summary>
    /// 异步使用强类型上下文导航
    /// </summary>
    /// <typeparam name="TContext">导航上下文类型</typeparam>
    /// <param name="viewName">视图名称</param>
    /// <param name="context">导航上下文</param>
    /// <returns>导航任务</returns>
    Task NavigateToAsync<TContext>(string viewName, TContext context) where TContext : NavigationRequest;
    
    /// <summary>
    /// 异步使用强类型上下文导航到指定区域
    /// </summary>
    /// <typeparam name="TContext">导航上下文类型</typeparam>
    /// <param name="regionName">区域名称</param>
    /// <param name="viewName">视图名称</param>
    /// <param name="context">导航上下文</param>
    /// <returns>导航任务</returns>
    Task NavigateToAsync<TContext>(string regionName, string viewName, TContext context) where TContext : NavigationRequest;
    
    /// <summary>
    /// 导航到患者相关视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="patientId">患者ID</param>
    /// <param name="action">操作类型</param>
    void NavigateToPatient(string viewName, Guid patientId, NavigationAction action = NavigationAction.View);
    
    /// <summary>
    /// 导航到诊疗相关视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="patientId">患者ID</param>
    /// <param name="medicalCaseId">医案ID（可选）</param>
    /// <param name="action">操作类型</param>
    void NavigateToMedical(string viewName, Guid patientId, Guid? medicalCaseId = null, NavigationAction action = NavigationAction.View);
    
    /// <summary>
    /// 导航到管理模块视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID（可选）</param>
    /// <param name="action">操作类型</param>
    void NavigateToManagement(string viewName, string entityType, Guid? entityId = null, NavigationAction action = NavigationAction.View);
}

/// <summary>
/// 视图名称常量
/// 统一管理所有视图名称，避免硬编码字符串
/// </summary>
public static class ViewNames
{
    // 主页和框架
    public const string Home = "HomeView";
    public const string Login = "LoginView";
    public const string Main = "MainWindow";
    
    // 患者模块
    public const string PatientList = "PatientListView";
    public const string PatientDetail = "PatientDetailView";
    public const string PatientManagement = "PatientManagementView";
    
    // 诊疗模块
    public const string MedicalWorkbench = "MedicalWorkbenchMainView";
    public const string MedicalWorkflow = "MedicalWorkflowView";
    public const string MedicalManagement = "MedicalManagementView";
    public const string ConsultationList = "ConsultationListView";
    public const string ConsultationDetail = "ConsultationDetailView";
    public const string DiagnosisEntry = "DiagnosisEntryView";
    public const string PatientSelection = "PatientSelectionView";
    
    // 处方模块
    public const string PrescriptionList = "PrescriptionListView";
    public const string PrescriptionDetail = "PrescriptionDetailView";
    public const string PrescriptionEntry = "PrescriptionEntryView";
    
    // 医案模块
    public const string MedicalCaseList = "MedicalCaseListView";
    public const string MedicalCaseDetail = "MedicalCaseDetailView";
    
    // 草药模块
    public const string HerbList = "HerbListView";
    public const string HerbManagement = "HerbManagementView";
    
    // 方剂模块
    public const string FormulaList = "FormulaListView";
    public const string FormulaManagement = "FormulaManagementView";
    
    // 用户模块
    public const string UserList = "UserListView";
    public const string UserManagement = "UserManagementView";
    
    // 系统管理
    public const string SystemWorkbench = "SystemWorkbenchMainView";
    public const string Settings = "SettingsView";
    public const string About = "AboutView";
}

