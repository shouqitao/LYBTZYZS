namespace LYBT.Desktop.Infrastructure.Interfaces;

/// <summary>
/// 活跃医案服务接口
/// OpenSpec: clarify-cancel-consultation-logic
/// 用于跟踪当前是否有活跃的医案会话，并在退出登录时处理确认逻辑
/// </summary>
public interface IActiveConsultationService
{
    /// <summary>
    /// 是否有活跃的医案
    /// </summary>
    bool HasActiveConsultation { get; }

    /// <summary>
    /// 活跃医案ID
    /// </summary>
    Guid? ActiveMedicalCaseId { get; }

    /// <summary>
    /// 注册活跃医案和离开处理器
    /// 由MedicalCaseWorkspaceViewModel在OnNavigatedTo时调用
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="leaveHandler">离开处理器，返回用户选择结果</param>
    void Register(Guid medicalCaseId, Func<Task<LeaveConsultationResult>> leaveHandler);

    /// <summary>
    /// 注销活跃医案
    /// 由MedicalCaseWorkspaceViewModel在OnNavigatedFrom时调用
    /// </summary>
    void Unregister();

    /// <summary>
    /// 请求离开（退出登录/关闭应用时调用）
    /// 如果有活跃医案，调用注册的处理器显示确认对话框
    /// 如果没有活跃医案，直接返回允许离开
    /// </summary>
    /// <returns>离开结果</returns>
    Task<LeaveConsultationResult> RequestLeaveAsync();
}

/// <summary>
/// 离开医案会话的结果
/// </summary>
public class LeaveConsultationResult
{
    /// <summary>
    /// 是否可以离开
    /// </summary>
    public bool CanLeave { get; set; }

    /// <summary>
    /// 用户的选择
    /// </summary>
    public LeaveConsultationChoice Choice { get; set; }

    /// <summary>
    /// 创建允许离开的结果
    /// </summary>
    public static LeaveConsultationResult AllowLeave(LeaveConsultationChoice choice = LeaveConsultationChoice.None)
        => new() { CanLeave = true, Choice = choice };

    /// <summary>
    /// 创建取消离开的结果
    /// </summary>
    public static LeaveConsultationResult CancelLeave()
        => new() { CanLeave = false, Choice = LeaveConsultationChoice.Stay };
}

/// <summary>
/// 离开看诊界面的选择
/// OpenSpec: clarify-cancel-consultation-logic
/// </summary>
public enum LeaveConsultationChoice
{
    /// <summary>无选择（无活跃医案时）</summary>
    None,
    /// <summary>暂存医案后离开</summary>
    SaveDraft,
    /// <summary>取消医案后离开</summary>
    CancelCase,
    /// <summary>继续停留</summary>
    Stay
}
