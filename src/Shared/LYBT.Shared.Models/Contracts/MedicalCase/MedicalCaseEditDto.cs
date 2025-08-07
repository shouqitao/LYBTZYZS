using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医疗案例编辑DTO
    /// </summary>
    public class MedicalCaseEditDto : MedicalCaseUpdateDto
    {
        public Guid Id { get; set; }
    }
}