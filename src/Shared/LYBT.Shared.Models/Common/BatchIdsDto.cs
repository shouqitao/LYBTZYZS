using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Common {

    /// <summary>
    /// 批量操作ID集合DTO（通用版本）
    /// </summary>
    public class BatchIdsDto {

        /// <summary>
        /// ID集合
        /// </summary>
        [Required(ErrorMessage = "ID集合不能为空")]
        [MinLength(1, ErrorMessage = "至少需要选择一项")]
        public List<Guid> Ids { get; set; } = [];

        /// <summary>
        /// 操作原因（可选）
        /// </summary>
        public string? Reason { get; set; }
    }
}
