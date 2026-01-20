using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Clinical.Handlers;

/// <summary>
/// 读卡器工作台处理器
/// 负责读卡器的初始化、读卡操作、患者匹配等业务逻辑
/// OpenSpec: integrate-cardreader-module - 从ViewModel提取读卡器相关逻辑
/// </summary>
public class CardReaderWorkspaceHandler : IDisposable
{
    #region 字段

    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientIntegration;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILogger<CardReaderWorkspaceHandler> _logger;

    private bool _isInitialized;
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    /// 是否已连接读卡器
    /// </summary>
    public bool IsConnected => _cardReaderService.IsConnected;

    /// <summary>
    /// 是否启用自动读卡
    /// </summary>
    public bool IsAutoReadEnabled => _cardReaderService.IsAutoReadEnabled;

    /// <summary>
    /// 是否正在读卡
    /// </summary>
    public bool IsReading { get; private set; }

    /// <summary>
    /// 状态信息
    /// </summary>
    public string StatusMessage { get; private set; } = "读卡器未连接";

    #endregion

    #region 回调属性

    /// <summary>
    /// 设置忙碌状态的回调
    /// </summary>
    public Action<bool, string?>? SetBusy { get; set; }

    /// <summary>
    /// 显示错误消息的回调
    /// </summary>
    public Func<string, Task>? ShowErrorMessage { get; set; }

    /// <summary>
    /// 显示成功消息的回调
    /// </summary>
    public Func<string, Task>? ShowSuccessMessage { get; set; }

    /// <summary>
    /// 获取弹窗服务的回调
    /// </summary>
    public Func<ICommonDialogService?>? GetCommonDialogService { get; set; }

    /// <summary>
    /// 属性变更通知的回调
    /// </summary>
    public Action<string>? OnPropertyChanged { get; set; }

    /// <summary>
    /// 读卡成功且找到/创建患者后的回调
    /// 参数：(PatientFromCardResult patient, CardReadResult cardResult)
    /// </summary>
    public Func<PatientFromCardResult, CardReadResult, Task>? OnPatientReadyForMedicalCase { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    public CardReaderWorkspaceHandler(
        ICardReaderService cardReaderService,
        IPatientCardReaderIntegration patientIntegration,
        IMedicalCaseService medicalCaseService,
        INavigationCoordinator navigationCoordinator,
        ILoggerFactory loggerFactory)
    {
        _cardReaderService = cardReaderService ?? throw new ArgumentNullException(nameof(cardReaderService));
        _patientIntegration = patientIntegration ?? throw new ArgumentNullException(nameof(patientIntegration));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _logger = loggerFactory.CreateLogger<CardReaderWorkspaceHandler>();

        // 订阅读卡器事件
        _cardReaderService.ConnectionStateChanged += OnConnectionStateChanged;
        _cardReaderService.CardReadCompleted += OnCardReadCompleted;
        _cardReaderService.CardReadError += OnCardReadError;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化读卡器
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _logger.LogInformation("开始初始化读卡器...");
            UpdateStatus("正在连接读卡器...");

            var success = await _cardReaderService.InitializeAsync();
            _isInitialized = true;

            if (success)
            {
                _logger.LogInformation("读卡器初始化成功");
                UpdateStatus("读卡器已就绪");
            }
            else
            {
                _logger.LogWarning("读卡器初始化失败，可能未安装读卡器");
                UpdateStatus("读卡器未连接");
            }

            NotifyPropertyChanged(nameof(IsConnected));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化读卡器时发生异常");
            UpdateStatus("读卡器初始化失败");
        }
    }

