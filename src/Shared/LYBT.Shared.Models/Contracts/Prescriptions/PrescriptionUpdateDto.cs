namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方更新DTO
    /// </summary>
    public class PrescriptionUpdateDto
    {
        /// <summary>
        /// 诊断
        /// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remarks { get; set; }
    }
}