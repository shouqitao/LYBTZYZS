using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Xunit;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Modules.Consultation.ViewModels;
using LYBT.WPF.Client.Services.Interfaces;

namespace LYBT.WPF.Client.Tests.Frontend.Desktop.Consultation
{
    /// <summary>
    /// 诊疗工作流集成测试
    /// </summary>
    public class ConsultationWorkflowIntegrationTests
    {
        private readonly Mock<IEventAggregator> _eventAggregatorMock;
        private readonly Mock<IConsultationService> _consultationServiceMock;
        private readonly Mock<IPrescriptionService> _prescriptionServiceMock;
        private readonly Mock<IHerbService> _herbServiceMock;
        private readonly Mock<IFormulaService> _formulaServiceMock;
        private readonly Mock<ICommonDialogService> _dialogServiceMock;
        private readonly Mock<ILogger<ConsultationWorkflowViewModel>> _workflowLoggerMock;
        private readonly Mock<ILogger<SimpleTCMFourDiagnosisViewModel>> _fourDiagnosisLoggerMock;
        private readonly Mock<ILogger<DifferentiationViewModel>> _differentiationLoggerMock;
        private readonly Mock<ILogger<PrescriptionViewModel>> _prescriptionLoggerMock;

        public ConsultationWorkflowIntegrationTests()
        {
            _eventAggregatorMock = new Mock<IEventAggregator>();
            _consultationServiceMock = new Mock<IConsultationService>();
            _prescriptionServiceMock = new Mock<IPrescriptionService>();
            _herbServiceMock = new Mock<IHerbService>();
            _formulaServiceMock = new Mock<IFormulaService>();
            _dialogServiceMock = new Mock<ICommonDialogService>();
            _workflowLoggerMock = new Mock<ILogger<ConsultationWorkflowViewModel>>();
            _fourDiagnosisLoggerMock = new Mock<ILogger<SimpleTCMFourDiagnosisViewModel>>();
            _differentiationLoggerMock = new Mock<ILogger<DifferentiationViewModel>>();
            _prescriptionLoggerMock = new Mock<ILogger<PrescriptionViewModel>>();

            // 设置事件聚合器
            SetupEventAggregator();
        }

        private void SetupEventAggregator()
        {
            // 模拟事件发布和订阅
            var workflowStepCompletedEvent = new Mock<WorkflowStepCompletedEvent>();
            var navigateToStepEvent = new Mock<NavigateToStepEvent>();
            var saveStepDataEvent = new Mock<SaveStepDataEvent>();

            _eventAggregatorMock.Setup(x => x.GetEvent<WorkflowStepCompletedEvent>())
                .Returns(workflowStepCompletedEvent.Object);
            _eventAggregatorMock.Setup(x => x.GetEvent<NavigateToStepEvent>())
                .Returns(navigateToStepEvent.Object);
            _eventAggregatorMock.Setup(x => x.GetEvent<SaveStepDataEvent>())
                .Returns(saveStepDataEvent.Object);
        }

        [Fact]
        public async Task CompleteWorkflow_ShouldProcessAllSteps_Successfully()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            
            // 创建工作流视图模型
            var workflowViewModel = new ConsultationWorkflowViewModel(
                _eventAggregatorMock.Object,
                _consultationServiceMock.Object,
                _workflowLoggerMock.Object);

            // 创建各步骤视图模型
            var fourDiagnosisViewModel = new SimpleTCMFourDiagnosisViewModel(
                _eventAggregatorMock.Object,
                _consultationServiceMock.Object,
                _dialogServiceMock.Object,
                _fourDiagnosisLoggerMock.Object);

            var differentiationViewModel = new DifferentiationViewModel(
                _eventAggregatorMock.Object,
                _consultationServiceMock.Object,
                _dialogServiceMock.Object,
                _differentiationLoggerMock.Object);

            var prescriptionViewModel = new PrescriptionViewModel(
                _eventAggregatorMock.Object,
                _prescriptionServiceMock.Object,
                _herbServiceMock.Object,
                _formulaServiceMock.Object,
                _dialogServiceMock.Object,
                _prescriptionLoggerMock.Object);

