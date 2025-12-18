using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者标签DTO
    /// </summary>
    public class PatientTagDto : BaseDto
    {

        /// <summary>标签名称</summary>
        [Required(ErrorMessage = "标签名称不能为空")]
        [StringLength(50, ErrorMessage = "标签名称长度不能超过50个字符")]
        [DisplayName("标签名称")]
        public string TagName { get; set; } = string.Empty;

        /// <summary>标签颜色</summary>
        [StringLength(7, ErrorMessage = "颜色代码长度不能超过7个字符")]
        [DisplayName("标签颜色")]
        public string? Color { get; set; }

        /// <summary>标签描述</summary>
        [StringLength(200, ErrorMessage = "标签描述长度不能超过200个字符")]
        [DisplayName("标签描述")]
        public string? Description { get; set; }

        /// <summary>使用次数</summary>
        [DisplayName("使用次数")]
        public int UsageCount { get; set; }

        /// <summary>排序号</summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; }

        /// <summary>是否系统标签</summary>
        [DisplayName("系统标签")]
        public bool IsSystem { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
    }
}
