using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 通用批量删除输入DTO
    /// OpenSpec: optimize-batch-operations Phase 2
    /// </summary>
    public class BatchDeleteInputDto
    {
        /// <summary>
        /// 要删除的实体ID列表
        /// </summary>
        [Required(ErrorMessage = "ID列表不能为空")]
        [MinLength(1, ErrorMessage = "至少选择一个项目")]
        [DisplayName("ID列表")]
        public List<Guid> Ids { get; set; } = new();
    }

    /// <summary>
    /// 通用批量操作结果DTO - 统一用于批量导入和批量删除
    /// OpenSpec: optimize-batch-operations Phase 2 - 合并导入/删除结果DTO
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

        /// <summary>成功的ID列表（用于导入操作）</summary>
        [DisplayName("成功的ID列表")]
        public List<Guid> SuccessfulIds { get; set; } = new();

        /// <summary>失败的ID列表</summary>
        [DisplayName("失败的ID列表")]
        public List<Guid> FailedIds { get; set; } = new();

        /// <summary>错误详情列表（用于导入操作）</summary>
        [DisplayName("错误详情")]
        public List<ErrorDetail> Errors { get; set; } = new();

        /// <summary>失败的项目信息（用于删除操作）</summary>
        [DisplayName("失败项目")]
        public List<BatchOperationFailureItem> FailedItems { get; set; } = new();

        /// <summary>操作成功率</summary>
        [DisplayName("成功率")]
        public decimal SuccessRate => TotalCount > 0 ? (decimal)SuccessCount / TotalCount * 100 : 0;

        /// <summary>错误详情（用于导入操作）</summary>
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
    /// 批量操作失败项（用于删除操作）
    /// </summary>
    public class BatchOperationFailureItem
    {
        /// <summary>失败的实体ID</summary>
        public Guid Id { get; set; }

        /// <summary>实体名称/标识（用于显示）</summary>
        public string? Name { get; set; }

        /// <summary>失败原因</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
