namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 批量导入分批执行器（US-SHELL-021 AC③：每批 ≤1000 行、顺序提交、进度可跟踪）。
    /// </summary>
    /// <remarks>
    /// 分批是**客户端传输细节**：服务端仍按单次请求处理每批，并返回该批的
    /// 成功/失败/跳过与失败行号（请求内相对行号，由调用方加批次偏移还原文件行号）。
    /// 某批返回 <c>false</c>（整批失败）时立即停止后续批次，避免在服务端已回滚后继续写入。
    /// </remarks>
    public static class ImportBatchRunner
    {
        /// <summary>单批最大行数（AC③）</summary>
        public const int MaxRowsPerBatch = 1000;

        /// <summary>计算指定行数需要的批次数（0 行 → 0 批）</summary>
        /// <param name="totalRows">总行数</param>
        /// <param name="maxRowsPerBatch">单批最大行数</param>
        public static int CountBatches(int totalRows, int maxRowsPerBatch = MaxRowsPerBatch)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxRowsPerBatch, 1);
            return totalRows <= 0 ? 0 : (totalRows + maxRowsPerBatch - 1) / maxRowsPerBatch;
        }

        /// <summary>按 ≤<paramref name="maxRowsPerBatch"/> 行分批顺序提交</summary>
        /// <typeparam name="TRow">导入行类型</typeparam>
        /// <param name="rows">待导入的全部行（解析器输出的顺序）</param>
        /// <param name="importBatchAsync">提交一批：参数为（批次行、该批首行在 <paramref name="rows"/> 中的偏移）；
        /// 返回 false 表示该批整体失败（调用方已记入报告），执行器随即停止</param>
        /// <param name="onProgress">每批成功后回调（累计已提交行数, 总行数）</param>
        /// <param name="maxRowsPerBatch">单批最大行数</param>
        public static async Task RunAsync<TRow>(
            IReadOnlyList<TRow> rows,
            Func<List<TRow>, int, Task<bool>> importBatchAsync,
            Action<int, int>? onProgress = null,
            int maxRowsPerBatch = MaxRowsPerBatch)
        {
            ArgumentNullException.ThrowIfNull(rows);
            ArgumentNullException.ThrowIfNull(importBatchAsync);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxRowsPerBatch, 1);

            for (var offset = 0; offset < rows.Count; offset += maxRowsPerBatch)
            {
                var count = Math.Min(maxRowsPerBatch, rows.Count - offset);
                var batch = new List<TRow>(count);
                for (var i = 0; i < count; i++)
                {
                    batch.Add(rows[offset + i]);
                }

                if (!await importBatchAsync(batch, offset))
                {
                    return;
                }

                onProgress?.Invoke(offset + count, rows.Count);
            }
        }
    }
}
