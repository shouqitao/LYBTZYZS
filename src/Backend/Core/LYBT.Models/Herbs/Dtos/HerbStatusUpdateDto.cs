using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using HerbStatus = LYBT.Shared.Models.Enums.HerbStatus;

namespace LYBT.Models.Herbs {

    /// <summary>
    /// 中药状态更新DTO
    /// </summary>
    public class HerbStatusUpdateDto {

        /// <summary>
        /// 中药ID
        /// </summary>
        [Required(ErrorMessage = "中药ID不能为空")]
        [DisplayName("中药ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [DisplayName("状态")]
        public HerbStatus Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 更新原因
        /// </summary>
        [StringLength(500, ErrorMessage = "更新原因长度不能超过500个字符")]
        [DisplayName("更新原因")]
        public string? Reason { get; set; }

        /// <summary>
        /// 更新备注
        /// </summary>
        [StringLength(500, ErrorMessage = "更新备注长度不能超过500个字符")]
        [DisplayName("更新备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 中药批量状态更新DTO
    /// </summary>
    public class HerbBatchStatusUpdateDto {

        /// <summary>
        /// 中药ID列表
        /// </summary>
        [Required(ErrorMessage = "中药ID列表不能为空")]
        [DisplayName("中药ID列表")]
        public List<Guid> Ids { get; set; } = new();

        /// <summary>
        /// 状态
        /// </summary>
        [DisplayName("状态")]
        public HerbStatus Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 更新原因
        /// </summary>
        [StringLength(500, ErrorMessage = "更新原因长度不能超过500个字符")]
        [DisplayName("更新原因")]
        public string? Reason { get; set; }

        /// <summary>
        /// 更新备注
        /// </summary>
        [StringLength(500, ErrorMessage = "更新备注长度不能超过500个字符")]
        [DisplayName("更新备注")]
        public string? Remark { get; set; }
    }
}