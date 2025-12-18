using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.ViewModels.Components; // Issue #1788: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 快速新建患者对话框视图模型
    /// Issue #1487: PatientSelectionDialog优化实现 - 新建患者功能
    /// </summary>
    public class QuickCreatePatientDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        // Issue #1788: 使用CommandHandler替代直接Repository访问
        private readonly PatientCommandHandler _commandHandler;

        #endregion

        #region 数据属性

        private string _name = string.Empty;
        private bool _isMale = true;
        private bool _isFemale;
        private DateTime? _birthDate;
        private string _phoneNumber = string.Empty;
        private string _pinyinCode = string.Empty;

        /// <summary>患者姓名（必填）</summary>
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>是否为男性</summary>
        public bool IsMale
        {
            get => _isMale;
            set
            {
                if (SetProperty(ref _isMale, value))
                {
                    if (value) IsFemale = false;
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>是否为女性</summary>
        public bool IsFemale
        {
            get => _isFemale;
            set
            {
                if (SetProperty(ref _isFemale, value))
                {
                    if (value) IsMale = false;
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>性别（根据IsMale/IsFemale计算）</summary>
        public Gender Gender => IsMale ? Gender.Male : (IsFemale ? Gender.Female : Gender.Unknown);

        /// <summary>
        /// 出生日期（必填）
        /// Issue #2240: 改为BirthDate，不再使用Age
        /// </summary>
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    RaisePropertyChanged(nameof(Age));
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 年龄（只读计算属性，从BirthDate计算）
        /// Issue #2240: Age改为计算属性，仅用于显示
        /// </summary>
        public int? Age
        {
            get
            {
                if (BirthDate.HasValue)
                {
                    var today = DateTime.Today;
                    var age = today.Year - BirthDate.Value.Year;
                    if (BirthDate.Value.Date > today.AddYears(-age))
                    {
                        age--;
                    }
                    return age;
                }
                return null;
            }
        }

        /// <summary>手机号码（必填，11位）</summary>
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>拼音码（自动生成，可编辑）</summary>
        public string PinyinCode
        {
            get => _pinyinCode;
            set => SetProperty(ref _pinyinCode, value);
        }

        #endregion

        #region 对话框属性

        public string Title => "新建患者";
        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 命令

        /// <summary>保存命令</summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>取消命令</summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public QuickCreatePatientDialogViewModel(
            PatientCommandHandler commandHandler, // Issue #1788: 注入CommandHandler
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager)
        {
            // Issue #1788: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);

            // 默认性别为男
            IsMale = true;
        }

        #endregion

        #region IDialogAware 实现

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 可以从参数中获取初始值（如果需要）
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 保存患者信息
        /// Issue #1788: 使用CommandHandler.CreatePatientAsync()
        /// Issue #2240: 直接使用BirthDate，不再从Age反算
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                // 验证表单
                if (!ValidateForm(out string errorMessage))
                {
                    _ = ShowErrorMessageAsync(errorMessage);
                    return;
                }

                SetIsBusy(true, "正在保存...");

                // 创建患者DTO
                // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端默认为Enabled
                var createDto = new PatientInputDto
                {
                    Name = Name.Trim(),
                    Gender = Gender,
                    BirthDate = BirthDate, // Issue #2240: 直接使用BirthDate
                    PhoneNumber = PhoneNumber.Trim()
                    // TODO: 拼音码功能待后续扩展（需要扩展PatientInputDto）
                };

                // Issue #1788: 使用CommandHandler创建患者
                var result = await _commandHandler.CreatePatientAsync(createDto);

                if (result.IsSuccess && result.Data != null)
                {
                    Logger.LogInformation("快速创建患者成功: {PatientName} (ID: {PatientId})", result.Data.Name, result.Data.Id);

                    // 通过对话框参数返回新创建的患者
                    var parameters = new DialogParameters
                    {
                        { "NewPatient", result.Data }
                    };

                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
                }
                else
                {
                    Logger.LogWarning("创建患者失败: {ErrorMessage}", result.ErrorMessage);
                    await ShowErrorMessageAsync($"保存失败：{result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建患者异常: {PatientName}", Name);
                await ShowErrorMessageAsync("保存失败，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            Logger.LogInformation("取消快速创建患者");
        }

        #endregion

        #region 验证逻辑

        /// <summary>
        /// 验证表单
        /// Issue #1487: 必填字段验证
        /// Issue #2240: 改为验证BirthDate而非Age
        /// </summary>
        private bool ValidateForm(out string errorMessage)
        {
            errorMessage = string.Empty;

            // 姓名验证
            if (string.IsNullOrWhiteSpace(Name))
            {
                errorMessage = "请输入患者姓名";
                return false;
            }

            if (Name.Trim().Length > 50)
            {
                errorMessage = "患者姓名不能超过50个字符";
                return false;
            }

            // 性别验证
            if (Gender == Gender.Unknown)
            {
                errorMessage = "请选择性别";
                return false;
            }

            // 出生日期验证（Issue #2240）
            if (!BirthDate.HasValue)
            {
                errorMessage = "请选择出生日期";
                return false;
            }

            // 验证出生日期合理性
            if (BirthDate.Value > DateTime.Today)
            {
                errorMessage = "出生日期不能是未来日期";
                return false;
            }

            if (BirthDate.Value < DateTime.Today.AddYears(-150))
            {
                errorMessage = "出生日期不能超过150年前";
                return false;
            }

            // 手机号验证（Issue #1487: 必填，11位）
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                errorMessage = "请输入手机号码";
                return false;
            }

            var phone = PhoneNumber.Trim();
            if (phone.Length != 11 || !phone.All(char.IsDigit))
            {
                errorMessage = "请输入正确的11位手机号码";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证保存命令是否可执行
        /// Issue #2240: 改为验证BirthDate而非Age
        /// </summary>
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   Gender != Gender.Unknown &&
                   BirthDate.HasValue &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !IsBusy;
        }

        /// <summary>
        /// 更新命令状态
        /// </summary>
        private void UpdateCommandStates()
        {
            SaveCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
