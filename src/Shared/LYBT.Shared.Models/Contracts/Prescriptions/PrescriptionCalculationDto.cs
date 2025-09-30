namespace LYBT.Shared.Models.Contracts.Prescriptions
{

    /// <summary>
    /// 处方计算结果DTO
    /// </summary>
    public class PrescriptionCalculationDto
    {
        public decimal TotalPrice { get; set; }
        public decimal SingleDosagePrice { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal SingleDosageWeight { get; set; }
    }
}
