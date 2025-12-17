using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 医疗案例状态枚举 - Issue #2242简化版
    /// 简化状态机：Draft ↔ Active → Completed，取消操作使用软删除（IsDeleted）
    /// </summary>
    public enum MedicalCaseStatus
    {
        /// <summary>暂存（用户暂时保存，稍后继续）- Issue #1647</summary>
        [Description("暂存")]
        Draft = 0,

        /// <summary>活跃/进行中（正在诊疗）</summary>
        [Description("进行中")]
        Active = 1,

        /// <summary>已完成（三步流程全部完成）</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>已取消（用户主动取消或管理员关闭）</summary>
        [Description("已取消")]
        Cancelled = 3
    }

    /// <summary>
    /// 审计操作类型枚举
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// </summary>
    public enum AuditOperationType
    {
        /// <summary>创建</summary>
        [Description("创建")]
        Create = 1,

        /// <summary>更新</summary>
        [Description("更新")]
        Update = 2,

        /// <summary>状态变更</summary>
        [Description("状态变更")]
        StatusChange = 3,

        /// <summary>软删除</summary>
        [Description("软删除")]
        SoftDelete = 4,

        /// <summary>取消</summary>
        [Description("取消")]
        Cancel = 5
    }


    /// <summary>
    /// 病例更新模式枚举
    /// OpenSpec: unify-enums-to-shared - 从MedicalCaseDtos.cs迁移
    /// </summary>
    public enum MedicalCaseUpdateMode
    {
        /// <summary>更新所有提供的字段</summary>
        [Description("全部更新")]
        UpdateAll = 0,

        /// <summary>仅更新提供的字段，其他保持不变</summary>
        [Description("部分更新")]
        UpdateOnly = 1,

        /// <summary>仅验证，不执行更新</summary>
        [Description("仅验证")]
        ValidateOnly = 2,

        /// <summary>事务模式：要么全部成功，要么全部回滚</summary>
        [Description("事务模式")]
        Transactional = 3
    }
}
