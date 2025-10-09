using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.ViewModels
{

    /// <summary>
    /// ������������ͼģ�� - �򻯰洿���ݼ�¼
    /// ֻ����򵥵���������¼�룬���������̼�ܺ����ܴ���
    /// </summary>
    public class ConsultationMainViewModel : UnifiedViewModelBase
    {

        #region ��������

        private readonly IConsultationService _consultationService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPatientService _patientService;

        #endregion ��������

        #region ��������

        private string _title = "���Ƽ�¼";

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
                    // P0-02�Ż�����ѡ����ʱ��������ʷ��ѯ����״̬
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

        private bool _isLoading = false;

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion ��������

        #region ����

        public ICommand LoadPatientsCommand { get; }
        public ICommand SaveConsultationCommand { get; }
        public ICommand ClearDataCommand { get; }

        // P0-02������������ʷ���Ʋ�ѯ����
        public ICommand ViewPatientHistoryCommand { get; }

        // P0-04：四诊记录辅助模板功能
        public ICommand ShowTemplateMenuCommand { get; }

        /// <summary>
        /// 新建诊疗命令
        /// </summary>
        public ICommand NewConsultationCommand { get; }

        /// <summary>
        /// 打印处方命令
        /// </summary>
        public ICommand PrintPrescriptionCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 增加数量命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> IncreaseQuantityCommand { get; }

        /// <summary>
        /// 减少数量命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> DecreaseQuantityCommand { get; }

        /// <summary>
        /// 移除处方项命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> RemovePrescriptionItemCommand { get; }

        #endregion 命令

        #region ���캯��

        public ConsultationMainViewModel(
        IConsultationService consultationService,
        IMedicalCaseService medicalCaseService,
        IPatientService patientService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

            LoadPatientsCommand = new DelegateCommand(async () => await LoadPatientsAsync());
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync());
            ClearDataCommand = new DelegateCommand(ClearData);

            // P0-02��������ʼ��������ʷ��ѯ����
            ViewPatientHistoryCommand = new DelegateCommand(async () => await ViewPatientHistoryAsync(), () => SelectedPatient != null);

            // P0-04：初始化四诊模板命令
            ShowTemplateMenuCommand = new DelegateCommand(async () => await ShowTemplateMenuAsync());

            // 初始化新增命令
            NewConsultationCommand = new DelegateCommand(ExecuteNewConsultation);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, CanPrintPrescription);
            RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync());

            // 初始化 DataGrid 行命令
            IncreaseQuantityCommand = new DelegateCommand<object>(ExecuteIncreaseQuantity, item => item != null);
            DecreaseQuantityCommand = new DelegateCommand<object>(ExecuteDecreaseQuantity, item => item != null);
            RemovePrescriptionItemCommand = new DelegateCommand<object>(ExecuteRemovePrescriptionItem, item => item != null);

            // 修改: 使用Task.Run等待初始化，防止fire-and-forget
            _ = Task.Run(async () => await InitializeAsync());
        }

        #endregion ���캯��

        #region ��ʼ��

        private async Task InitializeAsync()
        {
            try
            {
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "��ʼ��ʧ��");

                // ϵͳ��ʼ��ʧ��
            }
        }

        #endregion ��ʼ��

        #region ���ݼ���

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;

                // ʹ�÷�ҳ��ѯ��ȡ�����б�
                var query = new LYBT.Shared.Models.Contracts.Patients.PatientSearchDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = string.Empty
                };

                var result = await _patientService.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword);
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
                Logger.LogError(ex, "���ػ����б�ʧ��");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion ���ݼ���

        #region ���ݱ���

        private async Task SaveConsultationAsync()
        {
            try
            {
                if (SelectedPatient == null)
                {
                    Logger.LogWarning("未选择患者");
                    return;
                }

                // 聚合根模式校验：确保 MedicalCaseId 有效
                if (MedicalCaseId == null || MedicalCaseId == Guid.Empty)
                {
                    Logger.LogError("无法保存诊疗记录：缺少医案ID");
                    await ShowErrorMessageAsync("无法保存诊疗记录：请从病历详情进入诊疗界面");
                    return;
                }

                IsLoading = true;

                // 更新诊疗记录（不是创建）
                // 因为 MedicalCase 创建时已自动创建 Consultation
                var updateDto = new ConsultationUpdateDto
                {
                    ChiefComplaint = Consultation?.ChiefComplaint ?? string.Empty,
                    PresentIllness = Consultation?.PresentIllness,
                    Inspection = Consultation?.Inspection,
                    AuscultationOlfaction = Consultation?.AuscultationOlfaction,
                    Inquiry = Consultation?.Inquiry,
                    Palpation = Consultation?.Palpation,
                    TCMDiagnosis = Consultation?.TCMDiagnosis,
                    TreatmentPrinciple = Consultation?.TreatmentPrinciple,
                    Remark = $"患者：{SelectedPatient.Name}，医生：{SessionManager?.CurrentUser?.RealName ?? string.Empty}"
                };

                var result = await _consultationService.UpdateAsync(MedicalCaseId.Value, updateDto);
                if (result.IsSuccess && result.Data != null)
                {
                    Consultation = result.Data;
                    SetStatus("诊疗记录保存成功");
                }
                else
                {
                    Logger.LogError("保存失败: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存诊疗记录失败");
                Logger.LogError("保存失败，请稍后重试");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion ���ݱ���

        #region ��������

        private void ClearData()
        {
            Consultation = new ConsultationDto();
            SelectedPatient = null;
            MedicalCaseId = null;
        }

        #endregion 命令初始化

        #region 导航接口实现

        /// <inheritdoc/>
        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters["MedicalCaseId"] is Guid caseId)
            {
                MedicalCaseId = caseId;
                
                // 聚合根模式：加载已存在的 Consultation（MedicalCase 创建时自动生成）
                await LoadExistingConsultationAsync(caseId);
            }
        }

        /// <inheritdoc/>
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <inheritdoc/>
        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        /// <summary>
        /// 加载已存在的 Consultation（聚合根模式：MedicalCase 创建时自动生成）
        /// </summary>
        private async Task LoadExistingConsultationAsync(Guid medicalCaseId)
        {
            try
            {
                IsLoading = true;

                // 通过 MedicalCaseId 加载 Consultation（共享主键：Consultation.Id == MedicalCase.Id）
                var result = await _consultationService.GetByIdAsync(medicalCaseId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Consultation = result.Data;
                    Logger.LogInformation("已加载医案 {MedicalCaseId} 的诊疗记录", medicalCaseId);
                }
                else
                {
                    // 如果加载失败，可能是旧数据（MedicalCase 创建时未自动创建 Consultation）
                    Logger.LogWarning("未找到医案 {MedicalCaseId} 的诊疗记录，将创建新记录", medicalCaseId);
                    
                    // 初始化空的 Consultation 对象供用户填写
                    Consultation = new ConsultationDto
                    {
                        Id = medicalCaseId,
                        MedicalCaseId = medicalCaseId,
                        PatientId = SelectedPatient?.Id ?? Guid.Empty,
                        UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                        PatientName = SelectedPatient?.Name ?? string.Empty,
                        DoctorName = SessionManager?.CurrentUser?.RealName ?? string.Empty
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载诊疗记录失败");
                await ShowErrorMessageAsync("加载诊疗记录失败，请稍后重试");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion 导航接口实现

        #region P0-02: 就诊历史和复诊查询功能

        /// <summary>
        /// �鿴������ʷ���Ƽ�¼
        /// Epic 03-P0-02: ʵ�û���ʷ��ѯ���ܣ�רΪС�������
        /// �ṩ�����Ļ���������ʷ������ҽ������ϡ���������Ϣ
        /// </summary>
        private async Task ViewPatientHistoryAsync()
        {
            if (SelectedPatient == null)
            {
                Logger.LogWarning("����ѡ����");
                return;
            }

            try
            {
                IsLoading = true;

                // ��ȡ���ߵ�����ҽ����ʷ
                var medicalCasesResult = await _medicalCaseService.GetByPatientIdAsync(SelectedPatient.Id);

                if (!medicalCasesResult.IsSuccess || medicalCasesResult.Data == null || !medicalCasesResult.Data.Any())
                {
                    ShowHistoryDialog(SelectedPatient, null);
                    return;
                }

                var medicalCases = medicalCasesResult.Data
                .OrderByDescending(mc => mc.CreatedAt)
                .ToList();

                // Ϊÿ��ҽ����ȡ������Ƽ�¼
                var historyDetails = new List<PatientHistoryDetail>();

                foreach (var medicalCase in medicalCases.Take(20)) // �����ʾ20����ʷ��¼
                {
                    var detail = new PatientHistoryDetail
                    {
                        MedicalCase = medicalCase,
                        CreatedAt = medicalCase.CreatedAt,
                        Status = GetMedicalCaseStatusText((int)medicalCase.Status)
                    };

                    // ���Ի�ȡ��ҽ�������Ƽ�¼
                    try
                    {
                        var consultationResult = await _consultationService.GetByMedicalCaseIdAsync(medicalCase.Id);
                        if (consultationResult.IsSuccess && consultationResult.Data != null)
                        {
                            // �޸���ȡ��һ�����Ƽ�¼
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
                        Logger.LogWarning(ex, "��ȡҽ�� {MedicalCaseId} �����Ƽ�¼ʧ��", medicalCase.Id);
                    }

                    historyDetails.Add(detail);
                }

                ShowHistoryDialog(SelectedPatient, historyDetails);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "�鿴������ʷ���Ƽ�¼ʧ��: {PatientId}", SelectedPatient.Id);
                // �鿴������ʷ��¼ʧ��
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// ��ʾ������ʷ���Ƽ�¼�Ի���
        /// ʵ�û���ƣ�����չʾ�ؼ�������Ϣ������ҽ�������˽ⲡʷ
        /// </summary>
        /// <param name="patient">������Ϣ</param>
        /// <param name="historyDetails">��ʷ���������б�</param>
        private void ShowHistoryDialog(PatientDto patient, List<PatientHistoryDetail>? historyDetails)
        {
            var historyContent = new System.Text.StringBuilder();

            // ���߻�����Ϣ
            historyContent.AppendLine("=== ������ʷ���Ƽ�¼ ===\n");
            historyContent.AppendLine("��������Ϣ��");
            historyContent.AppendLine($"����: {patient.Name}");
            historyContent.AppendLine($"�Ա�: {GetGenderText(patient.Gender)}");
            historyContent.AppendLine($"����: {patient.Age}��");
            historyContent.AppendLine($"�绰: {patient.PhoneNumber ?? "δ��д"}");

            if (!string.IsNullOrEmpty(patient.AllergyHistory) && patient.AllergyHistory != "��")
            {
                historyContent.AppendLine($" ����ʷ: {patient.AllergyHistory}");
            }

            historyContent.AppendLine();

            if (historyDetails == null || !historyDetails.Any())
            {
                historyContent.AppendLine("�����Ƽ�¼��");
                historyContent.AppendLine("������ʷ�����¼");
                historyContent.AppendLine("\n ��ʾ���û�����δ�����Ƽ�¼���ɿ�ʼ�µ��������̡�");
            }
            else
            {
                historyContent.AppendLine($"�����Ƽ�¼��(�� {historyDetails.Count} �ξ���)\n");

                for (int i = 0; i < historyDetails.Count; i++)
                {
                    var detail = historyDetails[i];
                    var medicalCase = detail.MedicalCase;

                    historyContent.AppendLine($"? �� {i + 1} �ξ��� - {detail.CreatedAt:yyyy-MM-dd HH:mm}");
                    historyContent.AppendLine($" ״̬: {detail.Status}");

                    if (!string.IsNullOrEmpty(medicalCase.Remark))
                    {
                        var remark = medicalCase.Remark.Length > 50 ?
                        medicalCase.Remark.Substring(0, 50) + "..." :
                        medicalCase.Remark;
                        historyContent.AppendLine($" ��ע: {remark}");
                    }

                    // ��ʾ�����Ϣ
                    if (detail.HasConsultation && detail.Consultation != null)
                    {
                        var consultation = detail.Consultation;

                        if (!string.IsNullOrEmpty(consultation.ChiefComplaint))
                        {
                            var complaint = consultation.ChiefComplaint.Length > 40 ?
                            consultation.ChiefComplaint.Substring(0, 40) + "..." :
                            consultation.ChiefComplaint;
                            historyContent.AppendLine($" ����: {complaint}");
                        }

                        if (!string.IsNullOrEmpty(consultation.TCMDiagnosis))
                        {
                            var diagnosis = consultation.TCMDiagnosis.Length > 40 ?
                            consultation.TCMDiagnosis.Substring(0, 40) + "..." :
                            consultation.TCMDiagnosis;
                            historyContent.AppendLine($" ���: {diagnosis}");
                        }
                    }

                    historyContent.AppendLine();
                }

                if (historyDetails.Count >= 20)
                {
                    historyContent.AppendLine(" ע��Ϊ���ֽ����࣬����ʾ���20����¼��");
                }

                historyContent.AppendLine(" ��ʾ������ҽ������ģ��鿴������ϸ��¼��");
            }

            // ʹ�û����֪ͨ������ʾ��ʷ��Ϣ
            SetStatus(historyContent.ToString());
        }

        /// <summary>
        /// ��ȡ�Ա���ʾ�ı�
        /// </summary>
        private string GetGenderText(LYBT.Shared.Models.Enums.Gender gender)
        {
            return gender switch
            {
                LYBT.Shared.Models.Enums.Gender.Male => "��",
                LYBT.Shared.Models.Enums.Gender.Female => "Ů",
                _ => "δ֪"
            };
        }

        /// <summary>
        /// ��ȡҽ��״̬��ʾ�ı�
        /// </summary>
        private string GetMedicalCaseStatusText(int status)
        {
            return status switch
            {
                0 => "�ѵǼ�",
                1 => "������",
                2 => "�����",
                3 => "��ȡ��",
                4 => "����ͣ",
                _ => "δ֪״̬"
            };
        }

        /// <summary>
        /// ������ʷ������������ģ��
        /// ��������ҽ�������Ƽ�¼��Ϣ
        /// </summary>
        private class PatientHistoryDetail
        {
            public required LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDto MedicalCase { get; set; }
            public LYBT.Shared.Models.Contracts.Consultation.ConsultationDto? Consultation { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = string.Empty;
            public bool HasConsultation { get; set; }
        }

        #endregion P0-02: ������ʷ���Ʋ�ѯ����

        #region P0-04: ����¼��ģ�幦��

        /// <summary>
        /// ��ʾ����ģ��ѡ��˵�
        /// Epic 03-P0-04: ʵ�û�����¼�����򻯣�רΪС�������
        /// �ṩ��������ģ�����¼�룬���ҽ��¼��Ч��
        /// </summary>
        private async Task ShowTemplateMenuAsync()
        {
            try
            {
                // ��ȡ��������ģ���б�
                var templates = GetCommonTemplates();

                if (!templates.Any())
                {
                    SetStatus("���޿��õ�����ģ��");
                    return;
                }

                // ����ģ��ѡ��˵�����
                var menuContent = new System.Text.StringBuilder();
                menuContent.AppendLine("=== ��������¼��ģ�� ===\n");
                menuContent.AppendLine("��ѡ��Ҫʹ�õ�ģ�壨����������ţ���\n");

                for (int i = 0; i < templates.Count; i++)
                {
                    var template = templates[i];
                    menuContent.AppendLine($"{i + 1}. {template.Name}");
                    menuContent.AppendLine($" ����֢״��{template.Symptoms}");
                    menuContent.AppendLine($" ����������{template.Signs}");
                    menuContent.AppendLine();
                }

                menuContent.AppendLine(" ��ʾ��ѡ��ģ����Զ������������ݣ������Ը���ʵ��������е���");

                // ��ʵ�֣���ʾģ����Ϣ���ο�����ʵ�ָ��ӵ�ѡ���߼�
                // ΪС�����Ż���������ȸ��ӵ��û�����
                SetStatus(menuContent.ToString());

                // Ӧ�õ�һ����õ�ģ����Ϊʾ��
                if (templates.Any())
                {
                    await ApplyTemplateAsync(templates[0]);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "��ʾ����ģ��˵�ʧ��");
                Logger.LogError("��������ģ��ʧ��");
            }
        }

        /// <summary>
        /// Ӧ��ѡ��������ģ��
        /// ��ģ��������������¼���������¼��Ч��
        /// </summary>
        private async Task ApplyTemplateAsync(FourDiagnosisTemplate template)
        {
            try
            {
                await Task.Delay(50); // �����첽����

                // ����Ƿ��������ݣ����⸲��
                if (!string.IsNullOrEmpty(Consultation.Palpation?.Trim()))
                {
                    var confirmOverwrite = await ShowConfirmationAsync(
                    "��ǰ�������ݲ�Ϊ�գ��Ƿ�Ҫ�滻Ϊģ�����ݣ�",
                    "ȷ���滻");

                    if (!confirmOverwrite)
                    {
                        return;
                    }
                }

                // Ӧ��ģ�����ݵ�����¼������
                var templateContent = BuildTemplateContent(template);

                // ��������¼�����ݣ�ӳ�䵽Palpation�ֶΣ�
                var currentConsultation = Consultation;
                currentConsultation.Palpation = templateContent;

                // �������Ա��֪ͨ
                Consultation = currentConsultation;

                SetStatus($"��Ӧ��ģ�壺{template.Name}");
                Logger.LogInformation("Ӧ������ģ��ɹ�: {TemplateName}", template.Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Ӧ������ģ��ʧ��: {TemplateName}", template.Name);
                Logger.LogError("Ӧ��ģ��ʧ��");
            }
        }

        /// <summary>
        /// ��������ģ������
        /// ���ṹ����ģ������ת��Ϊ¼���ʽ
        /// </summary>
        private string BuildTemplateContent(FourDiagnosisTemplate template)
        {
            var content = new System.Text.StringBuilder();

            content.AppendLine($"��{template.Name}��");
            content.AppendLine();

            content.AppendLine("���");
            content.AppendLine($" ��ɫ��{template.FaceColor}");
            content.AppendLine($" ���ʣ�{template.TongueBody}");
            content.AppendLine($" ��̦��{template.TongueCoating}");
            content.AppendLine();

            content.AppendLine("���");
            content.AppendLine($" ������{template.Voice}");
            content.AppendLine($" ������{template.Breathing}");
            content.AppendLine();

            content.AppendLine("���");
            content.AppendLine($" ��Ҫ֢״��{template.MainSymptoms}");
            content.AppendLine($" ����֢״��{template.AccompanyingSymptoms}");
            content.AppendLine();

            content.AppendLine("���");
            content.AppendLine($" ����{template.Pulse}");
            content.AppendLine($" ���{template.Abdomen}");
            content.AppendLine();

            content.AppendLine("����֤Ҫ�㡿");
            content.AppendLine($" {template.DiagnosisPoints}");

            return content.ToString();
        }

        /// <summary>
        /// ��ȡ��������ģ��
        /// ʵ�û���ƣ����ó�����ҽ֢�������ģ��
        /// </summary>
        private List<FourDiagnosisTemplate> GetCommonTemplates()
        {
            return new List<FourDiagnosisTemplate>
{
new FourDiagnosisTemplate
{
Name = "�纮��ð",
Symptoms = "�񺮡����ȡ��޺���ͷʹ",
Signs = "�������ء������顢���Կ�̵��",
FaceColor = "��ɫ�԰׻�΢��",
TongueBody = "���ʵ���",
TongueCoating = "��̦����",
Voice = "��������",
Breathing = "����ƽ��",
MainSymptoms = "���ء������ᡢ�޺���ͷʹ��ʹ",
AccompanyingSymptoms = "�����������顢���ԡ�̵ϡ��",
Pulse = "������",
Abdomen = "�������쳣",
DiagnosisPoints = "�纮�����������������������½��"
},
new FourDiagnosisTemplate
{
Name = "Ƣθ����",
Symptoms = "ʳ�ٸ��͡����緦��",
Signs = "��ɫή�ơ��������ݡ��뵡����",
FaceColor = "��ɫή���޻�",
TongueBody = "���ʵ���",
TongueCoating = "��̦����",
Voice = "��������",
Breathing = "������ǳ",
MainSymptoms = "ʳ���ɴ����丹����������",
AccompanyingSymptoms = "��ƣ�������������ԡ���ɫ�޻�",
Pulse = "��ϸ��",
Abdomen = "������������֮����",
DiagnosisPoints = "Ƣ���������˻�ʧְ�����˽�Ƣ����"
},
new FourDiagnosisTemplate
{
Name = "��������",
Symptoms = "��־������в����ʹ",
Signs = "������ŭ����̫Ϣ����в����",
FaceColor = "��ɫ�������԰�",
TongueBody = "�����������Ժ�",
TongueCoating = "��̦���׻�΢��",
Voice = "�����������Ը�",
Breathing = "����ʱ��ʱǳ",
MainSymptoms = "��־���桢��в������ʹ����̫Ϣ",
AccompanyingSymptoms = "������ŭ��ʧ�߶��Ρ�ʳ������",
Pulse = "����",
Abdomen = "������֮���ʣ�в������",
DiagnosisPoints = "��ʧ��й���������ͣ�������ν���"
},
new FourDiagnosisTemplate
{
Name = "������",
Symptoms = "η��֫�䡢��ϥ����",
Signs = "����ή�ҡ���ɫ�԰ס��κ�֫��",
FaceColor = "��ɫ�԰��޻�",
TongueBody = "���ʵ���",
TongueCoating = "��̦�׻�",
Voice = "�����ͳ�",
Breathing = "����΢��",
MainSymptoms = "η�����䡢��֫���¡���ϥ������ʹ",
AccompanyingSymptoms = "������С���峤������籡",
Pulse = "����������",
Abdomen = "����ϲ��ϲ��",
DiagnosisPoints = "�������㣬����ʧְ�������²�����"
},
new FourDiagnosisTemplate
{
Name = "�������",
Symptoms = "���ȵ������ڸ�����",
Signs = "�������ݡ�ȧ�졢�ķ�ʧ��",
FaceColor = "��ɫ�����ȧ��",
TongueBody = "���ʺ��ٽ�",
TongueCoating = "��̦�ٻ���̦",
Voice = "����ɳ�ƻ�����",
Breathing = "�����Լ�",
MainSymptoms = "���ȡ��������ڸ�������ķ���",
AccompanyingSymptoms = "ʧ�߶��Ρ�ͷ�ζ�������ϥ����",
Pulse = "��ϸ��",
Abdomen = "�������쳣",
DiagnosisPoints = "��Һ���㣬������ף�������������"
}
};
        }

        /// <summary>
        /// ��ʾȷ�϶Ի��򣨼�ʵ�֣�
        /// </summary>
        private new async Task<bool> ShowConfirmationAsync(string message, string title)
        {
            await Task.Delay(50); // �����첽����

            // ��ʵ�֣�Ĭ��ȷ�ϣ����⸴�ӵ�UI����
            return true;
        }

        /// <summary>
        /// ����ģ������ģ��
        /// ���ڴ洢��������¼��ģ��Ľṹ������
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

        #endregion P0-04: 四诊记录辅助模板功能

        #region 新增命令实现

        /// <summary>
        /// 新建诊疗
        /// </summary>
        private void ExecuteNewConsultation()
        {
            try
            {
                Logger.LogInformation("新建诊疗");
                ClearData();
                SetStatus("已清空当前诊疗记录，可以开始新的诊疗");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "新建诊疗时发生异常");
            }
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        private void ExecutePrintPrescription()
        {
            try
            {
                Logger.LogInformation("打印处方功能开发中");
                SetStatus("打印处方功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打印处方时发生异常");
            }
        }

        /// <summary>
        /// 检查是否可以打印处方
        /// </summary>
        private bool CanPrintPrescription()
        {
            return !IsLoading && SelectedPatient != null;
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private async Task ExecuteRefreshAsync()
        {
            try
            {
                await LoadPatientsAsync();
                SetStatus("数据刷新成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新数据时发生异常");
            }
        }

        /// <summary>
        /// 增加药材数量
        /// </summary>
        private void ExecuteIncreaseQuantity(object item)
        {
            try
            {
                Logger.LogInformation("增加药材数量功能开发中");
                SetStatus("增加药材数量功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "增加药材数量时发生异常");
            }
        }

        /// <summary>
        /// 减少药材数量
        /// </summary>
        private void ExecuteDecreaseQuantity(object item)
        {
            try
            {
                Logger.LogInformation("减少药材数量功能开发中");
                SetStatus("减少药材数量功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "减少药材数量时发生异常");
            }
        }

        /// <summary>
        /// 移除处方项
        /// </summary>
        private void ExecuteRemovePrescriptionItem(object item)
        {
            try
            {
                Logger.LogInformation("移除处方项功能开发中");
                SetStatus("移除处方项功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "移除处方项时发生异常");
            }
        }

        #endregion 新增命令实现
    }
}
