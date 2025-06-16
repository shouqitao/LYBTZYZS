using LYBT.Common.Enums.Logs;
using System;

namespace LYBT.Models.Logs {
    /// <summary>
    /// 操作日志表实体类（结构化，适配多模块日志记录需求）
    /// </summary>
    public class LogModel {
    /// <summary>
    /// 日志ID（主键，唯一标识一条日志）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 日志类型（如操作日志、系统日志、登录日志等）
    /// </summary>
    public LogType LogType { get; set; }

    /// <summary>
    /// 操作对象类型（如用户、患者、病历、药方等）
    /// </summary>
    public ObjectType ObjectType { get; set; }

    /// <summary>
    /// 操作对象ID（如用户ID、病历ID等，便于跨模块检索）
    /// </summary>
    public Guid ObjectId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 操作类型（如新增、编辑、禁用、登录等，建议使用枚举）
    /// </summary>
    public ActionType ActionType { get; set; }

    /// <summary>
    /// 操作者ID（当前执行操作的用户ID）
    /// </summary>
    public Guid OperatorId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 操作者姓名（便于日志列表直接展示）
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 日志生成时间（操作发生的时间）
    /// </summary>
    public DateTime LogTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 操作内容简要描述（如“编辑用户资料”、“禁用病历”等）
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 操作前内容快照（JSON序列化格式，便于比对变更前后数据）
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// 操作后内容快照（JSON序列化格式，便于比对变更前后数据）
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// 操作来源IP地址（用于安全审计和溯源）
    /// </summary>
    public string? IP { get; set; }

    /// <summary>
    /// 备注（可选，补充说明内容）
    /// </summary>
    public string? Remark { get; set; }
}
}

