using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 更新医案状态DTO
    /// </summary>
    public class UpdateMedicalCaseStatusDto
    {
        [Required(ErrorMessage = "状态不能为空")]
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "状态变更原因长度不能超过500个字符")]
        [DisplayName("状态变更原因")]
        public string? StatusChangeReason { get; set; }
    }
}