            // Act & Assert - 步骤1：四诊采集
            await fourDiagnosisViewModel.InitializeAsync(medicalCaseId);
            
            fourDiagnosisViewModel.Inspection = "面色萎黄，神疲倦怠";
            fourDiagnosisViewModel.Auscultation = "语声低微";
            fourDiagnosisViewModel.Inquiry = "食欲不振，大便溏薄";
            fourDiagnosisViewModel.Palpation = "脉细弱，舌淡苔白";
            
            Assert.True(fourDiagnosisViewModel.HasChanges);
            
            // 模拟保存四诊数据
            await fourDiagnosisViewModel.SaveAsync();

            // Act & Assert - 步骤2：辨证分析
            await differentiationViewModel.InitializeAsync(medicalCaseId);
            
            differentiationViewModel.SelectedSyndrome = "脾胃虚弱";
            differentiationViewModel.TreatmentPrinciple = "健脾益气，和胃止泻";
            differentiationViewModel.Analysis = "患者脾胃虚弱，运化失常，故见食欲不振、大便溏薄等症";
            
            Assert.True(differentiationViewModel.HasChanges);
            
            // 模拟保存辨证数据
            await differentiationViewModel.SaveAsync();

            // Act & Assert - 步骤3：处方开具
            await prescriptionViewModel.InitializeAsync(medicalCaseId);
            
            // 添加处方药材
            prescriptionViewModel.PrescriptionItems.Add(new PrescriptionViewModel.PrescriptionItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "党参",
                Quantity = 15,
                Unit = "g",
                UnitPrice = 2.5m,
                Subtotal = 37.5m,
                Source = "手动添加"
            });
            
