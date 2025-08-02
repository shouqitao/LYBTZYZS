using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Herbs
{
    /// <summary>
    /// 批量更新状态DTO
    /// </summary>
    public class BatchStatusUpdateDto
    {
        /// <summary>药材ID列表</summary>
        [Required(ErrorMessage = "ID列表不能为空")]
        [MinLength(1, ErrorMessage = "至少选择一个药材")]
        public List<Guid> Ids { get; set; } = new();

        /// <summary>状态（0:停用 1:正常）</summary>
        [Required(ErrorMessage = "状态不能为空")]
        [Range(0, 1, ErrorMessage = "状态值必须为0或1")]
        public int Status { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; }

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        public string? Reason { get; set; }
    }
}