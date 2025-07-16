using LYBT.Common.Enums;
using LYBT.Common.Models;
using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.UI.WPF.ViewModels.Base;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 药材管理视图模型，支持分页
    /// </summary>
    public class HerbManagementViewModel : BaseListViewModel<HerbDto> {
        private readonly IHerbService _herbService;

        public ObservableCollection<HerbDto> Herbs => Items;

        private HerbDto? _selectedHerb;
        public HerbDto? SelectedHerb {
            get => _selectedHerb;
            set {
                if (SetProperty(ref _selectedHerb, value)) {
                    if (value != null)
                        _ = HerbProfileViewModel.LoadAsync(value.Id, ProfileMode.View);
                }
            }
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword { get => _searchKeyword; set => SetProperty(ref _searchKeyword, value); }

        public HerbProfileViewModel HerbProfileViewModel { get; }

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ImportCommand { get; }
        public DelegateCommand ExportCommand { get; }

        public HerbManagementViewModel(IHerbService herbService, HerbProfileViewModel profileViewModel) {
            _herbService = herbService;
            HerbProfileViewModel = profileViewModel;
            SearchCommand = new DelegateCommand(async () => await LoadPageAsync(1));
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedHerb != null).ObservesProperty(() => SelectedHerb);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => SelectedHerb != null).ObservesProperty(() => SelectedHerb);
            ImportCommand = new DelegateCommand(async () => await ImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExportAsync());
            _ = LoadPageAsync();
        }

        protected override async Task<PagedResultDto<HerbDto>> GetPagedAsync(int page, int pageSize) {
            var query = new HerbPagedQueryDto { Keyword = SearchKeyword, Page = page, PageSize = pageSize };
            return await _herbService.GetPagedAsync(query);
        }

        private void Add() {
            HerbProfileViewModel.CancelAction = async () => {
                await LoadPageAsync(CurrentPage);
                await HerbProfileViewModel.LoadAsync(SelectedHerb?.Id, ProfileMode.View);
            };
            HerbProfileViewModel.LoadAsync(null, ProfileMode.Create);
        }

        private void Edit() {
            if (SelectedHerb != null) {
                var id = SelectedHerb.Id;
                HerbProfileViewModel.CancelAction = async () => {
                    await LoadPageAsync(CurrentPage);
                    await HerbProfileViewModel.LoadAsync(id, ProfileMode.View);
                };
                HerbProfileViewModel.LoadAsync(id, ProfileMode.Edit);
            }
        }

        private async Task DeleteAsync() {
            if (SelectedHerb == null)
                return;
            if (MessageBox.Show("确定删除该药材吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                var success = await _herbService.DeleteAsync(SelectedHerb.Id);
                if (!success)
                    MessageBox.Show("删除失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadPageAsync(CurrentPage);
            }
        }

        private async Task ImportAsync() {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var count = await _herbService.ImportFromExcelAsync(dlg.FileName);
                    MessageBox.Show($"成功导入 {count} 条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPageAsync(CurrentPage);
                } catch (Exception ex) {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportAsync() {
            var dlg = new Microsoft.Win32.SaveFileDialog {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FileName = "药材.xlsx"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var count = await _herbService.ExportToExcelAsync(dlg.FileName);
                    MessageBox.Show($"已导出 {count} 条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                } catch (Exception ex) {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
