using System.Collections.ObjectModel;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// 导航历史栈管理器 - 维护Back/Forward栈、当前条目及对应的可观察集合
    /// </summary>
    internal sealed class NavigationHistoryManager
    {
        private readonly Stack<NavigationEntry> _history = new();
        private readonly Stack<NavigationEntry> _forwardStack = new();
        private NavigationEntry? _currentEntry;

        private readonly ObservableCollection<NavigationEntry> _historyCollection = new();
        private readonly ObservableCollection<NavigationEntry> _forwardCollection = new();

        /// <summary>
        /// 历史记录（只读可观察集合，按最近到最远排序）
        /// </summary>
        public ReadOnlyObservableCollection<NavigationEntry> History { get; }

        /// <summary>
        /// 前进栈（只读可观察集合）
        /// </summary>
        public ReadOnlyObservableCollection<NavigationEntry> ForwardStack { get; }

        /// <summary>
        /// 当前导航条目（可能为null）
        /// </summary>
        public NavigationEntry? CurrentEntry => _currentEntry;

        /// <summary>
        /// 是否可以返回
        /// </summary>
        public bool CanGoBack => _history.Count > 0;

        /// <summary>
        /// 是否可以前进
        /// </summary>
        public bool CanGoForward => _forwardStack.Count > 0;

        public NavigationHistoryManager()
        {
            History = new ReadOnlyObservableCollection<NavigationEntry>(_historyCollection);
            ForwardStack = new ReadOnlyObservableCollection<NavigationEntry>(_forwardCollection);
        }

        /// <summary>
        /// 将当前条目推入历史栈（在导航到新位置前调用）
        /// </summary>
        public void PushToHistory(NavigationEntry entry)
        {
            _history.Push(entry);
            UpdateHistoryCollection();
        }

        /// <summary>
        /// 弹出历史栈顶（用于执行返回操作）
        /// </summary>
        public NavigationEntry PopFromHistory()
        {
            var entry = _history.Pop();
            UpdateHistoryCollection();
            return entry;
        }

        /// <summary>
        /// 撤销最近一次历史压栈（导航失败时回滚）
        /// </summary>
        public void RollbackLastHistoryPush()
        {
            if (_history.Count > 0)
            {
                _history.Pop();
                UpdateHistoryCollection();
            }
        }

        /// <summary>
        /// 推入前进栈（执行返回时调用）
        /// </summary>
        public void PushToForward(NavigationEntry entry)
        {
            _forwardStack.Push(entry);
            UpdateForwardCollection();
        }

        /// <summary>
        /// 弹出前进栈顶（用于执行前进操作）
        /// </summary>
        public NavigationEntry PopFromForward()
        {
            var entry = _forwardStack.Pop();
            UpdateForwardCollection();
            return entry;
        }

        /// <summary>
        /// 清除前进栈（导航到新位置时调用）
        /// </summary>
        public void ClearForward()
        {
            _forwardStack.Clear();
            UpdateForwardCollection();
        }

        /// <summary>
        /// 设置当前导航条目
        /// </summary>
        public void SetCurrent(NavigationEntry? entry)
        {
            _currentEntry = entry;
        }

        /// <summary>
        /// 清除所有历史（历史栈、前进栈、当前条目）
        /// </summary>
        public void ClearAll()
        {
            _history.Clear();
            _forwardStack.Clear();
            _currentEntry = null;
            UpdateHistoryCollection();
            UpdateForwardCollection();
        }

        /// <summary>
        /// 获取历史栈的正向枚举（最旧到最新，用于频率统计/建议）
        /// </summary>
        public IEnumerable<NavigationEntry> GetHistoryEntries() => _history;

        private void UpdateHistoryCollection()
        {
            _historyCollection.Clear();
            foreach (var entry in _history.Reverse())
            {
                _historyCollection.Add(entry);
            }
        }

        private void UpdateForwardCollection()
        {
            _forwardCollection.Clear();
            foreach (var entry in _forwardStack.Reverse())
            {
                _forwardCollection.Add(entry);
            }
        }
    }
}
