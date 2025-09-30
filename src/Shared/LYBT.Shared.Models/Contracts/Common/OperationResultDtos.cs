using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 操作结果基础DTO - 用于所有操作结果的基类
    /// </summary>
    public class OperationResultDto
    {
        /// <summary>操作是否成功</summary>
        [DisplayName("操作成功")]
        public bool IsSuccess { get; set; } = true;

        /// <summary>操作消息</summary>
        [DisplayName("操作消息")]
        public string Message { get; set; } = string.Empty;

        /// <summary>错误代码</summary>
        [DisplayName("错误代码")]
        public string? ErrorCode { get; set; }

        /// <summary>操作时间</summary>
        [DisplayName("操作时间")]
        public DateTime OperationTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 操作结果DTO泛型版 - 支持返回具体数据
    /// </summary>
    public class OperationResultDto<T> : OperationResultDto
    {
        /// <summary>返回的数据</summary>
        [DisplayName("返回数据")]
        public T? Data { get; set; }
    }

    /// <summary>
    /// 批量操作结果DTO - 用于批量操作的结果
    /// </summary>
    public class BatchOperationResultDto : OperationResultDto
    {
        /// <summary>总数量</summary>
        [DisplayName("总数量")]
        public int TotalCount { get; set; }

        /// <summary>成功数量</summary>
        [DisplayName("成功数量")]
        public int SuccessCount { get; set; }

        /// <summary>失败数量</summary>
        [DisplayName("失败数量")]
        public int FailureCount { get; set; }

        /// <summary>跳过数量</summary>
        [DisplayName("跳过数量")]
        public int SkippedCount { get; set; }

        /// <summary>成功的ID列表</summary>
        [DisplayName("成功的ID列表")]
        public List<Guid> SuccessfulIds { get; set; } = new();

        /// <summary>失败的ID列表</summary>
        [DisplayName("失败的ID列表")]
        public List<Guid> FailedIds { get; set; } = new();

        /// <summary>错误详情列表</summary>
        [DisplayName("错误详情")]
        public List<ErrorDetail> Errors { get; set; } = new();

        /// <summary>操作成功率</summary>
        [DisplayName("成功率")]
        public decimal SuccessRate => TotalCount > 0 ? (decimal)SuccessCount / TotalCount * 100 : 0;

        /// <summary>错误详情</summary>
        public class ErrorDetail
        {
            /// <summary>记录标识</summary>
            public string RecordIdentifier { get; set; } = string.Empty;

            /// <summary>错误消息</summary>
            public string ErrorMessage { get; set; } = string.Empty;

            /// <summary>错误代码</summary>
            public string? ErrorCode { get; set; }
        }
    }

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

    /// <summary>
    /// 导出结果DTO - 用于数据导出操作的结果
    /// </summary>
    public class ExportResultDto : OperationResultDto
    {
        /// <summary>导出数量</summary>
        [DisplayName("导出数量")]
        public int ExportedCount { get; set; }

        /// <summary>文件路径</summary>
        [DisplayName("文件路径")]
        public string? FilePath { get; set; }

        /// <summary>文件名</summary>
        [DisplayName("文件名")]
        public string? FileName { get; set; }

        /// <summary>文件大小（字节）</summary>
        [DisplayName("文件大小")]
        public long FileSize { get; set; }

        /// <summary>导出格式</summary>
        [DisplayName("导出格式")]
        public string ExportFormat { get; set; } = "xlsx";

        /// <summary>导出时间</summary>
        [DisplayName("导出时间")]
        public DateTime ExportTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 删除结果DTO - 用于删除操作的结果
    /// </summary>
    public class DeleteResultDto : OperationResultDto
    {
        /// <summary>删除的ID</summary>
        [DisplayName("删除的ID")]
        public Guid? DeletedId { get; set; }

        /// <summary>删除数量</summary>
        [DisplayName("删除数量")]
        public int DeletedCount { get; set; }

        /// <summary>是否软删除</summary>
        [DisplayName("软删除")]
        public bool IsSoftDelete { get; set; } = true;
    }

    /// <summary>
    /// 验证结果DTO - 用于数据验证的结果
    /// </summary>
    public class ValidationResultDto
    {
        /// <summary>是否有效</summary>
        [DisplayName("验证通过")]
        public bool IsValid { get; set; } = true;

        /// <summary>验证错误列表</summary>
        [DisplayName("验证错误")]
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>验证警告列表</summary>
        [DisplayName("验证警告")]
        public List<ValidationWarning> Warnings { get; set; } = new();

        /// <summary>验证错误</summary>
        public class ValidationError
        {
            /// <summary>字段名</summary>
            public string FieldName { get; set; } = string.Empty;

            /// <summary>错误消息</summary>
            public string ErrorMessage { get; set; } = string.Empty;

            /// <summary>错误代码</summary>
            public string? ErrorCode { get; set; }
        }

        /// <summary>验证警告</summary>
        public class ValidationWarning
        {
            /// <summary>字段名</summary>
            public string FieldName { get; set; } = string.Empty;

            /// <summary>警告消息</summary>
            public string WarningMessage { get; set; } = string.Empty;
        }
    }
}
