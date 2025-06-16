using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Users.Dtos {
    /// <summary>
    /// 批量操作用户时提交的ID列表 DTO
    /// </summary>
    public class BatchIdsDto {
        [Required]
        public List<Guid> Ids { get; set; } = new();
    }
}
