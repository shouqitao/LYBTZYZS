using System;

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    /// <summary>
    /// 药材状态更新DTO
    /// </summary>
    public class HerbStatusUpdateDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>状态（0:正常 1:缺货 2:停用）</summary>
        public int Status { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; }

        /// <summary>原因</summary>
        public string? Reason { get; set; }
    }
}