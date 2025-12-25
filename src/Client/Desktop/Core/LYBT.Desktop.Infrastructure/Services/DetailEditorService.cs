using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 详情编辑服务实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public partial class DetailEditorService<TDetail> : ObservableObject, IDetailEditorService<TDetail>
        where TDetail : class
    {
        private Func<TDetail, TDetail>? _cloneFunc;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNew))]
        private TDetail? _currentDetail;

        [ObservableProperty]
        private TDetail? _originalDetail;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        [ObservableProperty]
        private bool _isLoadingDetail;

        private bool _isNewFlag;

        /// <inheritdoc/>
        public bool IsNew => _isNewFlag;

        /// <inheritdoc/>
        public event EventHandler<EditModeChangedEventArgs>? EditModeChanged;

        /// <inheritdoc/>
        public void EnterEditMode()
        {
            if (CurrentDetail == null) return;

            IsEditMode = true;
            EditModeChanged?.Invoke(this, new EditModeChangedEventArgs(true, IsNew));
        }

        /// <inheritdoc/>
        public void CancelEdit()
        {
            if (OriginalDetail != null && _cloneFunc != null)
            {
                CurrentDetail = _cloneFunc(OriginalDetail);
            }

            IsEditMode = false;
            HasUnsavedChanges = false;
            _isNewFlag = false;
            EditModeChanged?.Invoke(this, new EditModeChangedEventArgs(false, false));
        }

        /// <inheritdoc/>
        public void ConfirmSaved()
        {
            IsEditMode = false;
            HasUnsavedChanges = false;
            _isNewFlag = false;

            // 更新原始值为当前保存后的值
            if (CurrentDetail != null && _cloneFunc != null)
            {
                OriginalDetail = _cloneFunc(CurrentDetail);
            }

            EditModeChanged?.Invoke(this, new EditModeChangedEventArgs(false, false));
        }

        /// <inheritdoc/>
        public void CreateNew(Func<TDetail> factory)
        {
            CurrentDetail = factory();
            OriginalDetail = null;
            _isNewFlag = true;
            IsEditMode = true;
            HasUnsavedChanges = false;
            OnPropertyChanged(nameof(IsNew));
            EditModeChanged?.Invoke(this, new EditModeChangedEventArgs(true, true));
        }

        /// <inheritdoc/>
        public void LoadDetail(TDetail detail, Func<TDetail, TDetail>? clone = null)
        {
            _cloneFunc = clone;
            CurrentDetail = detail;
            OriginalDetail = clone?.Invoke(detail);
            _isNewFlag = false;
            IsEditMode = false;
            HasUnsavedChanges = false;
            OnPropertyChanged(nameof(IsNew));
        }

        /// <inheritdoc/>
        public void MarkAsChanged()
        {
            HasUnsavedChanges = true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            CurrentDetail = null;
            OriginalDetail = null;
            _isNewFlag = false;
            IsEditMode = false;
            HasUnsavedChanges = false;
            OnPropertyChanged(nameof(IsNew));
        }
    }
}
