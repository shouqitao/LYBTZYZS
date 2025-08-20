using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Managers
{
    /// <summary>
    /// 搜索管理器接口 - 负责搜索逻辑的统一管理
    /// UltraThink架构: 将搜索逻辑从ViewModel中分离出来，实现关注点分离
    /// </summary>
    public interface ISearchManager : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// 搜索关键字
        /// </summary>
        string SearchKeyword { get; set; }

        /// <summary>
        /// 是否有搜索条件
        /// </summary>
        bool HasSearchCriteria { get; }

        /// <summary>
        /// 是否正在搜索
        /// </summary>
        bool IsSearching { get; }

        /// <summary>
        /// 搜索延迟时间(毫秒) - 防抖功能
        /// </summary>
        int SearchDelay { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// 搜索执行事件
        /// </summary>
        event EventHandler<SearchExecutedEventArgs>? SearchExecuted;

        /// <summary>
        /// 搜索清除事件
        /// </summary>
        event EventHandler? SearchCleared;

        #endregion

        #region Methods

        /// <summary>
        /// 执行搜索
        /// </summary>
        Task ExecuteSearchAsync();

        /// <summary>
        /// 清除搜索条件
        /// </summary>
        void ClearSearch();

        /// <summary>
        /// 立即搜索(不使用防抖)
        /// </summary>
        Task SearchImmediatelyAsync();

        /// <summary>
        /// 设置搜索关键字并触发搜索
        /// </summary>
        Task SetSearchKeywordAsync(string keyword);

        #endregion
    }

    /// <summary>
    /// 搜索执行事件参数
    /// </summary>
    public class SearchExecutedEventArgs : EventArgs
    {
        public string SearchKeyword { get; }
        public DateTime ExecutedAt { get; }

        public SearchExecutedEventArgs(string searchKeyword)
        {
            SearchKeyword = searchKeyword;
            ExecutedAt = DateTime.Now;
        }
    }
}