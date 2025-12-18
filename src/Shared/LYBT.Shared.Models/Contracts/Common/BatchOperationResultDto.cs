using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
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
}
