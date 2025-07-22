using LYBT.Common.Enums.Logs;
using System.ComponentModel;

namespace LYBT.Module.Logs.Dtos {

    /// <summary>
    /// 操作日志数据传输对象（用于日志写入与查询）
    /// </summary>
    public class LogDto {

        /// <summary>
        /// 日志ID（主键，查询返回时用）
        /// </summary>
        [DisplayName("日志ID（主键，查询返回时用）")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 日志类型（枚举，对应LogType）
        /// </summary>
        [DisplayName("日志类型（枚举，对应LogType）")]
/// <summary>
/// LogType 属性。
/// </summary>
        public LogType LogType { get; set; }

        /// <summary>
        /// 操作对象类型（枚举，对应ObjectType）
        /// </summary>
        [DisplayName("操作对象类型（枚举，对应ObjectType）")]
/// <summary>
/// ObjectType 属性。
/// </summary>
        public ObjectType ObjectType { get; set; }

        /// <summary>
        /// 操作对象ID（如用户ID、病历ID等）
        /// </summary>
        [DisplayName("操作对象ID（如用户ID、病历ID等）")]
/// <summary>
/// ObjectId 属性。
/// </summary>
        public Guid ObjectId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 操作类型（枚举，对应ActionType）
        /// </summary>
        [DisplayName("操作类型（枚举，对应ActionType）")]
/// <summary>
/// ActionType 属性。
/// </summary>
        public ActionType ActionType { get; set; }

        /// <summary>
        /// 操作者ID
        /// </summary>
        [DisplayName("操作者ID")]
/// <summary>
/// OperatorId 属性。
/// </summary>
        public Guid OperatorId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 操作者姓名
        /// </summary>
        [DisplayName("操作者姓名")]
/// <summary>
/// OperatorName 属性。
/// </summary>
        public string? OperatorName { get; set; }

        /// <summary>
        /// 日志生成时间
        /// </summary>
        [DisplayName("日志生成时间")]
/// <summary>
/// LogTime 属性。
/// </summary>
        public DateTime LogTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作内容简要描述
        /// </summary>
        [DisplayName("操作内容简要描述")]
/// <summary>
/// Content 属性。
/// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// 操作前内容快照（JSON）
        /// </summary>
        [DisplayName("操作前内容快照（JSON）")]
/// <summary>
/// OldValue 属性。
/// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// 操作后内容快照（JSON）
        /// </summary>
        [DisplayName("操作后内容快照（JSON）")]
/// <summary>
/// NewValue 属性。
/// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// 操作来源IP地址
        /// </summary>
        [DisplayName("操作来源IP地址")]
/// <summary>
/// IP 属性。
/// </summary>
        public string? IP { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
