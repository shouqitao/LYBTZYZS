namespace LYBT.Shared.Models.Contracts.MedicalCase;

public class MedicalCasePermissionsDto
{
    public bool CanEdit { get; set; }
    public bool CanComplete { get; set; }
    public bool CanSuspend { get; set; }
    public bool CanCancel { get; set; }
    public bool CanDelete { get; set; }

    /// <summary>编辑是否需要 EditReason（P1 US-MC-016: 打印后修改 / 非 Admin 编辑已完成医案）</summary>
    public bool RequiresEditReason { get; set; }

    /// <summary>无权限操作的原因说明（P1 US-MC-016）</summary>
    public string? DenialReason { get; set; }
}
