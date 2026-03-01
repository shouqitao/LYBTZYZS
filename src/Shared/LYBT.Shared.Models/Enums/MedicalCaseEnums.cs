using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 医疗案例状态枚举 - 简化版
    /// 状态机：Suspended ↔ Active → Completed，取消操作统一使用软删除（IsDeleted）
    /// MC-D20: Draft 已重命名为 Suspended (挂起)
    /// </summary>
    public enum MedicalCaseStatus
    {
        /// <summary>已挂起（医生暂时离开，稍后继续）- MC-D20</summary>
        [Description("已挂起")]
        Suspended = 0,

        /// <summary>活跃/进行中（正在诊疗）</summary>
        [Description("进行中")]
        Active = 1,

        /// <summary>已完成（三步流程全部完成）</summary>
        [Description("已完成")]
        Completed = 2,
        // Cancelled = 3 已移除，取消操作统一使用软删除（IsDeleted=true）

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
    /// 医疗案例查询类型枚举
    /// OpenSpec: optimize-medicalcase-api - 统一查询端点
    /// </summary>
    public enum MedicalCaseQueryType
    {
        /// <summary>默认：分页列表</summary>
        [Description("全部")]
        All = 0,

        /// <summary>按患者ID查询</summary>
        [Description("按患者")]
        ByPatient = 1,

        /// <summary>待看诊（当前用户的Pending案例）</summary>
        [Description("待看诊")]
        Pending = 2,

        /// <summary>未完成（指定患者的Active案例）</summary>
        [Description("未完成")]
        Unfinished = 3,

        /// <summary>最近（处方参考用）</summary>
        [Description("最近")]
        Recent = 4
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
