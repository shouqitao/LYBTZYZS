using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Services;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Windows;

namespace LYBT.WPF.Client.Modules.Registration.ViewModels
{
    /// <summary>
    /// 挂号管理视图模型
    /// </summary>
    public class RegistrationMainViewModel : BindableBase
    {
        private readonly IApiService _apiService;
        private string _searchText = string.Empty;
        private bool _isLoading;

        public RegistrationMainViewModel(IApiService apiService)
        {
            _apiService = apiService;
            
            SearchCommand = new DelegateCommand(ExecuteSearchCommand);
            RefreshCommand = new DelegateCommand(ExecuteRefreshCommand);
            AddRegistrationCommand = new DelegateCommand(ExecuteAddRegistrationCommand);
            EditRegistrationCommand = new DelegateCommand<object>(ExecuteEditRegistrationCommand);
            DeleteRegistrationCommand = new DelegateCommand<object>(ExecuteDeleteRegistrationCommand);
            
            Registrations = new ObservableCollection<dynamic>();
            LoadRegistrations();
        }

        #region Properties

        public ObservableCollection<dynamic> Registrations { get; }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddRegistrationCommand { get; }
        public DelegateCommand<object> EditRegistrationCommand { get; }
        public DelegateCommand<object> DeleteRegistrationCommand { get; }

        #endregion

        #region Command Handlers

        private void ExecuteSearchCommand()
        {
            LoadRegistrations();
        }

        private void ExecuteRefreshCommand()
        {
            SearchText = string.Empty;
            LoadRegistrations();
        }

        private void ExecuteAddRegistrationCommand()
        {
            // TODO: 实现添加挂号逻辑
            MessageBox.Show("添加挂号功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteEditRegistrationCommand(object parameter)
        {
            if (parameter != null)
            {
                // TODO: 实现编辑挂号逻辑
                MessageBox.Show("编辑挂号功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteDeleteRegistrationCommand(object parameter)
        {
            if (parameter != null)
            {
                var result = MessageBox.Show("确定要删除这条挂号记录吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: 实现删除挂号逻辑
                    MessageBox.Show("删除挂号功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Private Methods

        private async void LoadRegistrations()
        {
            try
            {
                IsLoading = true;
                
                // TODO: 调用API获取挂号数据
                await Task.Delay(1000); // 模拟API调用
                
                // 临时测试数据
                Registrations.Clear();
                for (int i = 1; i <= 10; i++)
                {
                    Registrations.Add(new
                    {
                        Id = i,
                        PatientName = $"患者{i}",
                        DoctorName = $"医生{i}",
                        RegistrationDate = DateTime.Now.AddDays(-i),
                        Status = i % 2 == 0 ? "已完成" : "待就诊"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载挂号数据失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}