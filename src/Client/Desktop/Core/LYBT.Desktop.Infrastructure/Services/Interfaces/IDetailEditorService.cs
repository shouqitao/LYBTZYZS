using System.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 详情编辑服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供详情编辑状态管理、原始值备份、变更检测
    /// </summary>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public interface IDetailEditorService<TDetail> : INotifyPropertyChanged where TDetail : class
    {
        /// <summary>当前编辑的详情</summary>
        TDetail? CurrentDetail { get; set; }

        /// <summary>原始详情（用于取消还原）</summary>
        TDetail? OriginalDetail { get; }

        /// <summary>是否处于编辑模式</summary>
        bool IsEditMode { get; set; }

        /// <summary>是否有未保存的更改</summary>
        bool HasUnsavedChanges { get; }

        /// <summary>是否为新建</summary>
        bool IsNew { get; }

        /// <summary>是否正在加载详情</summary>
        bool IsLoadingDetail { get; set; }

        /// <summary>
        /// 编辑模式变更事件
        /// </summary>
        event EventHandler<EditModeChangedEventArgs>? EditModeChanged;

        /// <summary>
        /// 进入编辑模式
        /// </summary>
        void EnterEditMode();

        /// <summary>
        /// 取消编辑
        /// </summary>
        void CancelEdit();

        /// <summary>
        /// 确认保存完成
        /// </summary>
        void ConfirmSaved();

        /// <summary>
        /// 创建新详情
        /// </summary>
        /// <param name="factory">详情工厂方法</param>
        void CreateNew(Func<TDetail> factory);

        /// <summary>
        /// 加载详情
        /// </summary>
        /// <param name="detail">详情对象</param>
        /// <param name="clone">克隆方法（用于备份原始值）</param>
        void LoadDetail(TDetail detail, Func<TDetail, TDetail>? clone = null);

        /// <summary>
        /// 标记有未保存更改
        /// </summary>
        void MarkAsChanged();

        /// <summary>
        /// 清除编辑状态
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 编辑模式变更事件参数
    /// </summary>
    public class EditModeChangedEventArgs : EventArgs
    {
        /// <summary>是否进入编辑模式</summary>
        public bool IsEditMode { get; }

        /// <summary>是否为新建</summary>
        public bool IsNew { get; }

        public EditModeChangedEventArgs(bool isEditMode, bool isNew)
        {
            IsEditMode = isEditMode;
            IsNew = isNew;
        }
    }
}
