using LYBT.Common.Enums.Logs;
using System.ComponentModel;

namespace LYBT.Infrastructure.Logging.Dtos {

    /// <summary>
    /// 日志查询条件传输对象
    /// </summary>
    public class LogQueryDto {

        /// <summary>
        /// 日志类型筛选
        /// </summary>
        [DisplayName("日志类型筛选")]
        public LogType? LogType { get; set; }

        /// <summary>
        /// 操作对象类型筛选
        /// </summary>
        [DisplayName("操作对象类型筛选")]
        public ObjectType? ObjectType { get; set; }

        /// <summary>
        /// 操作类型筛选
        /// </summary>
        [DisplayName("操作类型筛选")]
        public ActionType? ActionType { get; set; }

        /// <summary>
        /// 操作者ID筛选
        /// </summary>
        [DisplayName("操作者ID筛选")]
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 操作者姓名筛选（模糊查询）
        /// </summary>
        [DisplayName("操作者姓名筛选（模糊查询）")]
        public string? OperatorName { get; set; }

        /// <summary>
        /// 开始时间筛选
        /// </summary>
        [DisplayName("开始时间筛选")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间筛选
        /// </summary>
        [DisplayName("结束时间筛选")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 操作内容关键词筛选
        /// </summary>
        [DisplayName("操作内容关键词筛选")]
        public string? ContentKeyword { get; set; }

        /// <summary>
        /// IP地址筛选
        /// </summary>
        [DisplayName("IP地址筛选")]
        public string? IP { get; set; }

        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        [DisplayName("页码（从1开始）")]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        [DisplayName("每页大小")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 排序字段
        /// </summary>
        [DisplayName("排序字段")]
        public string? OrderBy { get; set; } = "LogTime";

        /// <summary>
        /// 排序方向（desc/asc）
        /// </summary>
        [DisplayName("排序方向（desc/asc）")]
        public string? OrderDirection { get; set; } = "desc";
    }
}