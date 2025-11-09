using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 导出模板配置DTO
    /// FR-002: 控制导出Excel模板的配置参数
    /// </summary>
    public class ExportTemplateDto
    {
        /// <summary>是否包含示例数据（默认true）</summary>
        [DisplayName("包含示例数据")]
        public bool IncludeSampleData { get; set; } = true;

        /// <summary>示例数据行数（默认3行，最少1行，最多10行）</summary>
        [DisplayName("示例行数")]
        [Range(1, 10, ErrorMessage = "示例行数必须在1-10之间")]
        public int SampleRowCount { get; set; } = 3;
    }
}
