using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Patients.Base;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型（简化重构版）
    /// </summary>
    public class PatientManagementViewModelSimple : BaseServiceManagementViewModel<PatientInfo, IPatientService>
    {
        private readonly IDialogService _commonDialogService;
        private readonly IDialogService _dialogService;
        private readonly IPatientApiService _patientApiService;

        protected override string ModuleName => "患者管理";

        #region Commands

        public DelegateCommand<PatientInfo> ToggleStatusCommand { get; }
        public DelegateCommand<PatientInfo> ViewDetailsCommand { get; }

        #endregion

        public PatientManagementViewModelSimple(
            IPatientService patientService,
            IPatientApiService patientApiService,
            IDialogService commonDialogService,
            IDialogService dialogService,
            Prism.Events.IEventAggregator eventAggregator)
            : base(patientService, eventAggregator)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            _patientApiService = patientApiService;

            // 初始化命令
            ToggleStatusCommand = new DelegateCommand<PatientInfo>(async patient => await ToggleStatusAsync(patient));
            ViewDetailsCommand = new DelegateCommand<PatientInfo>(async patient => await ViewDetailsAsync(patient));
        }

        #region 重写基类方法

        protected override async Task<ServiceResult<LYBT.Desktop.Core.Models.Common.PagedResult<PatientInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                var query = new PatientPagedQueryDto
                {
                    PageIndex = request.CurrentPage,
                    PageSize = request.PageSize,
                    Keyword = SearchKeyword
                };

                var result = await Service.GetPagedAsync(query);
                return ServiceResult<LYBT.Desktop.Core.Models.Common.PagedResult<PatientInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<LYBT.Desktop.Core.Models.Common.PagedResult<PatientInfo>>.Failure($"加载患者列表失败: {ex.Message}");
            }
        }

        protected override async Task AddAsync()
        {
            try
            {
                var dialog = new Views.PatientAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "新增患者";

                // 创建ViewModel并设置为添加模式
                var viewModel = new PatientAddEditDialogViewModel(_patientApiService, null); // null表示新增
                dialog.DataContext = viewModel;

                // 设置保存成功回调
                viewModel.SaveCompleteCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                };

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("患者添加成功", "成功");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"添加患者失败: {ex.Message}", "错误");
            }
        }

        protected override async Task EditAsync(PatientInfo item)
        {
            if (item == null) return;

            try
            {
                var dialog = new Views.PatientAddEditDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "编辑患者";

                // 创建ViewModel并设置为编辑模式
                var viewModel = new PatientAddEditDialogViewModel(_patientApiService, item);
                dialog.DataContext = viewModel;

                // 设置保存成功回调
                viewModel.SaveCompleteCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                };

                if (dialog.ShowDialog() == true)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync("患者编辑成功", "成功");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"编辑患者失败: {ex.Message}", "错误");
            }
        }

        protected override async Task DeleteAsync(PatientInfo item)
        {
            if (item == null) return;

            // 患者信息不支持删除，只能禁用
            await ToggleStatusAsync(item);
        }

        #endregion

        #region 额外方法

        /// <summary>
        /// 切换患者状态
        /// </summary>
        private async Task ToggleStatusAsync(PatientInfo patient)
        {
            if (patient == null) return;

            var action = patient.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要{action}患者 {patient.Name} 吗？",
                $"{action}患者");

            if (confirm)
            {
                var newStatus = patient.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
                var statusDto = new CommonStatusUpdateDto
                {
                    Status = newStatus,
                    Reason = $"手动{action}患者档案"
                };
                
                // 注意: 这里需要根据实际的服务方法调整
                ServiceResult result;
                if (patient.Status == CommonStatus.Enabled)
                {
                    result = await Service.DisableAsync(patient.Id);
                }
                else
                {
                    result = await Service.EnableAsync(patient.Id);
                }

                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync($"患者{action}成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"患者{action}失败",
                        "错误");
                }
            }
        }

        /// <summary>
        /// 查看患者详情
        /// </summary>
        private async Task ViewDetailsAsync(PatientInfo patient)
        {
            if (patient == null) return;

            try
            {
                // 获取患者详情
                var detailResult = await Service.GetByIdAsync(patient.Id);
                if (detailResult.IsSuccess)
                {
                    var detailInfo = $"姓名: {patient.Name}\n" +
                                   $"性别: {patient.GenderText}\n" +
                                   $"年龄: {patient.AgeDescription}\n" +
                                   $"电话: {patient.Phone ?? "未填写"}\n" +
                                   $"地址: {patient.Address ?? "未填写"}\n" +
                                   $"就诊次数: {patient.VisitCount}次\n" +
                                   $"最后就诊: {(patient.LastVisitTime?.ToString("yyyy-MM-dd HH:mm") ?? "从未就诊")}";

                    await _commonDialogService.ShowInformationAsync(detailInfo, $"患者详情 - {patient.Name}");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync("获取患者详情失败", "错误");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"查看患者详情失败: {ex.Message}", "错误");
            }
        }

        #endregion
    }
}