using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 导入结果DTO - 用于数据导入操作的结果
    /// </summary>
    public class ImportResultDto : BatchOperationResultDto
    {
        /// <summary>重复数量</summary>
        [DisplayName("重复数量")]
        public int DuplicateCount { get; set; }

        /// <summary>导入批次ID</summary>
        [DisplayName("批次ID")]
        public string ImportBatchId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>导入文件名</summary>
        [DisplayName("文件名")]
        public string? FileName { get; set; }

        /// <summary>重复记录列表</summary>
        [DisplayName("重复记录")]
        public List<string> DuplicateRecords { get; set; } = new();

        /// <summary>失败记录列表</summary>
        [DisplayName("失败记录")]
        public List<string> FailedRecords { get; set; } = new();

        /// <summary>导入时间</summary>
        [DisplayName("导入时间")]
        public DateTime ImportTime { get; set; } = DateTime.Now;
    }
}
