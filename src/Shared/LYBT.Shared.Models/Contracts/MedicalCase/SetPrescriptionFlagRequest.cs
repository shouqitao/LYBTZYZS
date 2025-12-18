using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 标记是否开处方请求
    /// Task 3.4 (#1661): RadioBox变化时自动保存
    /// </summary>
    public class SetPrescriptionFlagRequest
    {
        /// <summary>是否需要开处方</summary>
        [Required(ErrorMessage = "开处方标志不能为空")]
        public bool NeedsPrescription { get; set; }
    }
}
