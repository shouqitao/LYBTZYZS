using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Clinical.ViewModels.Workspace;

/// <summary>
/// Child ViewModel for card reader operations.
/// Replaces CardReaderWorkspaceHandler's callback-based design with Composite VM pattern.
/// </summary>
public class CardReaderViewModel : ChildViewModelBase
{
    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientIntegration;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IMedicalCaseWorkspaceContext _context;

    private bool _isInitialized;
    private bool _disposed;
    private bool _isReading;
    private string _statusMessage = "读卡器未连接";

    #region Properties

    /// <summary>
    /// Whether the card reader is connected (delegates to service).
    /// </summary>
    public bool IsConnected => _cardReaderService.IsConnected;

    /// <summary>
    /// Whether auto-read mode is enabled (delegates to service).
    /// </summary>
    public bool IsAutoReadEnabled => _cardReaderService.IsAutoReadEnabled;

    /// <summary>
    /// Whether a card read operation is in progress.
    /// </summary>
    public bool IsReading
    {
        get => _isReading;
        private set => SetProperty(ref _isReading, value);
    }

    /// <summary>
    /// Current status message for the card reader.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    #endregion

    #region Commands

    public DelegateCommand ReadCardCommand { get; }
    public DelegateCommand ToggleAutoReadCommand { get; }

    #endregion

    #region Constructor

    public CardReaderViewModel(
        ICardReaderService cardReaderService,
        IPatientCardReaderIntegration patientIntegration,
        IMedicalCaseService medicalCaseService,
        INavigationCoordinator navigationCoordinator,
        IMedicalCaseWorkspaceContext context,
        IWorkspaceHost host,
        ILoggerFactory loggerFactory)
        : base(host, loggerFactory)
    {
        _cardReaderService = cardReaderService ?? throw new ArgumentNullException(nameof(cardReaderService));
        _patientIntegration = patientIntegration ?? throw new ArgumentNullException(nameof(patientIntegration));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _context = context ?? throw new ArgumentNullException(nameof(context));

        ReadCardCommand = new DelegateCommand(async () => await ManualReadCardAsync());
        ToggleAutoReadCommand = new DelegateCommand(ToggleAutoRead);

        // Subscribe to card reader events
        _cardReaderService.ConnectionStateChanged += OnConnectionStateChanged;
        _cardReaderService.CardReadCompleted += OnCardReadCompleted;
        _cardReaderService.CardReadError += OnCardReadError;
    }

    #endregion

    #region Public Methods

    public override async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            Logger.LogInformation("开始初始化读卡器...");
            UpdateStatus("正在连接读卡器...");

            var success = await _cardReaderService.InitializeAsync();
            _isInitialized = true;

            if (success)
            {
                Logger.LogInformation("读卡器初始化成功");
                UpdateStatus("读卡器已就绪");
            }
            else
            {
                Logger.LogWarning("读卡器初始化失败，可能未安装读卡器");
                UpdateStatus("读卡器未连接");
            }

