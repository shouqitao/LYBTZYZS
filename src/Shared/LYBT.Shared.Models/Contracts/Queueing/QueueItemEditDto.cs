using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Queueing {

    /// <summary>
    /// 编辑排队信息 DTO
    /// </summary>
    public class QueueingEditDto {

        /// <summary>排队ID</summary>
        [Required(ErrorMessage = "排队ID不能为空")]
        [DisplayName("排队ID")]
        public Guid Id { get; set; }

        /// <summary>排队类型</summary>
        [Required(ErrorMessage = "排队类型不能为空")]
        [DisplayName("排队类型")]
        public string QueueType { get; set; } = "普通";

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}