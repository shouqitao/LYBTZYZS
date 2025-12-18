using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 导入结果DTO泛型版 - 用于数据导入操作的结果（支持返回导入的数据）
    /// Issue #1165: 患者批量导入功能
    /// </summary>
    /// <typeparam name="T">导入数据的类型</typeparam>
    public class ImportResultDto<T> : ImportResultDto
    {
        /// <summary>导入的数据列表</summary>
        [DisplayName("导入数据")]
        public List<T> ImportedData { get; set; } = new();
    }
}
