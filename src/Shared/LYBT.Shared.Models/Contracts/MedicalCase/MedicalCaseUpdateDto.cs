using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 更新医疗案例DTO
    /// </summary>
    public class MedicalCaseUpdateDto
    {
        /// <summary>状态</summary>
        public MedicalCaseStatus? Status { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        /// <summary>完成时间</summary>
        public DateTime? CompleteTime { get; set; }
    }
}