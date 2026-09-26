using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models
{
    /// <summary>
    /// 批量导入进度（US-SHELL-021 AC③：分批导入的可跟踪进度）。
    /// </summary>
    /// <remarks>
    /// 由导入流程在每批提交完成后更新；绑定到导入报告面板的进度条与文本。
    /// 由 Patients.Models 迁入 Infrastructure：三类导入（患者/药材/验方）分属不同模块，需共享同一进度模型。
    /// </remarks>
    public partial class ImportProgressInfo : ObservableObject
    {
        /// <summary>进度百分比 (0-100)</summary>
        [ObservableProperty]
        private int _percentComplete;

        /// <summary>当前处理的项目描述</summary>
        [ObservableProperty]
        private string _currentItem = string.Empty;

        /// <summary>已处理数量（已提交行数）</summary>
        [ObservableProperty]
        private int _processedCount;

        /// <summary>总数量（文件行数）</summary>
        [ObservableProperty]
        private int _totalCount;

        /// <summary>状态消息（如「已导入 1000/2500 行」）</summary>
        [ObservableProperty]
        private string _message = string.Empty;

        /// <summary>开始一次新的导入（重置进度并记录总行数）</summary>
        /// <param name="totalCount">本次导入的文件总行数</param>
        public void Reset(int totalCount)
        {
            TotalCount = totalCount;
            ProcessedCount = 0;
            PercentComplete = 0;
            CurrentItem = string.Empty;
            Message = totalCount > 0 ? $"准备导入 {totalCount} 行" : string.Empty;
        }

        /// <summary>记录一批提交完成后的进度</summary>
        /// <param name="processedCount">累计已提交行数</param>
        /// <param name="currentItem">当前批次描述（如「第 2/3 批」）</param>
        public void Report(int processedCount, string? currentItem = null)
        {
            ProcessedCount = processedCount;
            if (currentItem != null)
            {
                CurrentItem = currentItem;
            }

            // 先乘后除（long 防溢出）；总量为 0 时视为已完成的空导入
            PercentComplete = TotalCount > 0 ? (int)((long)processedCount * 100 / TotalCount) : 100;
            Message = $"已导入 {processedCount}/{TotalCount} 行";
        }
    }
}
