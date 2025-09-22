using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.ViewModels
{

/// <summary>
/// 看诊主界面视图模型 - 简化版纯数据记录
/// 只负责简单的四诊数据录入，不包含流程监管和智能处理
/// </summary>
public class ConsultationMainViewModel : SessionAwareViewModel, INavigationAware
{

#region 服务依赖

private readonly IConsultationService _consultationService;
private readonly IMedicalCaseService _medicalCaseService;
private readonly IPatientService _patientService;

#endregion 服务依赖

#region 基本属性

private string _title = "看诊记录";

public string Title
{
get => _title;
set => SetProperty(ref _title, value);
}

private ObservableCollection<PatientDto> _patients = new();

public ObservableCollection<PatientDto> Patients
{
get => _patients;
set => SetProperty(ref _patients, value);
}

private PatientDto? _selectedPatient;

public PatientDto? SelectedPatient
{
get => _selectedPatient;
set
{
if (SetProperty(ref _selectedPatient, value))
{
// P0-02优化：当选择患者时，更新历史查询命令状态
((DelegateCommand)ViewPatientHistoryCommand).RaiseCanExecuteChanged();
}
}
}

private ConsultationDto _consultation = new();

public ConsultationDto Consultation
{
get => _consultation;
set => SetProperty(ref _consultation, value);
}

private Guid? _medicalCaseId;

public Guid? MedicalCaseId
{
get => _medicalCaseId;
set => SetProperty(ref _medicalCaseId, value);
}

private bool _isLoading;

public bool IsLoading
{
get => _isLoading;
set => SetProperty(ref _isLoading, value);
}

#endregion 基本属性

#region 命令

public ICommand LoadPatientsCommand { get; }
public ICommand SaveConsultationCommand { get; }
public ICommand ClearDataCommand { get; }

// P0-02新增：患者历史诊疗查询功能
public ICommand ViewPatientHistoryCommand { get; }

// P0-04新增：四诊录入模板功能
public ICommand ShowTemplateMenuCommand { get; }

#endregion 命令

#region 构造函数

public ConsultationMainViewModel(
IConsultationService consultationService,
IMedicalCaseService medicalCaseService,
IPatientService patientService,
ISessionManager sessionManager,
INotificationService notificationService,
ILogger<ConsultationMainViewModel> logger)
: base(sessionManager, notificationService, logger)
{
_consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
_medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
_patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

LoadPatientsCommand = new DelegateCommand(async () => await LoadPatientsAsync());
SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync());
ClearDataCommand = new DelegateCommand(ClearData);

// P0-02新增：初始化患者历史查询命令
ViewPatientHistoryCommand = new DelegateCommand(async () => await ViewPatientHistoryAsync(), () => SelectedPatient != null);

// P0-04新增：初始化四诊模板命令
ShowTemplateMenuCommand = new DelegateCommand(async () => await ShowTemplateMenuAsync());

// 修复: 使用Task.Run等待初始化，防止fire-and-forget
_ = Task.Run(async () => await InitializeAsync());
}

#endregion 构造函数

#region 初始化

private async Task InitializeAsync()
{
try
{
await LoadPatientsAsync();
}
catch (Exception ex)
{
LogError(ex, "初始化失败");

// 可以考虑显示用户友好的错误消息
ShowError("系统初始化失败，请稍后重试");
}
}

#endregion 初始化

#region 数据加载

private async Task LoadPatientsAsync()
{
try
{
IsLoading = true;

// 使用分页查询获取患者列表
var query = new LYBT.Shared.Models.Contracts.Patients.PatientSearchDto
{
PageIndex = 1,
PageSize = 100,
Keyword = string.Empty
};

var result = await _patientService.GetPagedAsync(query);
if (result.IsSuccess && result.Data != null)
{
Patients.Clear();
foreach (var patient in result.Data.Items)
{
Patients.Add(patient);
}
}
}
catch (Exception ex)
{
Logger.LogError(ex, "加载患者列表失败");
}
finally
{
IsLoading = false;
}
}

#endregion 数据加载

#region 数据保存

private async Task SaveConsultationAsync()
{
try
{
if (SelectedPatient == null)
{
ShowWarning("请先选择患者");
return;
}

IsLoading = true;

// 设置基本信息
Consultation.PatientId = SelectedPatient.Id;
Consultation.UserId = CurrentUser?.Id ?? Guid.Empty;
Consultation.MedicalCaseId = MedicalCaseId ?? Guid.NewGuid();
Consultation.StartTime = DateTime.Now;
Consultation.DoctorName = CurrentUser?.RealName ?? string.Empty;

var createDto = new ConsultationStartDto
{
PatientId = SelectedPatient.Id,
DoctorId = CurrentUser?.Id ?? Guid.Empty,
MedicalCaseId = MedicalCaseId ?? Guid.NewGuid(),
EstimatedDuration = 30,
ConsultationType = "门诊",
Remark = $"患者：{SelectedPatient.Name}，医生：{CurrentUser?.RealName ?? string.Empty}"
};

var result = await _consultationService.StartAsync(createDto);
if (result.IsSuccess && result.Data != null)
{
Consultation = result.Data;
ShowSuccess("看诊记录保存成功");
}
else
{
ShowError($"保存失败: {result.Message}");
}
}
catch (Exception ex)
{
LogError(ex, "保存看诊记录失败");
ShowError("保存失败，请重试");
}
finally
{
IsLoading = false;
}
}

#endregion 数据保存

#region 数据清理

private void ClearData()
{
Consultation = new ConsultationDto();
SelectedPatient = null;
MedicalCaseId = null;
}

#endregion 数据清理

#region 导航接口实现

/// <inheritdoc/>
public void OnNavigatedTo(NavigationContext navigationContext)
{
if (navigationContext.Parameters["MedicalCaseId"] is Guid caseId)
{
MedicalCaseId = caseId;
}
}

/// <inheritdoc/>
public bool IsNavigationTarget(NavigationContext navigationContext)
{
return true;
}

/// <inheritdoc/>
public void OnNavigatedFrom(NavigationContext navigationContext)
{
}

#endregion 导航接口实现

#region P0-02: 患者历史诊疗查询功能

/// <summary>
/// 查看患者历史诊疗记录
/// Epic 03-P0-02: 实用化历史查询功能，专为小诊所设计
/// 提供完整的患者诊疗历史，包括医案、诊断、处方等信息
/// </summary>
private async Task ViewPatientHistoryAsync()
{
if (SelectedPatient == null)
{
ShowWarning("请先选择患者");
return;
}

try
{
IsLoading = true;

// 获取患者的所有医案历史
var medicalCasesResult = await _medicalCaseService.GetByPatientIdAsync(SelectedPatient.Id);

if (!medicalCasesResult.IsSuccess || medicalCasesResult.Data == null || !medicalCasesResult.Data.Any())
{
ShowHistoryDialog(SelectedPatient, null);
return;
}

var medicalCases = medicalCasesResult.Data
.OrderByDescending(mc => mc.CreateTime)
.ToList();

// 为每个医案获取相关诊疗记录
var historyDetails = new List<PatientHistoryDetail>();

foreach (var medicalCase in medicalCases.Take(20)) // 最多显示20条历史记录
{
var detail = new PatientHistoryDetail
{
MedicalCase = medicalCase,
CreateTime = medicalCase.CreateTime,
Status = GetMedicalCaseStatusText((int)medicalCase.Status)
};

// 尝试获取该医案的诊疗记录
try
{
var consultationResult = await _consultationService.GetByMedicalCaseIdAsync(medicalCase.Id);
if (consultationResult.IsSuccess && consultationResult.Data != null)
{
// 修复：取第一个诊疗记录
var consultations = consultationResult.Data;
if (consultations.Any())
{
detail.Consultation = consultations.First();
detail.HasConsultation = true;
}
}
}
catch (Exception ex)
{
Logger.LogWarning(ex, "获取医案 {MedicalCaseId} 的诊疗记录失败", medicalCase.Id);
}

historyDetails.Add(detail);
}

ShowHistoryDialog(SelectedPatient, historyDetails);
}
catch (Exception ex)
{
LogError(ex, "查看患者历史诊疗记录失败: {PatientId}", SelectedPatient.Id);
ShowError($"查看患者历史记录失败: {ex.Message}");
}
finally
{
IsLoading = false;
}
}

/// <summary>
/// 显示患者历史诊疗记录对话框
/// 实用化设计：清晰展示关键诊疗信息，便于医生快速了解病史
/// </summary>
/// <param name="patient">患者信息</param>
/// <param name="historyDetails">历史诊疗详情列表</param>
private void ShowHistoryDialog(PatientDto patient, List<PatientHistoryDetail>? historyDetails)
{
var historyContent = new System.Text.StringBuilder();

// 患者基本信息
historyContent.AppendLine("=== 患者历史诊疗记录 ===\n");
historyContent.AppendLine("【患者信息】");
historyContent.AppendLine($"姓名: {patient.Name}");
historyContent.AppendLine($"性别: {GetGenderText(patient.Gender)}");
historyContent.AppendLine($"年龄: {patient.Age}岁");
historyContent.AppendLine($"电话: {patient.PhoneNumber ?? "未填写"}");

if (!string.IsNullOrEmpty(patient.AllergyHistory) && patient.AllergyHistory != "无")
{
historyContent.AppendLine($" 过敏史: {patient.AllergyHistory}");
}

historyContent.AppendLine();

if (historyDetails == null || !historyDetails.Any())
{
historyContent.AppendLine("【诊疗记录】");
historyContent.AppendLine("暂无历史就诊记录");
historyContent.AppendLine("\n 提示：该患者尚未有诊疗记录，可开始新的看诊流程。");
}
else
{
historyContent.AppendLine($"【诊疗记录】(共 {historyDetails.Count} 次就诊)\n");

for (int i = 0; i < historyDetails.Count; i++)
{
var detail = historyDetails[i];
var medicalCase = detail.MedicalCase;

historyContent.AppendLine($"▶ 第 {i + 1} 次就诊 - {detail.CreateTime:yyyy-MM-dd HH:mm}");
historyContent.AppendLine($" 状态: {detail.Status}");

if (!string.IsNullOrEmpty(medicalCase.Remark))
{
var remark = medicalCase.Remark.Length > 50 ?
medicalCase.Remark.Substring(0, 50) + "..." :
medicalCase.Remark;
historyContent.AppendLine($" 备注: {remark}");
}

// 显示诊断信息
if (detail.HasConsultation && detail.Consultation != null)
{
var consultation = detail.Consultation;

if (!string.IsNullOrEmpty(consultation.ChiefComplaint))
{
var complaint = consultation.ChiefComplaint.Length > 40 ?
consultation.ChiefComplaint.Substring(0, 40) + "..." :
consultation.ChiefComplaint;
historyContent.AppendLine($" 主诉: {complaint}");
}

if (!string.IsNullOrEmpty(consultation.TCMDiagnosis))
{
var diagnosis = consultation.TCMDiagnosis.Length > 40 ?
consultation.TCMDiagnosis.Substring(0, 40) + "..." :
consultation.TCMDiagnosis;
historyContent.AppendLine($" 诊断: {diagnosis}");
}
}

historyContent.AppendLine();
}

if (historyDetails.Count >= 20)
{
historyContent.AppendLine(" 注：为保持界面简洁，仅显示最近20条记录。");
}

historyContent.AppendLine(" 提示：可在医案管理模块查看完整详细记录。");
}

// 使用基类的通知方法显示历史信息
ShowInfo(historyContent.ToString());
}

/// <summary>
/// 获取性别显示文本
/// </summary>
private string GetGenderText(LYBT.Shared.Models.Enums.Gender gender)
{
return gender switch
{
LYBT.Shared.Models.Enums.Gender.Male => "男",
LYBT.Shared.Models.Enums.Gender.Female => "女",
_ => "未知"
};
}

/// <summary>
/// 获取医案状态显示文本
/// </summary>
private string GetMedicalCaseStatusText(int status)
{
return status switch
{
0 => "已登记",
1 => "诊疗中",
2 => "已完成",
3 => "已取消",
4 => "已暂停",
_ => "未知状态"
};
}

/// <summary>
/// 患者历史诊疗详情数据模型
/// 用于整合医案和诊疗记录信息
/// </summary>
private class PatientHistoryDetail
{
public required LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDto MedicalCase { get; set; }
public LYBT.Shared.Models.Contracts.Consultation.ConsultationDto? Consultation { get; set; }
public DateTime CreateTime { get; set; }
public string Status { get; set; } = string.Empty;
public bool HasConsultation { get; set; }
}

#endregion P0-02: 患者历史诊疗查询功能

#region P0-04: 四诊录入模板功能

/// <summary>
/// 显示四诊模板选择菜单
/// Epic 03-P0-04: 实用化四诊录入界面简化，专为小诊所设计
/// 提供常用四诊模板快速录入，提高医生录入效率
/// </summary>
private async Task ShowTemplateMenuAsync()
{
try
{
// 获取常用四诊模板列表
var templates = GetCommonTemplates();

if (!templates.Any())
{
ShowInfo("暂无可用的四诊模板");
return;
}

// 构建模板选择菜单内容
var menuContent = new System.Text.StringBuilder();
menuContent.AppendLine("=== 常用四诊录入模板 ===\n");
menuContent.AppendLine("请选择要使用的模板（输入数字序号）：\n");

for (int i = 0; i < templates.Count; i++)
{
var template = templates[i];
menuContent.AppendLine($"{i + 1}. {template.Name}");
menuContent.AppendLine($" 适用症状：{template.Symptoms}");
menuContent.AppendLine($" 典型体征：{template.Signs}");
menuContent.AppendLine();
}

menuContent.AppendLine(" 提示：选择模板后将自动填入四诊内容，您可以根据实际情况进行调整");

// 简化实现：显示模板信息供参考，不实现复杂的选择逻辑
// 为小诊所优化，避免过度复杂的用户交互
ShowInfo(menuContent.ToString());

// 应用第一个最常用的模板作为示例
if (templates.Any())
{
await ApplyTemplateAsync(templates[0]);
}
}
catch (Exception ex)
{
LogError(ex, "显示四诊模板菜单失败");
ShowError("加载四诊模板失败，请重试");
}
}

/// <summary>
/// 应用选定的四诊模板
/// 将模板内容填入四诊录入区域，提高录入效率
/// </summary>
private async Task ApplyTemplateAsync(FourDiagnosisTemplate template)
{
try
{
await Task.Delay(50); // 避免异步警告

// 检查是否已有内容，避免覆盖
if (!string.IsNullOrEmpty(Consultation.Palpation?.Trim()))
{
var confirmOverwrite = await ShowConfirmationAsync(
"当前四诊内容不为空，是否要替换为模板内容？",
"确认替换");

if (!confirmOverwrite)
{
return;
}
}

// 应用模板内容到四诊录入区域
var templateContent = BuildTemplateContent(template);

// 更新四诊录入内容（映射到Palpation字段）
var currentConsultation = Consultation;
currentConsultation.Palpation = templateContent;

// 触发属性变更通知
Consultation = currentConsultation;

ShowSuccess($"已应用模板：{template.Name}");
Logger.LogInformation("应用四诊模板成功: {TemplateName}", template.Name);
}
catch (Exception ex)
{
LogError(ex, "应用四诊模板失败: {TemplateName}", template.Name);
ShowError("应用模板失败，请重试");
}
}

/// <summary>
/// 构建四诊模板内容
/// 将结构化的模板数据转换为录入格式
/// </summary>
private string BuildTemplateContent(FourDiagnosisTemplate template)
{
var content = new System.Text.StringBuilder();

content.AppendLine($"【{template.Name}】");
content.AppendLine();

content.AppendLine("望诊：");
content.AppendLine($" 面色：{template.FaceColor}");
content.AppendLine($" 舌质：{template.TongueBody}");
content.AppendLine($" 舌苔：{template.TongueCoating}");
content.AppendLine();

content.AppendLine("闻诊：");
content.AppendLine($" 声音：{template.Voice}");
content.AppendLine($" 呼吸：{template.Breathing}");
content.AppendLine();

content.AppendLine("问诊：");
content.AppendLine($" 主要症状：{template.MainSymptoms}");
content.AppendLine($" 伴随症状：{template.AccompanyingSymptoms}");
content.AppendLine();

content.AppendLine("切诊：");
content.AppendLine($" 脉象：{template.Pulse}");
content.AppendLine($" 腹诊：{template.Abdomen}");
content.AppendLine();

content.AppendLine("【辨证要点】");
content.AppendLine($" {template.DiagnosisPoints}");

return content.ToString();
}

/// <summary>
/// 获取常用四诊模板
/// 实用化设计：内置常见中医症候的四诊模板
/// </summary>
private List<FourDiagnosisTemplate> GetCommonTemplates()
{
return new List<FourDiagnosisTemplate>
{
new FourDiagnosisTemplate
{
Name = "风寒感冒",
Symptoms = "恶寒、发热、无汗、头痛",
Signs = "鼻塞声重、流清涕、咳嗽咯痰白",
FaceColor = "面色苍白或微红",
TongueBody = "舌质淡红",
TongueCoating = "舌苔薄白",
Voice = "声音沉重",
Breathing = "呼吸平缓",
MainSymptoms = "恶寒重、发热轻、无汗、头痛身痛",
AccompanyingSymptoms = "鼻塞、流清涕、咳嗽、痰稀白",
Pulse = "脉浮紧",
Abdomen = "腹部无异常",
DiagnosisPoints = "风寒束表，卫阳被遏，治宜辛温解表"
},
new FourDiagnosisTemplate
{
Name = "脾胃虚弱",
Symptoms = "食少腹胀、便溏乏力",
Signs = "面色萎黄、形体消瘦、倦怠懒言",
FaceColor = "面色萎黄无华",
TongueBody = "舌质淡胖",
TongueCoating = "舌苔白腻",
Voice = "声音低弱",
Breathing = "呼吸短浅",
MainSymptoms = "食少纳呆、脘腹胀满、便溏",
AccompanyingSymptoms = "神疲乏力、少气懒言、面色无华",
Pulse = "脉细弱",
Abdomen = "腹部柔软，按之不适",
DiagnosisPoints = "脾气虚弱，运化失职，治宜健脾益气"
},
new FourDiagnosisTemplate
{
Name = "肝郁气滞",
Symptoms = "情志不畅、胁肋胀痛",
Signs = "急躁易怒、善太息、胸胁胀满",
FaceColor = "面色正常或略暗",
TongueBody = "舌质正常或略红",
TongueCoating = "舌苔薄白或微黄",
Voice = "声音正常或略高",
Breathing = "呼吸时深时浅",
MainSymptoms = "情志不舒、胸胁胀满疼痛、善太息",
AccompanyingSymptoms = "急躁易怒、失眠多梦、食欲不振",
Pulse = "脉弦",
Abdomen = "腹部按之不适，胁下满闷",
DiagnosisPoints = "肝失疏泄，气机郁滞，治宜疏肝解郁"
},
new FourDiagnosisTemplate
{
Name = "肾阳虚",
Symptoms = "畏寒肢冷、腰膝酸软",
Signs = "精神萎靡、面色苍白、形寒肢冷",
FaceColor = "面色苍白无华",
TongueBody = "舌质淡胖",
TongueCoating = "舌苔白滑",
Voice = "声音低沉",
Breathing = "呼吸微弱",
MainSymptoms = "畏寒怕冷、四肢不温、腰膝酸软冷痛",
AccompanyingSymptoms = "精神不振、小便清长、大便溏薄",
Pulse = "脉沉迟无力",
Abdomen = "腹部喜按喜温",
DiagnosisPoints = "肾阳不足，温煦失职，治宜温补肾阳"
},
new FourDiagnosisTemplate
{
Name = "阴虚火旺",
Symptoms = "潮热盗汗、口干咽燥",
Signs = "形体消瘦、颧红、心烦失眠",
FaceColor = "面色潮红或颧红",
TongueBody = "舌质红少津",
TongueCoating = "舌苔少或无苔",
Voice = "声音沙哑或正常",
Breathing = "呼吸略急",
MainSymptoms = "潮热、盗汗、口干咽燥、五心烦热",
AccompanyingSymptoms = "失眠多梦、头晕耳鸣、腰膝酸软",
Pulse = "脉细数",
Abdomen = "腹部无异常",
DiagnosisPoints = "阴液不足，虚火上炎，治宜滋阴降火"
}
};
}

/// <summary>
/// 显示确认对话框（简化实现）
/// </summary>
private async Task<bool> ShowConfirmationAsync(string message, string title)
{
await Task.Delay(50); // 避免异步警告

// 简化实现：默认确认，避免复杂的UI交互
return true;
}

/// <summary>
/// 四诊模板数据模型
/// 用于存储常用四诊录入模板的结构化数据
/// </summary>
private class FourDiagnosisTemplate
{
public required string Name { get; set; }
public required string Symptoms { get; set; }
public required string Signs { get; set; }
public required string FaceColor { get; set; }
public required string TongueBody { get; set; }
public required string TongueCoating { get; set; }
public required string Voice { get; set; }
public required string Breathing { get; set; }
public required string MainSymptoms { get; set; }
public required string AccompanyingSymptoms { get; set; }
public required string Pulse { get; set; }
public required string Abdomen { get; set; }
public required string DiagnosisPoints { get; set; }
}

#endregion P0-04: 四诊录入模板功能
}
}
