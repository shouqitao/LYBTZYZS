using System.Globalization;

namespace LYBT.Desktop.Infrastructure.Models
{
    /// <summary>
    /// 单条导入失败记录（US-SHELL-021 AC②：错误行定位）。
    /// </summary>
    public sealed class ImportReportFailure
    {
        /// <summary>文件行号（Excel 行号，第 1 行为表头）；0 表示整批失败，无单一行号</summary>
        public int RowNumber { get; init; }

        /// <summary>记录标识（名称，或整批失败时的行区间描述）</summary>
        public string Identifier { get; init; } = string.Empty;

        /// <summary>失败原因</summary>
        public string Reason { get; init; } = string.Empty;

        /// <summary>行号显示文本（0 → 「整批」）</summary>
        public string RowNumberText => RowNumber > 0
            ? RowNumber.ToString(CultureInfo.InvariantCulture)
            : "整批";
    }

    /// <summary>
    /// 批量导入报告（US-SHELL-021 AC⑤：成功/失败/跳过总数 + 失败明细行号）。
    /// </summary>
    /// <remarks>
    /// <para>由每个批次的服务端结果累加而成：<see cref="TotalCount"/> 为**已提交行数**（成功+失败+跳过），
    /// 不依赖服务端返回的 TotalCount（各模块处理器并不都回填）。</para>
    /// <para>服务端失败行号是**请求内相对行号**（首数据行 = 2，与单次整文件导入一致）；
    /// 分批导入时由调用方加上批次偏移（<c>offset + 服务端行号</c>）还原为文件行号。</para>
    /// <para>某批请求整体失败（网络/服务端错误）时用 <see cref="Abort"/> 记录该批全部行：
    /// <see cref="FailureCount"/> 计入该批行数，失败明细只留一条区间说明，并中止后续批次
    /// （服务端回滚该批——AC④）。</para>
    /// </remarks>
    public sealed class ImportReport
    {
        /// <summary>已提交行数（成功 + 失败 + 跳过）</summary>
        public int TotalCount { get; private set; }

        /// <summary>成功行数</summary>
        public int SuccessCount { get; private set; }

        /// <summary>失败行数</summary>
        public int FailureCount { get; private set; }

        /// <summary>跳过行数（重复策略 Skip）</summary>
        public int SkippedCount { get; private set; }

        /// <summary>已提交批次数</summary>
        public int BatchCount { get; private set; }

        /// <summary>是否因某批整体失败而中止</summary>
        public bool IsAborted { get; private set; }

        /// <summary>中止原因（未中止时为空）</summary>
        public string AbortReason { get; private set; } = string.Empty;

        /// <summary>失败明细（含行号）</summary>
        public List<ImportReportFailure> Failures { get; } = new();

        /// <summary>是否存在失败明细</summary>
        public bool HasFailures => Failures.Count > 0;

        /// <summary>附加说明（如验方导入的药材匹配数；无则为空串）</summary>
        public string ExtraSummary { get; private set; } = string.Empty;

        /// <summary>总数汇总文本（界面直接绑定）</summary>
        public string Summary
        {
            get
            {
                var summary = $"共 {TotalCount} 行（{BatchCount} 批）：成功 {SuccessCount}，失败 {FailureCount}，跳过 {SkippedCount}";
                if (ExtraSummary.Length > 0)
                {
                    summary = $"{summary}；{ExtraSummary}";
                }

                return IsAborted ? $"{summary}；已中止——{AbortReason}" : summary;
            }
        }

        /// <summary>设置附加说明（在全部批次提交完成后调用）</summary>
        /// <param name="text">附加说明文本</param>
        public void SetExtraSummary(string? text) => ExtraSummary = text ?? string.Empty;

        /// <summary>累加一个批次的导入结果</summary>
        /// <param name="rowCount">该批提交行数</param>
        /// <param name="successCount">成功行数</param>
        /// <param name="failureCount">失败行数</param>
        /// <param name="skippedCount">跳过行数</param>
        public void AddBatch(int rowCount, int successCount, int failureCount, int skippedCount)
        {
            BatchCount++;
            TotalCount += rowCount;
            SuccessCount += successCount;
            FailureCount += failureCount;
            SkippedCount += skippedCount;
        }

        /// <summary>记录一条行级失败</summary>
        /// <param name="rowNumber">文件行号（服务端相对行号 + 批次偏移）</param>
        /// <param name="identifier">记录标识（名称等）</param>
        /// <param name="reason">失败原因</param>
        public void AddFailure(int rowNumber, string? identifier, string? reason)
        {
            Failures.Add(new ImportReportFailure
            {
                RowNumber = rowNumber,
                Identifier = identifier ?? string.Empty,
                Reason = reason ?? string.Empty
            });
        }

        /// <summary>记录某批整体失败（该批行数计入失败），并标记后续批次不再提交</summary>
        /// <param name="rowCount">该批提交行数</param>
        /// <param name="firstRowNumber">该批首行的文件行号</param>
        /// <param name="reason">失败原因</param>
        public void Abort(int rowCount, int firstRowNumber, string reason)
        {
            BatchCount++;
            TotalCount += rowCount;
            FailureCount += rowCount;
            IsAborted = true;
            AbortReason = reason;

            Failures.Add(new ImportReportFailure
            {
                RowNumber = firstRowNumber,
                Identifier = rowCount > 1 ? $"{firstRowNumber}-{firstRowNumber + rowCount - 1} 行" : $"{firstRowNumber} 行",
                Reason = reason
            });
        }
    }
}