            OnPropertyChanged(nameof(IsConnected));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "初始化读卡器时发生异常");
            UpdateStatus("读卡器初始化失败");
        }
    }

    public async Task ManualReadCardAsync()
    {
        if (!IsConnected)
        {
            await Host.ShowErrorAsync("读卡器未连接");
            return;
        }

        if (IsReading)
        {
            Logger.LogDebug("正在读卡中，忽略重复请求");
            return;
        }

        try
        {
            IsReading = true;
            UpdateStatus("正在读卡...");
            Host.SetBusy(true, "正在读取身份证...");

            var result = await _cardReaderService.ReadCardAsync();
            await HandleCardReadResultAsync(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "手动读卡时发生异常");
            UpdateStatus("读卡失败");
            await Host.ShowErrorAsync("读卡失败，请重试");
        }
        finally
        {
            IsReading = false;
            Host.SetBusy(false, null);
        }
    }

    public void ToggleAutoRead()
    {
        if (!IsConnected)
        {
            Logger.LogDebug("读卡器未连接，无法切换自动读卡");
            return;
        }

        if (IsAutoReadEnabled)
        {
            StopAutoRead();
        }
        else
        {
            StartAutoRead();
        }
    }

    public void StartAutoRead()
    {
        if (!IsConnected) return;

        Logger.LogInformation("启动自动读卡模式");
        _cardReaderService.StartAutoRead(500);
        UpdateStatus("自动读卡已启用");
        OnPropertyChanged(nameof(IsAutoReadEnabled));
    }

    public void StopAutoRead()
    {
        Logger.LogInformation("停止自动读卡模式");
        _cardReaderService.StopAutoRead();
        UpdateStatus(IsConnected ? "读卡器已就绪" : "读卡器未连接");
        OnPropertyChanged(nameof(IsAutoReadEnabled));
    }

    public async Task DisconnectAsync()
    {
        try
        {
            StopAutoRead();
            await _cardReaderService.DisconnectAsync();
            UpdateStatus("读卡器已断开");
            OnPropertyChanged(nameof(IsConnected));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "断开读卡器时发生异常");
        }
    }

    #endregion

    #region Private Methods

    private async Task HandleCardReadResultAsync(CardReadResult result)
    {
        if (!result.IsSuccess)
        {
            Logger.LogWarning("读卡失败：{ErrorMessage}", result.ErrorMessage);
            UpdateStatus($"读卡失败：{result.ErrorMessage}");
            await Host.ShowErrorAsync($"读卡失败：{result.ErrorMessage}");
            return;
        }

        Logger.LogInformation("读卡成功：{Name}，身份证号：{IdNumber}",
            result.Name, MaskIdNumber(result.IdNumber));

        await ProcessPatientFromCardAsync(result);
    }

    private async Task ProcessPatientFromCardAsync(CardReadResult cardResult)
    {
        try
        {
            Host.SetBusy(true, "正在查找患者...");
            UpdateStatus($"正在查找患者：{cardResult.Name}");

            var existingPatient = await _patientIntegration.FindPatientByIdNumberAsync(cardResult.IdNumber);

            if (existingPatient != null)
            {
                Logger.LogInformation("找到现有患者：{PatientId}, {Name}",
                    existingPatient.PatientId, existingPatient.Name);

                UpdateStatus($"找到患者：{existingPatient.Name}");

                var visitInfo = existingPatient.VisitCount > 0
                    ? $"，已就诊{existingPatient.VisitCount}次"
                    : "，首次就诊";
                await Host.ShowSuccessAsync($"找到患者：{existingPatient.Name}{visitInfo}");

                await NavigateToMedicalCaseForPatientAsync(existingPatient);
            }
            else
            {
                await HandleNewPatientFromCardAsync(cardResult);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理患者信息时发生异常");
            UpdateStatus("处理患者信息失败");
            await Host.ShowErrorAsync("处理患者信息失败，请重试");
        }
        finally
        {
            Host.SetBusy(false, null);
        }
    }

    private async Task HandleNewPatientFromCardAsync(CardReadResult cardResult)
    {
        var dialogService = Host.CommonDialogService;
        if (dialogService == null)
        {
            Logger.LogWarning("CommonDialogService为空，无法显示弹窗");
            return;
        }

        var message = $"未找到患者记录：{cardResult.Name}\n" +
                     $"身份证号：{MaskIdNumber(cardResult.IdNumber)}\n\n" +
                     "是否创建新患者档案？\n" +
                     "注意：身份证不含电话号码，创建后需补充联系方式。";

        var confirmed = await dialogService.ShowConfirmAsync(message, "创建新患者");

        if (confirmed)
        {
            try
            {
                Host.SetBusy(true, "正在创建患者...");
                UpdateStatus("正在创建患者...");

                var patientResult = await _patientIntegration.FindOrCreatePatientAsync(cardResult);

                Logger.LogInformation("患者创建成功：{PatientId}, {Name}",
                    patientResult.PatientId, patientResult.Name);

                UpdateStatus($"患者已创建：{patientResult.Name}");
                await Host.ShowSuccessAsync($"患者 {patientResult.Name} 创建成功");

                await NavigateToMedicalCaseForPatientAsync(patientResult);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建患者失败");
                UpdateStatus("创建患者失败");
                await Host.ShowErrorAsync("创建患者失败，请重试");
            }
            finally
            {
                Host.SetBusy(false, null);
            }
        }
        else
        {
            UpdateStatus("已取消创建患者");
        }
    }

    private async Task NavigateToMedicalCaseForPatientAsync(PatientFromCardResult patient)
    {
        try
        {
            Host.SetBusy(true, "正在准备就诊...");
            Logger.LogInformation("[CardReader] 患者就绪: {PatientId}, {Name}, IsNew={IsNew}",
                patient.PatientId, patient.Name, patient.IsNewlyCreated);

            var patientDetail = await _patientIntegration.GetPatientDetailByIdAsync(patient.PatientId);
            if (patientDetail == null)
            {
                Logger.LogWarning("[CardReader] 获取患者详情失败: {PatientId}", patient.PatientId);
                await Host.ShowErrorAsync("获取患者信息失败，请重试");
                return;
            }

            var currentDoctorId = _context.SessionManager?.CurrentUser?.Id ?? Guid.Empty;
            var existingCase = await _medicalCaseService.GetUnfinishedCaseByPatientIdAsync(patient.PatientId, currentDoctorId);

            if (existingCase != null)
            {
                Logger.LogInformation("[CardReader] 找到未完成医案: {MedicalCaseId}", existingCase.Id);
                await Host.ShowSuccessAsync("找到未完成医案，正在加载...");
                NavigateToWorkspace(existingCase.Id, patientDetail);
            }
            else
            {
                var createResult = await _medicalCaseService.CreateMedicalCaseAsync(patient.PatientId);
                if (!createResult.success)
                {
                    Logger.LogWarning("[CardReader] 创建医案失败: {Error}", createResult.errorMessage);
                    await Host.ShowErrorAsync("创建医案失败: " + createResult.errorMessage);
                    return;
                }

                Logger.LogInformation("[CardReader] 医案创建成功: {MedicalCaseId}", createResult.medicalCaseId);
                await Host.ShowSuccessAsync("医案创建成功，正在打开...");
                NavigateToWorkspace(createResult.medicalCaseId, patientDetail);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CardReader] 处理患者就绪事件失败");
            await Host.ShowErrorAsync("处理患者信息失败，请重试");
        }
        finally
        {
            Host.SetBusy(false, null);
        }
    }

    private void NavigateToWorkspace(Guid medicalCaseId, PatientDetailDto patientDetail)
    {
        var parameters = new Dictionary<string, object>
        {
            { "MedicalCaseId", medicalCaseId },
            { "CurrentPatient", patientDetail },
            { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
            { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
        };
        _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
    }

    private void UpdateStatus(string message)
    {
        StatusMessage = message;
    }

    /// <summary>
    /// Mask ID number for privacy (keep first 6 and last 4 digits).
    /// </summary>
    public static string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return idNumber;

        return idNumber[..6] + "****" + idNumber[^4..];
    }

    #endregion

    #region Event Handlers

    private void OnConnectionStateChanged(object? sender, CardReaderConnectionEventArgs e)
    {
        Logger.LogInformation("读卡器连接状态变化：{IsConnected}", e.IsConnected);
        UpdateStatus(e.IsConnected ? "读卡器已就绪" : "读卡器已断开");
        OnPropertyChanged(nameof(IsConnected));
    }

    private void OnCardReadCompleted(object? sender, CardReadResult e)
    {
        Logger.LogInformation("自动读卡完成：{Name}", e.Name);
        _ = HandleCardReadResultAsync(e);
    }

    private void OnCardReadError(object? sender, CardReadErrorEventArgs e)
    {
        Logger.LogWarning("读卡错误：{ErrorCode} - {Message}", e.ErrorCode, e.Message);
        UpdateStatus($"读卡错误：{e.Message}");
    }

    #endregion

    #region IDisposable

    public override void Dispose()
    {
        if (_disposed) return;

        _cardReaderService.ConnectionStateChanged -= OnConnectionStateChanged;
        _cardReaderService.CardReadCompleted -= OnCardReadCompleted;
        _cardReaderService.CardReadError -= OnCardReadError;

        if (IsAutoReadEnabled)
        {
            _cardReaderService.StopAutoRead();
        }

        _disposed = true;
        base.Dispose();
    }

    #endregion
}
