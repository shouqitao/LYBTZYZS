using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.Common.Enums;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace LYBT.UI.WPF.ViewModels.Admin {
    public class HerbManagementViewModel : BindableBase {
        private readonly IHerbService _herbService;
        private IList<HerbDto> _allHerbs = new List<HerbDto>();
        public ObservableCollection<HerbDto> Herbs { get; } = new();

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
            SearchCommand = new DelegateCommand(Search);
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedHerb != null).ObservesProperty(() => SelectedHerb);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => SelectedHerb != null).ObservesProperty(() => SelectedHerb);
            ImportCommand = new DelegateCommand(async () => await ImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExportAsync());
            _ = LoadAsync();
        }

        private async Task LoadAsync() {
            var list = await _herbService.GetListAsync();
            _allHerbs = list;
            ApplyFilter();
        }

        private void Search() => ApplyFilter();

        private void ApplyFilter() {
            Herbs.Clear();
            foreach (var item in _allHerbs.Where(h => string.IsNullOrWhiteSpace(SearchKeyword)
                || (h.Name?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (h.Pinyin?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false)))
                Herbs.Add(item);
        }

        private void Add() {
            HerbProfileViewModel.CancelAction = async () => {
                await LoadAsync();
                await HerbProfileViewModel.LoadAsync(SelectedHerb?.Id, ProfileMode.View);
            };
            HerbProfileViewModel.LoadAsync(null, ProfileMode.Create);
        }

        private void Edit() {
            if (SelectedHerb != null) {
                HerbProfileViewModel.CancelAction = async () => {
                    await LoadAsync();
                    await HerbProfileViewModel.LoadAsync(SelectedHerb.Id, ProfileMode.View);
                };
                HerbProfileViewModel.LoadAsync(SelectedHerb.Id, ProfileMode.Edit);
            }
        }

        private async Task DeleteAsync() {
            if (SelectedHerb == null)
                return;
            if (MessageBox.Show("确定删除该药材吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                var success = await _herbService.DeleteAsync(SelectedHerb.Id);
                if (!success)
                    MessageBox.Show("删除失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadAsync();
            }
        }

        private async Task ImportAsync() {
            var dlg = new Microsoft.Win32.OpenFileDialog {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var json = await File.ReadAllTextAsync(dlg.FileName);
                    var list = JsonSerializer.Deserialize<List<HerbDetailDto>>(json);
                    if (list != null) {
                        var count = await _herbService.ImportAsync(list);
                        MessageBox.Show($"成功导入 {count} 条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadAsync();
                    }
                } catch (Exception ex) {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportAsync() {
            var dlg = new Microsoft.Win32.SaveFileDialog {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = "herbs.json"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var data = await _herbService.ExportAsync();
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(dlg.FileName, json);
                    MessageBox.Show($"已导出 {data.Count} 条记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                } catch (Exception ex) {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
