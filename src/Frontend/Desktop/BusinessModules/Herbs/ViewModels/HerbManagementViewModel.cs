using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Shared;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Navigation.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Herbs.Shared.ViewModels
{
    /// <summary>
    /// 中药材管理视图模型
    /// </summary>
    public class HerbManagementViewModel : BaseManagementViewModel<HerbDto>
    {
        private readonly ISharedHerbService _herbService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<HerbManagementViewModel> _logger;

        private string _searchKeyword = string.Empty;
        private HerbDto _selectedHerb;

        public HerbManagementViewModel(
            ISharedHerbService herbService,
            IDialogService dialogService,
            ILogger<HerbManagementViewModel> logger)
            : base(logger)
        {
            _herbService = herbService;
            _dialogService = dialogService;
            _logger = logger;

            Title = "中药材管理";
            InitializeCommands();
            
            // 自动加载数据
            _ = LoadDataAsync();
        }

        #region Properties

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 选中的中药材
        /// </summary>
        public HerbDto SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                SetProperty(ref _selectedHerb, value);
                UpdateCommandStates();
            }
        }

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddHerbCommand { get; private set; }
        public DelegateCommand EditHerbCommand { get; private set; }
        public DelegateCommand ToggleStatusCommand { get; private set; }
        public DelegateCommand ViewHerbCommand { get; private set; }

        #endregion

        #region Methods

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            SearchCommand = new DelegateCommand(async () => await SearchHerbsAsync());
            AddHerbCommand = new DelegateCommand(async () => await AddHerbAsync());
            EditHerbCommand = new DelegateCommand(async () => await EditHerbAsync(), () => SelectedHerb != null);
            ToggleStatusCommand = new DelegateCommand(async () => await ToggleHerbStatusAsync(), () => SelectedHerb != null);
            ViewHerbCommand = new DelegateCommand(async () => await ViewHerbAsync(), () => SelectedHerb != null);
        }

        protected override async Task LoadDataAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _herbService.GetHerbsAsync(CurrentPage, PageSize, SearchKeyword);
                if (result.IsSuccess)
                {
                    var pagedData = result.Data;
                    Items = new ObservableCollection<HerbDto>(pagedData.Items);
                    TotalCount = pagedData.TotalCount;
                    TotalPages = pagedData.TotalPages;

                    _logger.LogInformation("中药材数据加载完成，共 {Count} 条记录", TotalCount);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                    _logger.LogWarning("中药材数据加载失败: {Error}", result.ErrorMessage);
                }
            });
        }

        /// <summary>
        /// 搜索中药材
        /// </summary>
        private async Task SearchHerbsAsync()
        {
            CurrentPage = 1; // 重置到第一页
            await LoadDataAsync();
        }

        /// <summary>
        /// 添加中药材
        /// </summary>
        private async Task AddHerbAsync()
        {
            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "添加中药材" },
                    { "Mode", "Add" }
                };

                _dialogService.ShowDialog("HerbAddEditDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("HerbData"))
                    {
                        var createDto = result.Parameters.GetValue<HerbCreateDto>("HerbData");
                        await ExecuteWithLoadingAsync(async () =>
                        {
                            var serviceResult = await _herbService.CreateHerbAsync(createDto);
                            if (serviceResult.IsSuccess)
                            {
                                await LoadDataAsync(); // 刷新列表
                                ShowSuccessMessage("中药材添加成功");
                                _logger.LogInformation("中药材添加成功: {HerbName}", createDto.Name);
                            }
                            else
                            {
                                ErrorMessage = serviceResult.ErrorMessage;
                                _logger.LogWarning("中药材添加失败: {Error}", serviceResult.ErrorMessage);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加中药材时发生错误");
                ErrorMessage = $"添加中药材时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 编辑中药材
        /// </summary>
        private async Task EditHerbAsync()
        {
            if (SelectedHerb == null)
                return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "编辑中药材" },
                    { "Mode", "Edit" },
                    { "HerbId", SelectedHerb.Id }
                };

                _dialogService.ShowDialog("HerbAddEditDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("HerbData"))
                    {
                        var updateDto = result.Parameters.GetValue<HerbUpdateDto>("HerbData");
                        await ExecuteWithLoadingAsync(async () =>
                        {
                            var serviceResult = await _herbService.UpdateHerbAsync(SelectedHerb.Id, updateDto);
                            if (serviceResult.IsSuccess)
                            {
                                await LoadDataAsync(); // 刷新列表
                                ShowSuccessMessage("中药材更新成功");
                                _logger.LogInformation("中药材更新成功: {HerbName}", updateDto.Name);
                            }
                            else
                            {
                                ErrorMessage = serviceResult.ErrorMessage;
                                _logger.LogWarning("中药材更新失败: {Error}", serviceResult.ErrorMessage);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑中药材时发生错误");
                ErrorMessage = $"编辑中药材时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 切换中药材状态
        /// </summary>
        private async Task ToggleHerbStatusAsync()
        {
            if (SelectedHerb == null)
                return;

            try
            {
                var statusText = SelectedHerb.Status == Shared.Models.Enums.CommonStatus.Enabled ? "禁用" : "启用";
                var confirmResult = MessageBox.Show(
                    $"确定要{statusText}中药材 '{SelectedHerb.Name}' 吗？",
                    "确认操作",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    await ExecuteWithLoadingAsync(async () =>
                    {
                        var result = await _herbService.ToggleHerbStatusAsync(SelectedHerb.Id);
                        if (result.IsSuccess)
                        {
                            await LoadDataAsync(); // 刷新列表
                            ShowSuccessMessage($"中药材{statusText}成功");
                            _logger.LogInformation("中药材状态切换成功: {HerbName} -> {Status}", 
                                SelectedHerb.Name, statusText);
                        }
                        else
                        {
                            ErrorMessage = result.ErrorMessage;
                            _logger.LogWarning("中药材状态切换失败: {Error}", result.ErrorMessage);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换中药材状态时发生错误");
                ErrorMessage = $"切换中药材状态时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 查看中药材详情
        /// </summary>
        private async Task ViewHerbAsync()
        {
            if (SelectedHerb == null)
                return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "中药材详情" },
                    { "Mode", "View" },
                    { "HerbId", SelectedHerb.Id }
                };

                _dialogService.ShowDialog("HerbAddEditDialog", dialogParameters, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看中药材详情时发生错误");
                ErrorMessage = $"查看中药材详情时发生错误: {ex.Message}";
            }
        }

        private void UpdateCommandStates()
        {
            EditHerbCommand?.RaiseCanExecuteChanged();
            ToggleStatusCommand?.RaiseCanExecuteChanged();
            ViewHerbCommand?.RaiseCanExecuteChanged();
        }

        private void ShowSuccessMessage(string message)
        {
            // TODO: 实现更好的成功消息提示
            MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}