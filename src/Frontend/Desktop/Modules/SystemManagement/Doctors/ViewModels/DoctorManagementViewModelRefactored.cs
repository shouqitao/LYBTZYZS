using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Modules.SystemManagement.Doctors.Views;
using LYBT.Shared.Models.Common;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Doctors.ViewModels
{
    /// <summary>
    /// 医生管理视图模型 - 重构版本，基于BaseManagementViewModel
    /// </summary>
    public class DoctorManagementViewModelRefactored : BaseManagementViewModel<DoctorInfo, IDoctorService>
    {
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _dialogService;

        protected override string ModuleName => "医生";

        public DoctorManagementViewModelRefactored(IDoctorService doctorService,
            ICommonDialogService commonDialogService,
            IDialogService dialogService)
            : base(doctorService)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
        }

        #region 实现抽象方法

        /// <summary>
        /// 从服务加载医生数据（由于服务不支持分页，这里手动实现）
        /// </summary>
        protected override async Task<ServiceResult<PagedResult<DoctorInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                // 获取所有医生数据
                var result = await Service.GetDoctorsAsync();
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return ServiceResult<PagedResult<DoctorInfo>>.Failure(
                        result.ErrorMessage ?? "加载医生列表失败", 
                        result.Exception);
                }

                var allDoctors = result.Data;

                // 应用搜索过滤
                if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
                {
                    var keyword = request.SearchKeyword.ToUpper();
                    allDoctors = allDoctors.Where(d =>
                        d.Name.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        (d.Department?.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (d.Code?.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (d.Phone?.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (d.PinYinCode != null && d.PinYinCode.Contains(keyword)) ||
                        (d.WuBiCode != null && d.WuBiCode.Contains(keyword))
                    ).ToList();
                }

                // 手动实现分页
                var totalCount = allDoctors.Count;
                var pagedDoctors = allDoctors
                    .Skip((request.CurrentPage - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 返回分页结果
                return ServiceResult<PagedResult<DoctorInfo>>.Success(new PagedResult<DoctorInfo>
                {
                    TotalCount = totalCount,
                    Items = pagedDoctors,
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<DoctorInfo>>.Failure($"加载医生列表失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从服务删除医生（使用软删除/禁用）
        /// </summary>
        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(DoctorInfo doctor)
        {
            if (doctor == null) return ServiceResult<bool>.Failure("医生信息不能为空");

            try
            {
                // 切换启用状态而不是真正删除
                doctor.IsActive = false;
                var result = await Service.UpdateDoctorAsync(doctor);
                
                if (result.IsSuccess)
                {
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    return ServiceResult<bool>.Failure(result.ErrorMessage ?? "禁用医生失败", result.Exception);
                }
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"禁用医生失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取医生显示名称
        /// </summary>
        protected override string GetItemDisplayName(DoctorInfo doctor)
        {
            return doctor?.Name ?? "未知医生";
        }

        #endregion

        #region 重写虚方法

        /// <summary>
        /// 执行新增医生
        /// </summary>
        protected override void ExecuteAdd()
        {
            try
            {
                var viewModel = new AddDoctorDialogViewModel(Service);
                var dialog = new AddDoctorDialog
                {
                    DataContext = viewModel,
                    Owner = Application.Current.MainWindow
                };

                viewModel.CloseDialogCallback = (success) =>
                {
                    dialog.DialogResult = success;
                    dialog.Close();
                    if (success)
                    {
                        RefreshCommand.Execute();
                    }
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开新增医生对话框失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 执行编辑医生
        /// </summary>
        protected override void ExecuteEdit(DoctorInfo doctor)
        {
            if (doctor == null) return;

            try
            {
                var viewModel = new EditDoctorDialogViewModel(Service, doctor.Id);
                var dialog = new EditDoctorDialog
                {
                    DataContext = viewModel,
                    Owner = Application.Current.MainWindow
                };

                viewModel.CloseDialogCallback = (success) =>
                {
                    dialog.DialogResult = success;
                    dialog.Close();
                    if (success)
                    {
                        RefreshCommand.Execute();
                    }
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开编辑医生对话框失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 执行查看医生详情
        /// </summary>
        protected override void ExecuteView(DoctorInfo doctor)
        {
            if (doctor == null) return;

            try
            {
                var parameters = new DialogParameters
                {
                    { "doctorId", doctor.Id }
                };

                _dialogService.ShowDialog("ViewDoctorDialog", parameters, result =>
                {
                    // 对话框关闭后的处理（如果需要）
                });
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开医生详情对话框失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion

        #region 扩展功能

        /// <summary>
        /// 切换医生状态命令（启用/禁用）
        /// </summary>
        public async Task ToggleDoctorStatusAsync(DoctorInfo doctor)
        {
            if (doctor == null) return;

            var action = doctor.IsActive ? "禁用" : "启用";
            var confirmResult = await _commonDialogService.ShowConfirmationAsync($"确定要{action}医生 {doctor.Name} 吗？", "确认");
                
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                doctor.IsActive = !doctor.IsActive;
                var result = await Service.UpdateDoctorAsync(doctor);
                
                if (result.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync($"{action}成功", "提示").GetAwaiter().GetResult();
                    RefreshCommand.Execute();
                }
                else
                {
                    // 恢复原状态
                    doctor.IsActive = !doctor.IsActive;
                    _commonDialogService.ShowErrorAsync($"{action}失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                // 恢复原状态
                doctor.IsActive = !doctor.IsActive;
                _commonDialogService.ShowErrorAsync($"{action}失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion
    }
}