using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums.Herbs;

namespace LYBT.Module.Herbs.Dtos {
    /// <summary>
    /// 药材状态更新 DTO
    /// </summary>
    public class HerbStatusUpdateDto {
        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

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