            prescriptionViewModel.PrescriptionItems.Add(new PrescriptionViewModel.PrescriptionItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "白术",
                Quantity = 10,
                Unit = "g",
                UnitPrice = 1.8m,
                Subtotal = 18m,
                Source = "手动添加"
            });
            
            prescriptionViewModel.DosageCount = 7;
            prescriptionViewModel.Usage = "每日1剂，水煎服，分早晚两次温服";
            
            Assert.Equal(2, prescriptionViewModel.PrescriptionItems.Count);
            Assert.Equal(55.5m, prescriptionViewModel.SingleDosagePrice);
            Assert.Equal(388.5m, prescriptionViewModel.TotalPrice);
            
            // 模拟保存处方数据
            await prescriptionViewModel.SaveAsync();

            // Verify - 验证工作流完成
            Assert.NotNull(workflowViewModel);
            Assert.NotNull(fourDiagnosisViewModel);
            Assert.NotNull(differentiationViewModel);
            Assert.NotNull(prescriptionViewModel);
        }

        [Fact]
        public void WorkflowNavigation_ShouldAllowStepSwitching()
        {
            // Arrange
            var workflowViewModel = new ConsultationWorkflowViewModel(
                _eventAggregatorMock.Object,
                _consultationServiceMock.Object,
                _workflowLoggerMock.Object);

            // Act & Assert - 初始状态
            Assert.Equal(ConsultationWorkflowViewModel.WorkflowStep.PatientSelection, 
                workflowViewModel.CurrentStep);

            // 切换到四诊
            workflowViewModel.CurrentStep = ConsultationWorkflowViewModel.WorkflowStep.FourDiagnosis;
            Assert.Equal(ConsultationWorkflowViewModel.WorkflowStep.FourDiagnosis, 
                workflowViewModel.CurrentStep);

            // 切换到辨证
            workflowViewModel.CurrentStep = ConsultationWorkflowViewModel.WorkflowStep.Differentiation;
            Assert.Equal(ConsultationWorkflowViewModel.WorkflowStep.Differentiation, 
                workflowViewModel.CurrentStep);

            // 切换到处方
            workflowViewModel.CurrentStep = ConsultationWorkflowViewModel.WorkflowStep.Prescription;
            Assert.Equal(ConsultationWorkflowViewModel.WorkflowStep.Prescription, 
                workflowViewModel.CurrentStep);
        }

        [Fact]
        public async Task FourDiagnosis_ImportTemplate_ShouldPopulateFields()
        {
            // Arrange
            var viewModel = new SimpleTCMFourDiagnosisViewModel(
                _eventAggregatorMock.Object,
                _consultationServiceMock.Object,
                _dialogServiceMock.Object,
                _fourDiagnosisLoggerMock.Object);

            var medicalCaseId = Guid.NewGuid();
            await viewModel.InitializeAsync(medicalCaseId);

            // Act - 导入模板
            var template = viewModel.TemplateManager.GetTemplate("风寒感冒");
            if (template != null)
            {
                viewModel.Inspection = template.Inspection;
                viewModel.Auscultation = template.Auscultation;
                viewModel.Inquiry = template.Inquiry;
                viewModel.Palpation = template.Palpation;
            }

            // Assert
            Assert.NotNull(template);
            Assert.Contains("恶寒", viewModel.Inspection);
            Assert.Contains("咳嗽", viewModel.Auscultation);
            Assert.True(viewModel.HasChanges);
        }

        [Fact]
        public void Differentiation_SelectSyndrome_ShouldAutoFillTreatmentPrinciple()
        {
            // Arrange
            var viewModel = new DifferentiationViewModel(
                _eventAggregatorMock.Object,
                _consultationServiceMock.Object,
                _dialogServiceMock.Object,
                _differentiationLoggerMock.Object);

            // Act
            var syndrome = viewModel.CommonSyndromes.First(s => s.Name == "风寒感冒");
            viewModel.SelectSyndromeCommand.Execute(syndrome);

            // Assert
            Assert.Equal("风寒感冒", viewModel.SelectedSyndrome);
            Assert.Equal("辛温解表，宣肺散寒", viewModel.TreatmentPrinciple);
            Assert.True(viewModel.HasChanges);
        }

        [Fact]
        public void Prescription_AddHerbs_ShouldCalculatePrice()
        {
            // Arrange
            var viewModel = new PrescriptionViewModel(
                _eventAggregatorMock.Object,
                _prescriptionServiceMock.Object,
                _herbServiceMock.Object,
                _formulaServiceMock.Object,
                _dialogServiceMock.Object,
                _prescriptionLoggerMock.Object);

            // Act
            viewModel.PrescriptionItems.Add(new PrescriptionViewModel.PrescriptionItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "麻黄",
                Quantity = 6,
                Unit = "g",
                UnitPrice = 3.5m,
                Subtotal = 21m
            });

            viewModel.PrescriptionItems.Add(new PrescriptionViewModel.PrescriptionItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "桂枝",
                Quantity = 9,
                Unit = "g",
                UnitPrice = 2.8m,
                Subtotal = 25.2m
            });

            viewModel.DosageCount = 5;

            // Assert
            Assert.Equal(2, viewModel.PrescriptionItems.Count);
            Assert.Equal(46.2m, viewModel.SingleDosagePrice);
            Assert.Equal(231m, viewModel.TotalPrice);
            Assert.Equal(231m, viewModel.DiscountedPrice); // 无折扣
        }

        [Fact]
        public void Prescription_ApplyDiscount_ShouldUpdatePrice()
        {
            // Arrange
            var viewModel = new PrescriptionViewModel(
                _eventAggregatorMock.Object,
                _prescriptionServiceMock.Object,
                _herbServiceMock.Object,
                _formulaServiceMock.Object,
                _dialogServiceMock.Object,
                _prescriptionLoggerMock.Object);

            viewModel.PrescriptionItems.Add(new PrescriptionViewModel.PrescriptionItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "人参",
                Quantity = 10,
                Unit = "g",
                UnitPrice = 50m,
                Subtotal = 500m
            });

            viewModel.DosageCount = 1;

            // Act
            viewModel.Discount = 0.8m; // 8折

            // Assert
            Assert.Equal(500m, viewModel.TotalPrice);
            Assert.Equal(400m, viewModel.DiscountedPrice);
            Assert.Equal("8.0折", viewModel.DiscountText);
        }
    }
}