using LYBT.Common.Enums.Logs;
using System.ComponentModel;

namespace LYBT.Infrastructure.Logging {

    /// <summary>
    /// 统一日志实体模型（整合原 Module.Logs）
    /// </summary>
    public class LogModel {

        /// <summary>
        /// 日志ID（主键，唯一标识一条日志）
        /// </summary>
        [DisplayName("日志ID（主键，唯一标识一条日志）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 日志类型（如操作日志、系统日志、登录日志等）
        /// </summary>
        [DisplayName("日志类型（如操作日志、系统日志、登录日志等）")]
        public LogType LogType { get; set; }

        /// <summary>
        /// 操作对象类型（如用户、患者、病历、药方等）
        /// </summary>
        [DisplayName("操作对象类型（如用户、患者、病历、药方等）")]
        public ObjectType ObjectType { get; set; }

        /// <summary>
        /// 操作对象ID（如用户ID、病历ID等，便于跨模块检索）
        /// </summary>
        [DisplayName("操作对象ID（如用户ID、病历ID等，便于跨模块检索）")]
        public Guid ObjectId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 操作类型（如新增、编辑、禁用、登录等，建议使用枚举）
        /// </summary>
        [DisplayName("操作类型（如新增、编辑、禁用、登录等，建议使用枚举）")]
        public ActionType ActionType { get; set; }

        /// <summary>
        /// 操作者ID（当前执行操作的用户ID）
        /// </summary>
        [DisplayName("操作者ID（当前执行操作的用户ID）")]
        public Guid OperatorId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 操作者姓名（便于日志列表直接展示）
        /// </summary>
        [DisplayName("操作者姓名（便于日志列表直接展示）")]
        public string? OperatorName { get; set; }

        /// <summary>
        /// 日志生成时间（操作发生的时间）
        /// </summary>
        [DisplayName("日志生成时间（操作发生的时间）")]
        public DateTime LogTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作内容简要描述（如编辑用户资料、禁用病历等）
        /// </summary>
        [DisplayName("操作内容简要描述")]
        public string? Content { get; set; }

        /// <summary>
        /// 操作前内容快照（JSON序列化格式，便于比对变更前后数据）
        /// </summary>
        [DisplayName("操作前内容快照（JSON序列化格式，便于比对变更前后数据）")]
        public string? OldValue { get; set; }

        /// <summary>
        /// 操作后内容快照（JSON序列化格式，便于比对变更前后数据）
        /// </summary>
        [DisplayName("操作后内容快照（JSON序列化格式，便于比对变更前后数据）")]
        public string? NewValue { get; set; }

        /// <summary>
        /// 操作来源IP地址（用于安全审计和溯源）
        /// </summary>
        [DisplayName("操作来源IP地址（用于安全审计和溯源）")]
        public string? IP { get; set; }

        /// <summary>
        /// 备注（可选，补充说明内容）
        /// </summary>
        [DisplayName("备注（可选，补充说明内容）")]
        public string? Remark { get; set; }
    }
}