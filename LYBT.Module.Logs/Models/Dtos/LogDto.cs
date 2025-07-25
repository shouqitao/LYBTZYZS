using LYBT.Common.Enums.Logs;
using System.ComponentModel;

namespace LYBT.Module.Logs.Models.Dtos {

    /// <summary>
    /// 操作日志数据传输对象（用于日志写入与查询）
    /// </summary>
    public class LogDto {

        /// <summary>
        /// 日志ID（主键，查询返回时用）
        /// </summary>
        [DisplayName("日志ID（主键，查询返回时用）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 日志类型（枚举，对应LogType）
        /// </summary>
        [DisplayName("日志类型（枚举，对应LogType）")]
        public LogType LogType { get; set; }

        /// <summary>
        /// 操作对象类型（枚举，对应ObjectType）
        /// </summary>
        [DisplayName("操作对象类型（枚举，对应ObjectType）")]
        public ObjectType ObjectType { get; set; }

        /// <summary>
        /// 操作对象ID（如用户ID、病历ID等）
        /// </summary>
        [DisplayName("操作对象ID（如用户ID、病历ID等）")]
        public Guid ObjectId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 操作类型（枚举，对应ActionType）
        /// </summary>
        [DisplayName("操作类型（枚举，对应ActionType）")]
        public ActionType ActionType { get; set; }

        /// <summary>
        /// 操作者ID
        /// </summary>
        [DisplayName("操作者ID")]
        public Guid OperatorId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 操作者姓名
        /// </summary>
        [DisplayName("操作者姓名")]
        public string? OperatorName { get; set; }

        /// <summary>
        /// 日志生成时间
        /// </summary>
        [DisplayName("日志生成时间")]
        public DateTime LogTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作内容简要描述
        /// </summary>
        [DisplayName("操作内容简要描述")]
        public string? Content { get; set; }

        /// <summary>
        /// 操作前内容快照（JSON）
        /// </summary>
        [DisplayName("操作前内容快照（JSON）")]
        public string? OldValue { get; set; }

        /// <summary>
        /// 操作后内容快照（JSON）
        /// </summary>
        [DisplayName("操作后内容快照（JSON）")]
        public string? NewValue { get; set; }

        /// <summary>
        /// 操作来源IP地址
        /// </summary>
        [DisplayName("操作来源IP地址")]
        public string? IP { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}