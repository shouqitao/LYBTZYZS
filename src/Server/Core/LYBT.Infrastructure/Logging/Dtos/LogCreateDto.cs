using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Logging.Dtos
{

    /// <summary>
    /// 日志创建传输对象
    /// </summary>
    public class LogCreateDto
    {

        /// <summary>
        /// 日志类型（必填）
        /// </summary>
        [Required(ErrorMessage = "日志类型不能为空")]
        [DisplayName("日志类型（必填）")]
        public LogType LogType { get; set; }

        /// <summary>
        /// 操作对象类型（必填）
        /// </summary>
        [Required(ErrorMessage = "操作对象类型不能为空")]
        [DisplayName("操作对象类型（必填）")]
        public ObjectType ObjectType { get; set; }

        /// <summary>
        /// 操作对象ID（必填）
        /// </summary>
        [Required(ErrorMessage = "操作对象ID不能为空")]
        [DisplayName("操作对象ID（必填）")]
        public Guid ObjectId { get; set; }

        /// <summary>
        /// 操作类型（必填）
        /// </summary>
        [Required(ErrorMessage = "操作类型不能为空")]
        [DisplayName("操作类型（必填）")]
        public ActionType ActionType { get; set; }

        /// <summary>
        /// 操作者ID（必填）
        /// </summary>
        [Required(ErrorMessage = "操作者ID不能为空")]
        [DisplayName("操作者ID（必填）")]
        public Guid OperatorId { get; set; }

        /// <summary>
        /// 操作者姓名（必填）
        /// </summary>
        [Required(ErrorMessage = "操作者姓名不能为空")]
        [StringLength(50, ErrorMessage = "操作者姓名长度不能超过50个字符")]
        [DisplayName("操作者姓名（必填）")]
        public string? OperatorName { get; set; }

        /// <summary>
        /// 操作内容简要描述（必填）
        /// </summary>
        [Required(ErrorMessage = "操作内容描述不能为空")]
        [StringLength(500, ErrorMessage = "操作内容描述长度不能超过500个字符")]
        [DisplayName("操作内容简要描述（必填）")]
        public string? Content { get; set; }

        /// <summary>
        /// 操作前内容快照（JSON格式）
        /// </summary>
        [DisplayName("操作前内容快照（JSON格式）")]
        public string? OldValue { get; set; }

        /// <summary>
        /// 操作后内容快照（JSON格式）
        /// </summary>
        [DisplayName("操作后内容快照（JSON格式）")]
        public string? NewValue { get; set; }

        /// <summary>
        /// 操作来源IP地址
        /// </summary>
        [StringLength(45, ErrorMessage = "IP地址长度不能超过45个字符")]
        [DisplayName("操作来源IP地址")]
        public string? IP { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}