namespace LYBT.Desktop.Core.Services.Navigation;

/// <summary>
/// 强类型导航上下文基类
/// 替代字典式的NavigationParameters，提供类型安全的参数传递
/// </summary>
public abstract class NavigationRequest
{
    /// <summary>
    /// 导航操作类型
    /// </summary>
    public NavigationAction Action { get; set; } = NavigationAction.View;
    
    /// <summary>
    /// 是否为工作流模式
    /// </summary>
    public bool IsWorkflowMode { get; set; }
    
    /// <summary>
    /// 来源视图
    /// </summary>
    public string? SourceView { get; set; }
}

/// <summary>
/// 导航操作枚举
/// </summary>
public enum NavigationAction
{
    /// <summary>
    /// 查看
    /// </summary>
    View,
    
    /// <summary>
    /// 新增
    /// </summary>
    Add,
    
    /// <summary>
    /// 编辑
    /// </summary>
    Edit,
    
    /// <summary>
    /// 删除
    /// </summary>
    Delete,
    
    /// <summary>
    /// 选择
    /// </summary>
    Select
}

/// <summary>
/// 患者导航上下文
/// </summary>
public class PatientNavigationRequest : NavigationRequest
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId { get; set; }
    
    /// <summary>
    /// 患者名称（用于显示）
    /// </summary>
    public string? PatientName { get; set; }
}

/// <summary>
/// 诊疗导航上下文
/// </summary>
public class MedicalNavigationRequest : NavigationRequest
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId { get; set; }
    
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid? MedicalCaseId { get; set; }
    
    /// <summary>
    /// 诊疗ID
    /// </summary>
    public Guid? ConsultationId { get; set; }
    
    /// <summary>
    /// 处方ID
    /// </summary>
    public Guid? PrescriptionId { get; set; }
}

/// <summary>
/// 工作流导航上下文
/// </summary>
public class WorkflowNavigationRequest : NavigationRequest
{
    /// <summary>
    /// 当前步骤
    /// </summary>
    public string CurrentStep { get; set; } = "";
    
    /// <summary>
    /// 目标步骤
    /// </summary>
    public string? TargetStep { get; set; }
    
    /// <summary>
    /// 工作流数据
    /// </summary>
    public object? WorkflowData { get; set; }
}

/// <summary>
/// 管理模块导航上下文
/// </summary>
public class ManagementNavigationRequest : NavigationRequest
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = "";
    
    /// <summary>
    /// 筛选条件
    /// </summary>
    public object? Filter { get; set; }
}