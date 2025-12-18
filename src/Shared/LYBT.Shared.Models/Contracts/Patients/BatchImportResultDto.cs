using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者批量导入结果DTO - 继承自通用导入结果基类
    /// FR-001: 批量导入患者数据的返回结果
    /// OpenSpec: optimize-batch-operations - DTO继承规范化
    /// </summary>
    public class PatientBatchImportResultDto : ImportResultDto
    {
        /// <summary>失败详情列表（患者特定类型）</summary>
        [DisplayName("失败详情")]
        public List<PatientImportFailureDto> Failures { get; set; } = new();
    }
}
