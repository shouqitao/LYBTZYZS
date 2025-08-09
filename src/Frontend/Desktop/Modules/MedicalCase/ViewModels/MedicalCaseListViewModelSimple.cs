using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.WPF.Client.Core.ViewModels;
using Prism.Commands;
using Prism.Events;
using LYBT.WPF.Client.Core.Models.Common;

using Prism.Dialogs;
using LYBT.WPF.Client.Core.Extensions;
namespace LYBT.WPF.Client.Modules.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例列表视图模型 - 简化版
    /// </summary>
    public class MedicalCaseListViewModelSimple : BaseViewModel
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<MedicalCaseInfo> _medicalCases = new();
        public ObservableCollection<MedicalCaseInfo> MedicalCases
        {
            get => _medicalCases;
            set => SetProperty(ref _medicalCases, value);
        }

        private MedicalCaseInfo? _selectedMedicalCase;
        public MedicalCaseInfo? SelectedMedicalCase
        {
            get => _selectedMedicalCase;
            set => SetProperty(ref _selectedMedicalCase, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public DelegateCommand LoadDataCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddCommand { get; }
        public new DelegateCommand RefreshCommand { get; }

        public MedicalCaseListViewModelSimple(
            IMedicalCaseService medicalCaseService,
            IDialogService dialogService,
            IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            _medicalCaseService = medicalCaseService;
            _dialogService = dialogService;

            MedicalCases = new ObservableCollection<MedicalCaseInfo>();

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            AddCommand = new DelegateCommand(async () => await AddMedicalCaseAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());

            LoadDataCommand.Execute();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var query = new ExtendedPaginationRequest
                {
                    CurrentPage = 1,
                    PageSize = 50,
                    SearchKeyword = SearchKeyword
                };

                // 暂时使用简单的查询，待后端API完善后再更新
                var result = await Task.FromResult(new LYBT.WPF.Client.Core.Models.ServiceResult<PagedResult<MedicalCaseInfo>>
                {
                    IsSuccess = true,
                    Data = new PagedResult<MedicalCaseInfo>
                    {
                        Items = new System.Collections.Generic.List<MedicalCaseInfo>(),
                        TotalCount = 0,
                        CurrentPage = 1,
                        PageSize = 50
                    }
                });
                if (result.IsSuccess && result.Data != null)
                {
                    MedicalCases.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        MedicalCases.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载数据失败: {ex.Message}", "错误");
            }
        }

        private async Task SearchAsync()
        {
            await LoadDataAsync();
        }

        private async Task RefreshAsync()
        {
            SearchKeyword = string.Empty;
            await LoadDataAsync();
        }

        private async Task AddMedicalCaseAsync()
        {
            await _dialogService.ShowInformationAsync("新增功能待完善", "提示");
        }
    }
}