using LYBT.Common.Enums.Herbs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Herbs.Dtos {

    /// <summary>
    /// 药材批量状态更新 DTO
    /// </summary>
    public class HerbBatchStatusUpdateDto {

        /// <summary>药材ID列表</summary>
        [Required(ErrorMessage = "药材ID列表不能为空")]
        [DisplayName("药材ID列表")]
        [MinLength(1, ErrorMessage = "至少需要选择一个药材")]
        [MaxLength(100, ErrorMessage = "批量操作最多支持100个药材")]
        public List<Guid> Ids { get; set; } = new List<Guid>();

        /// <summary>药材状态</summary>
        [Required(ErrorMessage = "药材状态不能为空")]
        [DisplayName("药材状态")]
        public HerbStatus Status { get; set; }

        /// <summary>状态变更原因</summary>
        [DisplayName("状态变更原因")]
        [StringLength(200, ErrorMessage = "状态变更原因不能超过200字符")]
        public string? Reason { get; set; }
    }
}