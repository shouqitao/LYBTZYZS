using System;

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    /// <summary>
    /// 更新药材DTO
    /// </summary>
    public class HerbUpdateDto : HerbCreateDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>状态（0:正常 1:缺货 2:停用）</summary>
        public int Status { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; }
    }
}