    /// <summary>
    /// 手动读卡
    /// </summary>
    public async Task ManualReadCardAsync()
    {
        if (!IsConnected)
        {
            if (ShowErrorMessage != null)
                await ShowErrorMessage("读卡器未连接");
            return;
        }

        if (IsReading)
        {
            _logger.LogDebug("正在读卡中，忽略重复请求");
            return;
        }

        try
        {
            IsReading = true;
            NotifyPropertyChanged(nameof(IsReading));
            UpdateStatus("正在读卡...");
            SetBusy?.Invoke(true, "正在读取身份证...");

            var result = await _cardReaderService.ReadCardAsync();
            await HandleCardReadResultAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动读卡时发生异常");
            UpdateStatus("读卡失败");
            if (ShowErrorMessage != null)
                await ShowErrorMessage("读卡失败，请重试");
        }
        finally
        {
            IsReading = false;
            NotifyPropertyChanged(nameof(IsReading));
            SetBusy?.Invoke(false, null);
        }
    }

    /// <summary>
    /// 切换自动读卡模式
    /// </summary>
    public void ToggleAutoRead()
    {
        if (!IsConnected)
        {
            _logger.LogDebug("读卡器未连接，无法切换自动读卡");
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

    /// <summary>
    /// 启动自动读卡
    /// </summary>
    public void StartAutoRead()
    {
        if (!IsConnected) return;

        _logger.LogInformation("启动自动读卡模式");
        _cardReaderService.StartAutoRead(500);
        UpdateStatus("自动读卡已启用");
        NotifyPropertyChanged(nameof(IsAutoReadEnabled));
    }

    /// <summary>
    /// 停止自动读卡
    /// </summary>
    public void StopAutoRead()
    {
        _logger.LogInformation("停止自动读卡模式");
        _cardReaderService.StopAutoRead();
        UpdateStatus(IsConnected ? "读卡器已就绪" : "读卡器未连接");
        NotifyPropertyChanged(nameof(IsAutoReadEnabled));
    }

    /// <summary>
    /// 断开读卡器连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            StopAutoRead();
            await _cardReaderService.DisconnectAsync();
            UpdateStatus("读卡器已断开");
            NotifyPropertyChanged(nameof(IsConnected));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "断开读卡器时发生异常");
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 处理读卡结果
    /// </summary>
    private async Task HandleCardReadResultAsync(CardReadResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogWarning("读卡失败：{ErrorMessage}", result.ErrorMessage);
            UpdateStatus($"读卡失败：{result.ErrorMessage}");
            if (ShowErrorMessage != null)
                await ShowErrorMessage($"读卡失败：{result.ErrorMessage}");
            return;
        }

        _logger.LogInformation("读卡成功：{Name}，身份证号：{IdNumber}",
            result.Name, MaskIdNumber(result.IdNumber));

        // 查找或创建患者
        await ProcessPatientFromCardAsync(result);
    }

