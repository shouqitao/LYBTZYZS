using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Configuration {

    /// <summary>
    /// 诊断目录DTO
    /// </summary>
    public class DiagnosisCatalogDto : BaseDto {

        /// <summary>诊断代码</summary>
        [Required(ErrorMessage = "诊断代码不能为空")]
        [StringLength(50, ErrorMessage = "诊断代码长度不能超过50个字符")]
        [DisplayName("诊断代码")]
        public string Code { get; set; } = string.Empty;

        /// <summary>诊断名称</summary>
        [Required(ErrorMessage = "诊断名称不能为空")]
        [StringLength(200, ErrorMessage = "诊断名称长度不能超过200个字符")]
        [DisplayName("诊断名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>诊断分类</summary>
        [StringLength(100, ErrorMessage = "诊断分类长度不能超过100个字符")]
        [DisplayName("诊断分类")]
        public string? Category { get; set; }

        /// <summary>ICD代码</summary>
        [StringLength(20, ErrorMessage = "ICD代码长度不能超过20个字符")]
        [DisplayName("ICD代码")]
        public string? IcdCode { get; set; }

        /// <summary>拼音码</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>描述</summary>
        [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
        [DisplayName("描述")]
        public string? Description { get; set; }

        /// <summary>排序号</summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 治疗目录DTO
    /// </summary>
    public class TreatmentCatalogDto : BaseDto {

        /// <summary>治疗代码</summary>
        [Required(ErrorMessage = "治疗代码不能为空")]
        [StringLength(50, ErrorMessage = "治疗代码长度不能超过50个字符")]
        [DisplayName("治疗代码")]
        public string Code { get; set; } = string.Empty;

        /// <summary>治疗名称</summary>
        [Required(ErrorMessage = "治疗名称不能为空")]
        [StringLength(200, ErrorMessage = "治疗名称长度不能超过200个字符")]
        [DisplayName("治疗名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>治疗分类</summary>
        [StringLength(100, ErrorMessage = "治疗分类长度不能超过100个字符")]
        [DisplayName("治疗分类")]
        public string? Category { get; set; }

        /// <summary>治疗方法</summary>
        [StringLength(500, ErrorMessage = "治疗方法长度不能超过500个字符")]
        [DisplayName("治疗方法")]
        public string? Method { get; set; }

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>单位</summary>
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        [DisplayName("单位")]
        public string Unit { get; set; } = "次";

        /// <summary>拼音码</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>描述</summary>
        [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
        [DisplayName("描述")]
        public string? Description { get; set; }

        /// <summary>排序号</summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 日志DTO
    /// </summary>
    public class LogDto : BaseDto {

        /// <summary>日志级别</summary>
        [DisplayName("日志级别")]
        public string Level { get; set; } = string.Empty;

        /// <summary>日志消息</summary>
        [DisplayName("日志消息")]
        public string Message { get; set; } = string.Empty;

        /// <summary>日志来源</summary>
        [DisplayName("日志来源")]
        public string? Source { get; set; }

        /// <summary>异常信息</summary>
        [DisplayName("异常信息")]
        public string? Exception { get; set; }

        /// <summary>用户ID</summary>
        [DisplayName("用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        /// <summary>操作类型</summary>
        [DisplayName("操作类型")]
        public string? ActionType { get; set; }

        /// <summary>IP地址</summary>
        [DisplayName("IP地址")]
        public string? IpAddress { get; set; }

        /// <summary>请求路径</summary>
        [DisplayName("请求路径")]
        public string? RequestPath { get; set; }

        /// <summary>请求方法</summary>
        [DisplayName("请求方法")]
        public string? RequestMethod { get; set; }

        /// <summary>响应状态码</summary>
        [DisplayName("响应状态码")]
        public int? StatusCode { get; set; }

        /// <summary>执行时长(毫秒)</summary>
        [DisplayName("执行时长")]
        public long? Duration { get; set; }

        /// <summary>附加数据</summary>
        [DisplayName("附加数据")]
        public string? AdditionalData { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
