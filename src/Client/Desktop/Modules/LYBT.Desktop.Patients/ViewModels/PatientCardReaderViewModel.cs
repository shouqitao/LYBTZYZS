using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 患者读卡器功能 ViewModel (Child VM)
/// 从 PatientMasterDetailViewModel 拆分出来的读卡器相关功能
/// </summary>
public partial class PatientCardReaderViewModel : CoreViewModelBase
{
    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientCardReaderIntegration;
    private readonly ICommonDialogService _dialogService;
    private readonly ILogger<PatientCardReaderViewModel> _logger;

    public PatientCardReaderViewModel(
        IViewModelServices viewModelServices,
        ICardReaderService cardReaderService,
        IPatientCardReaderIntegration patientCardReaderIntegration,
        ILogger<PatientCardReaderViewModel> logger) : base(viewModelServices)
    {
        _cardReaderService = cardReaderService ?? throw new ArgumentNullException(nameof(cardReaderService));
        _patientCardReaderIntegration = patientCardReaderIntegration ?? throw new ArgumentNullException(nameof(patientCardReaderIntegration));
        _dialogService = viewModelServices.CommonDialogService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>是否已连接读卡器</summary>
    public bool IsCardReaderConnected => _cardReaderService.IsConnected;

    /// <summary>是否正在读卡</summary>
    private bool _isReadingCard;
    public bool IsReadingCard
    {
        get => _isReadingCard;
        private set => SetProperty(ref _isReadingCard, value);
    }

    /// <summary>刷卡录入命令</summary>
    [RelayCommand(CanExecute = nameof(CanReadCard))]
    public async Task<CardReadResult?> ReadCardAsync()
    {
        if (!_cardReaderService.IsConnected)
        {
            var initialized = await _cardReaderService.InitializeAsync();
            if (!initialized)
            {
                await _dialogService.ShowErrorAsync("读卡器未连接，请检查设备", "读卡器未连接");
                return null;
            }
        }

        try
        {
            IsReadingCard = true;
            var result = await _cardReaderService.ReadCardAsync();

            if (!result.IsSuccess)
            {
                await _dialogService.ShowErrorAsync($"读卡失败：{result.ErrorMessage}", "读卡失败");
                return null;
            }

            _logger.LogInformation("读卡成功：{Name}", result.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读卡时发生异常");
            await _dialogService.ShowErrorAsync("读卡失败，请重试", "读卡失败");
            return null;
        }
        finally
        {
            IsReadingCard = false;
        }
    }

    private bool CanReadCard() => !IsReadingCard;

    /// <summary>根据身份证号查找患者</summary>
    public async Task<PatientFromCardResult?> FindPatientByIdNumberAsync(string idNumber)
    {
        return await _patientCardReaderIntegration.FindPatientByIdNumberAsync(idNumber);
    }

    /// <summary>根据读卡结果查找或创建患者</summary>
    public async Task<PatientFromCardResult> FindOrCreatePatientAsync(CardReadResult cardResult)
    {
        return await _patientCardReaderIntegration.FindOrCreatePatientAsync(cardResult);
    }

    /// <summary>掩码身份证号（保护隐私）</summary>
    public static string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return idNumber;
        return idNumber[..6] + "****" + idNumber[^4..];
    }
}
