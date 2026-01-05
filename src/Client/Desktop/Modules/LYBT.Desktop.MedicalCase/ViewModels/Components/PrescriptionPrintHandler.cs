// OpenSpec: create-printing-module - 使用新的独立打印模块
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方打印处理器
/// 负责处方打印预览和DTO构建
/// OpenSpec: slim-medicalcase-viewmodel (Phase 2)
/// OpenSpec: create-printing-module - 更新为使用独立Printing模块
/// </summary>
public class PrescriptionPrintHandler
{
    #region 字段

    private readonly IPrintService<PrescriptionPrintModel>? _printService;
    private readonly MedicalCaseDataLoader _dataLoader;
    private readonly ILogger<PrescriptionPrintHandler> _logger;

    #endregion

    #region 构造函数

    public PrescriptionPrintHandler(
        MedicalCaseDataLoader dataLoader,
        ILoggerFactory loggerFactory,
        IPrintService<PrescriptionPrintModel>? printService = null)
    {
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
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

            // OpenSpec: create-printing-module - 组装PrescriptionPrintModel并调用新接口
            var printModel = BuildPrintModel(prescription, currentPatient, consultationData);
            await _printService.PreviewAsync(printModel);

            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印处方笺失败");
            return PrintResult.Failed($"打印失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 构建打印数据模型
    /// OpenSpec: create-printing-module
    /// </summary>
    private static PrescriptionPrintModel BuildPrintModel(
        PrescriptionDetailDto prescription,
        PatientDetailDto? patient,
        ConsultationInputDto? consultation)
    {
        var model = new PrescriptionPrintModel
        {
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

            // 签名
            PrescriptionDate = DateTime.Now
        };

        // 计算总价
        model.MedicineFee = model.SingleDosePrice * model.DosageCount;
        model.TotalPrice = model.ConsultationFee + model.MedicineFee + model.TreatmentFee;

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
        var cachedPrescription = _dataLoader.GetCachedPrescription();
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
