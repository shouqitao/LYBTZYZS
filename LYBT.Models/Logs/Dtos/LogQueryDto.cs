using LYBT.Common.Enums.Logs;
using System.ComponentModel;

namespace LYBT.Module.Logs.Dtos {

    /// <summary>
    /// 操作日志分页/条件查询DTO
    /// </summary>
    public class LogQueryDto {

        /// <summary>
        /// 对象类型（可选条件）
        /// </summary>
        [DisplayName("对象类型（可选条件）")]
/// <summary>
/// ObjectType 属性。
/// </summary>
        public ObjectType? ObjectType { get; set; }

        /// <summary>
        /// 对象ID（可选条件）
        /// </summary>
        [DisplayName("对象ID（可选条件）")]
/// <summary>
/// ObjectId 属性。
/// </summary>
        public Guid? ObjectId { get; set; }

        /// <summary>
        /// 操作类型（可选条件）
        /// </summary>
        [DisplayName("操作类型（可选条件）")]
/// <summary>
/// ActionType 属性。
/// </summary>
        public ActionType? ActionType { get; set; }

        /// <summary>
        /// 操作者ID（可选条件）
        /// </summary>
        [DisplayName("操作者ID（可选条件）")]
/// <summary>
/// OperatorId 属性。
/// </summary>
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 日志类型（可选条件）
        /// </summary>
        [DisplayName("日志类型（可选条件）")]
/// <summary>
/// LogType 属性。
/// </summary>
        public LogType? LogType { get; set; }

        /// <summary>
        /// 起始时间（可选条件）
        /// </summary>
        [DisplayName("起始时间（可选条件）")]
/// <summary>
/// StartTime 属性。
/// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 截止时间（可选条件）
        /// </summary>
        [DisplayName("截止时间（可选条件）")]
/// <summary>
/// EndTime 属性。
/// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 页码（默认1）
        /// </summary>
        [DisplayName("页码（默认1）")]
/// <summary>
/// Page 属性。
/// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数（默认20）
        /// </summary>
        [DisplayName("每页条数（默认20）")]
/// <summary>
/// PageSize 属性。
/// </summary>
        public int PageSize { get; set; } = 20;
    }
}
