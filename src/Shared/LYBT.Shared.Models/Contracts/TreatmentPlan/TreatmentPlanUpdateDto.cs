using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.TreatmentPlan
{
    /// <summary>
    /// 更新治疗方案DTO
    /// </summary>
    public class TreatmentPlanUpdateDto
    {
        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }
}