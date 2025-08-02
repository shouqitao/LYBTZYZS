using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs {

    /// <summary>
    /// 中药材状态更新DTO - 前后端共享API契约
    /// 用于更新中药材状态的请求模型
    /// </summary>
    public class HerbStatusUpdateDto {

        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>药材状态</summary>
        [DisplayName("药材状态")]
        public HerbStatus Status { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; }

        /// <summary>更新原因</summary>
        [StringLength(500, ErrorMessage = "更新原因长度不能超过500个字符")]
        [DisplayName("更新原因")]
        public string? Reason { get; set; }

        /// <summary>更新备注</summary>
        [StringLength(500, ErrorMessage = "更新备注长度不能超过500个字符")]
        [DisplayName("更新备注")]
        public string? Remark { get; set; }
    }
}