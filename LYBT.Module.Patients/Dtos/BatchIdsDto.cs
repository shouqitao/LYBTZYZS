using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Patients.Dtos {
    /// <summary>
    /// 批量操作患者ID列表 DTO
    /// </summary>
    public class BatchIdsDto {
        [Required]
        public List<Guid> Ids { get; set; } = new();
    }
}
