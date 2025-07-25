using LYBT.Common.Enums.Logs;
using System.ComponentModel;

namespace LYBT.Module.Logs.Models.Dtos {

    /// <summary>
    /// 操作日志分页/条件查询DTO
    /// </summary>
    public class LogQueryDto {

        /// <summary>
        /// 对象类型（可选条件）
        /// </summary>
        [DisplayName("对象类型（可选条件）")]
        public ObjectType? ObjectType { get; set; }

        /// <summary>
        /// 对象ID（可选条件）
        /// </summary>
        [DisplayName("对象ID（可选条件）")]
        public Guid? ObjectId { get; set; }

        /// <summary>
        /// 操作类型（可选条件）
        /// </summary>
        [DisplayName("操作类型（可选条件）")]
        public ActionType? ActionType { get; set; }

        /// <summary>
        /// 操作者ID（可选条件）
        /// </summary>
        [DisplayName("操作者ID（可选条件）")]
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 日志类型（可选条件）
        /// </summary>
        [DisplayName("日志类型（可选条件）")]
        public LogType? LogType { get; set; }

        /// <summary>
        /// 起始时间（可选条件）
        /// </summary>
        [DisplayName("起始时间（可选条件）")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 截止时间（可选条件）
        /// </summary>
        [DisplayName("截止时间（可选条件）")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 页码（默认1）
        /// </summary>
        [DisplayName("页码（默认1）")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数（默认20）
        /// </summary>
        [DisplayName("每页条数（默认20）")]
        public int PageSize { get; set; } = 20;
    }
}