    /// <summary>
    /// 根据读卡结果处理患者
    /// </summary>
    private async Task ProcessPatientFromCardAsync(CardReadResult cardResult)
    {
        try
        {
            SetBusy?.Invoke(true, "正在查找患者...");
            UpdateStatus($"正在查找患者：{cardResult.Name}");

            // 先查找是否存在
            var existingPatient = await _patientIntegration.FindPatientByIdNumberAsync(cardResult.IdNumber);

            if (existingPatient != null)
            {
                // 找到现有患者
                _logger.LogInformation("找到现有患者：{PatientId}, {Name}",
                    existingPatient.PatientId, existingPatient.Name);

                UpdateStatus($"找到患者：{existingPatient.Name}");

                if (ShowSuccessMessage != null)
                {
                    var visitInfo = existingPatient.VisitCount > 0
                        ? $"，已就诊{existingPatient.VisitCount}次"
                        : "，首次就诊";
                    await ShowSuccessMessage($"找到患者：{existingPatient.Name}{visitInfo}");
                }

                // 触发回调
                if (OnPatientReadyForMedicalCase != null)
                    await OnPatientReadyForMedicalCase(existingPatient, cardResult);
            }
            else
            {
                // 未找到患者，询问是否创建
                await HandleNewPatientFromCardAsync(cardResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理患者信息时发生异常");
            UpdateStatus("处理患者信息失败");
            if (ShowErrorMessage != null)
                await ShowErrorMessage("处理患者信息失败，请重试");
        }
        finally
        {
            SetBusy?.Invoke(false, null);
        }
    }

    /// <summary>
    /// 处理新患者（从读卡结果创建）
    /// </summary>
    private async Task HandleNewPatientFromCardAsync(CardReadResult cardResult)
    {
        var dialogService = GetCommonDialogService?.Invoke();
        if (dialogService == null)
        {
            _logger.LogWarning("CommonDialogService为空，无法显示弹窗");
            return;
        }

        // 询问是否创建新患者
        var message = $"未找到患者记录：{cardResult.Name}\n" +
                     $"身份证号：{MaskIdNumber(cardResult.IdNumber)}\n\n" +
                     "是否创建新患者档案？\n" +
                     "注意：身份证不含电话号码，创建后需补充联系方式。";

        var confirmed = await dialogService.ShowConfirmAsync(message, "创建新患者");

        if (confirmed)
        {
            try
            {
                SetBusy?.Invoke(true, "正在创建患者...");
                UpdateStatus("正在创建患者...");

                var patientResult = await _patientIntegration.FindOrCreatePatientAsync(cardResult);

                _logger.LogInformation("患者创建成功：{PatientId}, {Name}",
                    patientResult.PatientId, patientResult.Name);

                UpdateStatus($"患者已创建：{patientResult.Name}");

                if (ShowSuccessMessage != null)
                    await ShowSuccessMessage($"患者 {patientResult.Name} 创建成功");

                // 触发回调
                if (OnPatientReadyForMedicalCase != null)
                    await OnPatientReadyForMedicalCase(patientResult, cardResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者失败");
                UpdateStatus("创建患者失败");
                if (ShowErrorMessage != null)
                    await ShowErrorMessage("创建患者失败，请重试");
            }
            finally
            {
                SetBusy?.Invoke(false, null);
            }
        }
        else
        {
            UpdateStatus("已取消创建患者");
        }
    }

    /// <summary>
    /// 更新状态信息
    /// </summary>
    private void UpdateStatus(string message)
    {
        StatusMessage = message;
        NotifyPropertyChanged(nameof(StatusMessage));
    }

    /// <summary>
    /// 通知属性变更
    /// </summary>
    private void NotifyPropertyChanged(string propertyName)
    {
        OnPropertyChanged?.Invoke(propertyName);
    }

    /// <summary>
    /// 掩码身份证号（保护隐私）
    /// </summary>
    private static string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return idNumber;

        return idNumber[..6] + "****" + idNumber[^4..];
    }

    #endregion

    #region 事件处理

    private void OnConnectionStateChanged(object? sender, CardReaderConnectionEventArgs e)
    {
        _logger.LogInformation("读卡器连接状态变化：{IsConnected}", e.IsConnected);
        UpdateStatus(e.IsConnected ? "读卡器已就绪" : "读卡器已断开");
        NotifyPropertyChanged(nameof(IsConnected));
    }

    private async void OnCardReadCompleted(object? sender, CardReadResult e)
    {
        _logger.LogInformation("自动读卡完成：{Name}", e.Name);
        await HandleCardReadResultAsync(e);
    }

    private void OnCardReadError(object? sender, CardReadErrorEventArgs e)
    {
        _logger.LogWarning("读卡错误：{ErrorCode} - {Message}", e.ErrorCode, e.Message);
        UpdateStatus($"读卡错误：{e.Message}");
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // 取消订阅事件
            _cardReaderService.ConnectionStateChanged -= OnConnectionStateChanged;
            _cardReaderService.CardReadCompleted -= OnCardReadCompleted;
            _cardReaderService.CardReadError -= OnCardReadError;

            // 停止自动读卡
            if (IsAutoReadEnabled)
            {
                _cardReaderService.StopAutoRead();
            }
        }

        _disposed = true;
    }

    #endregion
}
