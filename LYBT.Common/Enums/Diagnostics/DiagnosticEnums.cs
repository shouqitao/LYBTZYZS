namespace LYBT.Common.Enums.Diagnostics {

    /// <summary>
    /// 配方类型
    /// </summary>
    public enum FormulaType {
        /// <summary>
        /// 汤剂
        /// </summary>
        Decoction = 1,

        /// <summary>
        /// 丸剂
        /// </summary>
        Pill = 2,

        /// <summary>
        /// 散剂
        /// </summary>
        Powder = 3,

        /// <summary>
        /// 膏剂
        /// </summary>
        Paste = 4,

        /// <summary>
        /// 外用
        /// </summary>
        External = 5
    }

    /// <summary>
    /// 处方状态
    /// </summary>
    public enum PrescriptionStatus {
        /// <summary>
        /// 草稿
        /// </summary>
        Draft = 0,

        /// <summary>
        /// 已开具
        /// </summary>
        Issued = 1,

        /// <summary>
        /// 已配药
        /// </summary>
        Dispensed = 2,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1
    }

    /// <summary>
    /// 病历状态
    /// </summary>
    public enum RecordStatus {
        /// <summary>
        /// 草稿
        /// </summary>
        Draft = 0,

        /// <summary>
        /// 已保存
        /// </summary>
        Saved = 1,

        /// <summary>
        /// 已提交
        /// </summary>
        Submitted = 2,

        /// <summary>
        /// 已审核
        /// </summary>
        Reviewed = 3,

        /// <summary>
        /// 已归档
        /// </summary>
        Archived = 4
    }

    /// <summary>
    /// 挂号状态
    /// </summary>
    public enum RegistrationStatus {
        /// <summary>
        /// 已挂号
        /// </summary>
        Registered = 1,

        /// <summary>
        /// 已就诊
        /// </summary>
        Visited = 2,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1,

        /// <summary>
        /// 过期
        /// </summary>
        Expired = -2
    }

    /// <summary>
    /// 挂号类型
    /// </summary>
    public enum RegistrationType {
        /// <summary>
        /// 普通号
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 专家号
        /// </summary>
        Expert = 2,

        /// <summary>
        /// 急诊号
        /// </summary>
        Emergency = 3,

        /// <summary>
        /// 复诊号
        /// </summary>
        FollowUp = 4
    }

    /// <summary>
    /// 排队状态
    /// </summary>
    public enum QueueStatus {
        /// <summary>
        /// 排队中
        /// </summary>
        Waiting = 1,

        /// <summary>
        /// 就诊中
        /// </summary>
        InProgress = 2,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped = 4,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1
    }

    /// <summary>
    /// 治疗室状态
    /// </summary>
    public enum TreatmentRoomStatus {
        /// <summary>
        /// 可用
        /// </summary>
        Available = 1,

        /// <summary>
        /// 使用中
        /// </summary>
        InUse = 2,

        /// <summary>
        /// 维护中
        /// </summary>
        Maintenance = 3,

        /// <summary>
        /// 停用
        /// </summary>
        Disabled = 0
    }

    /// <summary>
    /// 治疗任务状态
    /// </summary>
    public enum TreatmentTaskStatus {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 进行中
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused = 3
    }
}