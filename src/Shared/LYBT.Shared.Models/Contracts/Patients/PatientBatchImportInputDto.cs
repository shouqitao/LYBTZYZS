using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者批量导入请求DTO
    /// Issue #2004 Task 2.11: Desktop主导批量导入模式
    /// </summary>
    public class PatientBatchImportInputDto
    {
        /// <summary>患者DTO列表（≤10000条）</summary>
        public List<PatientInputDto> Patients { get; set; } = new();

        /// <summary>重复处理策略（Skip/Update/Error）</summary>
        public DuplicateStrategy Strategy { get; set; } = DuplicateStrategy.Skip;
    }
}
