using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方打印处理器
/// 负责处方打印预览和DTO构建
/// </summary>
public class PrescriptionPrintHandler
{
    #region 字段

    private readonly IPrintService<PrescriptionPrintModel>? _printService;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IMedicalCaseRepository _repository;
    private readonly ISessionManager _sessionManager;
    private readonly IClinicSettingsService _clinicSettingsService;
    private readonly ILogger<PrescriptionPrintHandler> _logger;

    #endregion

    #region 构造函数

    public PrescriptionPrintHandler(
        IMedicalCaseService medicalCaseService,
        IMedicalCaseRepository repository,
        ISessionManager sessionManager,
        IClinicSettingsService clinicSettingsService,
        ILoggerFactory loggerFactory,
        IPrintService<PrescriptionPrintModel>? printService = null)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _clinicSettingsService = clinicSettingsService ?? throw new ArgumentNullException(nameof(clinicSettingsService));
        _logger = loggerFactory.CreateLogger<PrescriptionPrintHandler>();
        _printService = printService;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 执行处方打印预览
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="prescriptionProvider">处方数据提供者</param>
    /// <param name="currentPatient">当前患者信息</param>
    /// <param name="consultationData">诊断数据</param>
    /// <returns>打印结果</returns>
    public async Task<PrintResult> PrintPreviewAsync(
        Guid medicalCaseId,
        IDataProvider? prescriptionProvider,
        PatientDetailDto? currentPatient,
        ConsultationInputDto? consultationData)
    {
        try
        {
            _logger.LogInformation("预览处方笺，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            if (_printService == null)
            {
                return PrintResult.Failed("打印服务未配置");
            }

            // 获取处方数据（从缓存或Provider构建）
            var prescription = BuildPrescriptionDetailDto(medicalCaseId, prescriptionProvider);
            if (prescription == null)
            {
                return PrintResult.Failed("没有可打印的处方数据");
            }

            // CODE-24: 校验空处方 -- 处方存在但无药材项不允许打印
            if (prescription.Items == null || prescription.Items.Count == 0)
            {
                return PrintResult.Failed("处方无药材信息，无法打印");
            }

            // OpenSpec: create-printing-module - 组装PrescriptionPrintModel并调用新接口
            var printModel = BuildPrintModel(prescription, currentPatient, consultationData);
            await _printService.PreviewAsync(printModel);

            // T2-X8-04~08: 打印成功后回写状态到服务端
            await RecordPrintCompletedAsync(medicalCaseId, PrintType.Prescription);

            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印处方笺失败");
            return PrintResult.Failed($"打印失败: {ex.Message}");
        }
    }

    /// <summary>
    /// D1: 导出处方笺为 PDF 文件
    /// </summary>
    public async Task<PrintResult> ExportPdfAsync(
        Guid medicalCaseId,
        IDataProvider? prescriptionProvider,
        PatientDetailDto? currentPatient,
        ConsultationInputDto? consultationData)
    {
        try
        {
            _logger.LogInformation("导出处方笺PDF，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            if (_printService == null)
                return PrintResult.Failed("打印服务未配置");

            var prescription = BuildPrescriptionDetailDto(medicalCaseId, prescriptionProvider);
            if (prescription == null)
                return PrintResult.Failed("没有可导出的处方数据");

            if (prescription.Items == null || prescription.Items.Count == 0)
                return PrintResult.Failed("处方无药材信息，无法导出");

            var printModel = BuildPrintModel(prescription, currentPatient, consultationData);

            // 弹出保存对话框
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF 文件 (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                FileName = $"处方笺_{currentPatient?.Name}_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() != true)
                return PrintResult.Success(); // 用户取消，非错误

            await _printService.ExportAsync(printModel, dialog.FileName, Printing.Interfaces.ExportFormat.Pdf);

            _logger.LogInformation("PDF导出成功: {FilePath}", dialog.FileName);
            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出处方笺PDF失败");
            return PrintResult.Failed($"导出失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 构建打印数据模型
    /// OpenSpec: create-printing-module
    /// T4-S5-10: 自动绑定DoctorName从当前登录用户
    /// T4-S5-11: 包含Discount折扣计算
    /// </summary>
    private PrescriptionPrintModel BuildPrintModel(
        PrescriptionDetailDto prescription,
        PatientDetailDto? patient,
        ConsultationInputDto? consultation)
    {
        // T4-S5-10: 自动从当前登录用户获取医师姓名
        var doctorName = _sessionManager.CurrentUser?.RealName ?? string.Empty;

        // T4-S5-11: 获取折扣值（0-1之间，默认1.0无折扣）
        var discount = prescription.Discount > 0 ? prescription.Discount : 1.0m;

        // D2: 从配置服务读取诊所信息
        var clinicSettings = _clinicSettingsService.GetSettings();

        var model = new PrescriptionPrintModel
        {
            // D2: 诊所信息 (从 clinic-settings.json 热更新)
            ClinicName = clinicSettings.Name,
            Department = clinicSettings.Department,
            ClinicAddress = clinicSettings.Address,
            ClinicPhone = clinicSettings.Phone,

            // 患者信息
            PatientName = patient?.Name ?? string.Empty,
            Gender = patient?.Gender.ToString() ?? string.Empty,
            Age = CalculateAge(patient?.BirthDate),
            PatientPhone = patient?.PhoneNumber,
            PatientAddress = patient?.Address,

            // 诊断信息
            TcmDiagnosis = consultation?.TcmDiagnosis,
            PresentIllness = consultation?.PresentIllness,
            TongueDiagnosis = consultation?.TongueDiagnosis,
            PulseDiagnosis = consultation?.PulseDiagnosis,

            // 处方信息
            DosageCount = prescription.DosageCount,
            Usage = prescription.Usage ?? "水煎服，日1剂，1日2次",
            Advice = prescription.Advice,
            FormulaSource = prescription.ReferencedFormulas,

            // 费用信息
            SingleDosePrice = prescription.Items?.Sum(i => i.Dosage * i.UnitPrice) ?? 0,

            // T4-S5-11: 折扣
            Discount = discount,

            // T4-S5-10: 签名 - 自动绑定当前用户
            DoctorName = doctorName,
            PrescriptionDate = DateTime.Now,

            // D3: 草稿水印 -- 非 Completed 状态即为草稿
            IsDraft = _medicalCaseService.Current?.CaseStatus != MedicalCaseStatus.Completed
        };

        // T4-S5-11: 计算总价（含折扣）
        model.MedicineFee = model.SingleDosePrice * model.DosageCount;
        var subtotal = model.ConsultationFee + model.MedicineFee + model.TreatmentFee;
        model.TotalPrice = discount < 1.0m
            ? Math.Round(subtotal * discount, 0, MidpointRounding.AwayFromZero)
            : subtotal;

        // 药材列表
        if (prescription.Items != null)
        {
            var seq = 1;
            foreach (var item in prescription.Items)
            {
                model.Items.Add(new PrescriptionItemPrintModel
                {
                    SequenceNumber = seq++,
                    HerbName = item.HerbName,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    DecocteMethod = item.DecocteMethod
                });
            }
        }

        return model;
    }

    /// <summary>
    /// 计算年龄
    /// </summary>
    private static int CalculateAge(DateTime? birthDate)
    {
        if (birthDate == null) return 0;
        var today = DateTime.Today;
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value.Date > today.AddYears(-age)) age--;
        return age;
    }

    /// <summary>
    /// 打印完成后回写状态到服务端
    /// T2-X8-04~08: IsPrinted/PrintCount/LastPrintedAt/PrintVersion + PrintLog
    /// </summary>
    private async Task RecordPrintCompletedAsync(Guid medicalCaseId, PrintType printType)
    {
        try
        {
            var request = new PrintCompletedRequest { PrintType = printType };
            var result = await _repository.RecordPrintCompletedAsync(medicalCaseId, request);
            if (result != null)
            {
                _logger.LogInformation("打印回写成功，MedicalCaseId: {MedicalCaseId}, PrintCount: {PrintCount}",
                    medicalCaseId, result.PrintCount);
            }
            else
            {
                _logger.LogWarning("打印回写失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            }
        }
        catch (Exception ex)
        {
            // 打印回写失败不应阻止打印预览本身的成功
            _logger.LogError(ex, "打印回写异常，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }
    }

    /// <summary>
    /// 构建处方详情DTO
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="prescriptionProvider">处方数据提供者</param>
    /// <returns>处方详情DTO，如果无数据则返回null</returns>
    public PrescriptionDetailDto? BuildPrescriptionDetailDto(
        Guid medicalCaseId,
        IDataProvider? prescriptionProvider)
    {
        // 优先使用缓存的处方数据
        var cachedPrescription = _medicalCaseService.CachedPrescription;
        if (cachedPrescription != null)
        {
            return cachedPrescription;
        }

        // 如果没有缓存，从Provider构建
        var prescriptionData = prescriptionProvider?.GetPrescriptionData();
        if (prescriptionData == null || 
            !prescriptionData.NeedsPrescription || 
            prescriptionData.Items == null || 
            prescriptionData.Items.Count == 0)
        {
            return null;
        }

        // 转换药材项类型
        var items = prescriptionData.Items.Select(item => new PrescriptionItemDto
        {
            Id = item.Id ?? Guid.NewGuid(),
            HerbId = item.HerbId,
            HerbName = item.HerbName ?? string.Empty,
            Dosage = item.Dosage,
            Unit = item.Unit,
            UnitPrice = item.UnitPrice,
            DecocteMethod = item.DecocteMethod
        }).ToList();

        return new PrescriptionDetailDto
        {
            Id = prescriptionData.Id ?? Guid.NewGuid(),
            MedicalCaseId = medicalCaseId,
            DosageCount = prescriptionData.DosageCount,
            Usage = prescriptionData.Usage,
            Advice = prescriptionData.Advice,
            ReferencedFormulas = prescriptionData.ReferencedFormulas,
            Remark = prescriptionData.Remark,
            Discount = prescriptionData.Discount, // T4-S5-11: 传递折扣值
            Items = items
        };
    }

    #endregion
}

#region 结果类型

/// <summary>
/// 打印操作结果
/// </summary>
public class PrintResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }

    private PrintResult() { }

    public static PrintResult Success() => new() { IsSuccess = true };
    public static PrintResult Failed(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };
}

#endregion
