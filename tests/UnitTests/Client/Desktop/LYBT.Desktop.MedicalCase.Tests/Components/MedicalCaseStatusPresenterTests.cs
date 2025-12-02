using FluentAssertions;
using LYBT.Desktop.MedicalCase.Components;
using System.Windows.Media;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Components
{
    /// <summary>
    /// MedicalCaseStatusPresenter单元测试
    /// OpenSpec: refactor-viewmodel-layer - Phase 5.1.5
    /// </summary>
    public class MedicalCaseStatusPresenterTests
    {
        private readonly MedicalCaseStatusPresenter _sut;

        // 测试用颜色常量
        private static readonly Color GreenColor = Color.FromRgb(76, 175, 80);
        private static readonly Color OrangeColor = Color.FromRgb(255, 152, 0);
        private static readonly Color GrayColor = Color.FromRgb(158, 158, 158);

        public MedicalCaseStatusPresenterTests()
        {
            _sut = new MedicalCaseStatusPresenter();
        }

        #region 初始状态测试

        [Fact]
        public void InitialState_ConsultationStatusText_ShouldBeUnfinished()
        {
            _sut.ConsultationStatusText.Should().Be("未完成");
        }

        [Fact]
        public void InitialState_ConsultationStatusColor_ShouldBeOrange()
        {
            var brush = _sut.ConsultationStatusColor as SolidColorBrush;
            brush.Should().NotBeNull();
            brush!.Color.Should().Be(OrangeColor);
        }

        [Fact]
        public void InitialState_ShowPrescriptionStatus_ShouldBeFalse()
        {
            _sut.ShowPrescriptionStatus.Should().BeFalse();
        }

        [Fact]
        public void InitialState_PrescriptionStatusText_ShouldBeWaitingDiagnosis()
        {
            _sut.PrescriptionStatusText.Should().Be("待诊断");
        }

        [Fact]
        public void InitialState_CanPrintPrescription_ShouldBeFalse()
        {
            _sut.CanPrintPrescription.Should().BeFalse();
        }

        [Fact]
        public void InitialState_CanComplete_ShouldBeFalse()
        {
            _sut.CanComplete.Should().BeFalse();
        }

        #endregion

        #region UpdateConsultationStatus 测试

        [Fact]
        public void UpdateConsultationStatus_WhenCompleted_ShouldSetGreenColorAndCompletedText()
        {
            // Act
            _sut.UpdateConsultationStatus(true);

            // Assert
            _sut.ConsultationStatusText.Should().Be("已完成");
            var brush = _sut.ConsultationStatusColor as SolidColorBrush;
            brush.Should().NotBeNull();
            brush!.Color.Should().Be(GreenColor);
        }

        [Fact]
        public void UpdateConsultationStatus_WhenNotCompleted_ShouldSetOrangeColorAndUnfinishedText()
        {
            // Arrange - 先设置为完成
            _sut.UpdateConsultationStatus(true);

            // Act - 再设置为未完成
            _sut.UpdateConsultationStatus(false);

            // Assert
            _sut.ConsultationStatusText.Should().Be("未完成");
            var brush = _sut.ConsultationStatusColor as SolidColorBrush;
            brush.Should().NotBeNull();
            brush!.Color.Should().Be(OrangeColor);
        }

        #endregion

        #region UpdatePrescriptionStatus 测试

        [Fact]
        public void UpdatePrescriptionStatus_WhenCompleted_ShouldSetGreenColorAndCompletedText()
        {
            // Act
            _sut.UpdatePrescriptionStatus(true);

            // Assert
            _sut.ShowPrescriptionStatus.Should().BeTrue();
            _sut.PrescriptionStatusText.Should().Be("已完成");
            _sut.PrescriptionStatusSummary.Should().Be("已开方");

            var bgBrush = _sut.PrescriptionStatusBackground as SolidColorBrush;
            bgBrush.Should().NotBeNull();
            bgBrush!.Color.Should().Be(GreenColor);

            var summaryBrush = _sut.PrescriptionStatusSummaryColor as SolidColorBrush;
            summaryBrush.Should().NotBeNull();
            summaryBrush!.Color.Should().Be(GreenColor);
        }

        [Fact]
        public void UpdatePrescriptionStatus_WhenNotCompleted_ShouldSetGrayColorAndDefaultText()
        {
            // Act
            _sut.UpdatePrescriptionStatus(false);

            // Assert
            _sut.ShowPrescriptionStatus.Should().BeTrue();
            _sut.PrescriptionStatusText.Should().Be("待开方");
            _sut.PrescriptionStatusSummary.Should().Be("待开方");

            var bgBrush = _sut.PrescriptionStatusBackground as SolidColorBrush;
            bgBrush.Should().NotBeNull();
            bgBrush!.Color.Should().Be(GrayColor);
        }

        [Fact]
        public void UpdatePrescriptionStatus_WithCustomText_ShouldUseCustomText()
        {
            // Act
            _sut.UpdatePrescriptionStatus(false, "无需开方");

            // Assert
            _sut.PrescriptionStatusText.Should().Be("无需开方");
            _sut.PrescriptionStatusSummary.Should().Be("无需开方");
        }

        #endregion

        #region Reset 测试

        [Fact]
        public void Reset_ShouldRestoreAllPropertiesToInitialState()
        {
            // Arrange - 修改所有属性
            _sut.UpdateConsultationStatus(true);
            _sut.UpdatePrescriptionStatus(true);
            _sut.CanPrintPrescription = true;
            _sut.CanComplete = true;

            // Act
            _sut.Reset();

            // Assert
            _sut.ConsultationStatusText.Should().Be("未完成");
            _sut.ShowPrescriptionStatus.Should().BeFalse();
            _sut.PrescriptionStatusText.Should().Be("待诊断");
            _sut.PrescriptionStatusSummary.Should().Be("待开方");
            _sut.CanPrintPrescription.Should().BeFalse();
            _sut.CanComplete.Should().BeFalse();
        }

        #endregion

        #region OnConsultationCompleted 测试

        [Fact]
        public void OnConsultationCompleted_WhenNeedsPrescription_ShouldSetWaitingPrescriptionStatus()
        {
            // Act
            _sut.OnConsultationCompleted(needsPrescription: true);

            // Assert
            _sut.ConsultationStatusText.Should().Be("已完成");
            _sut.PrescriptionStatusText.Should().Be("待开方");
            _sut.CanComplete.Should().BeFalse();
        }

        [Fact]
        public void OnConsultationCompleted_WhenNoPrescriptionNeeded_ShouldSetNoPrescriptionStatus()
        {
            // Act
            _sut.OnConsultationCompleted(needsPrescription: false);

            // Assert
            _sut.ConsultationStatusText.Should().Be("已完成");
            _sut.PrescriptionStatusText.Should().Be("无需开方");
            _sut.CanComplete.Should().BeTrue();
        }

        #endregion

        #region OnPrescriptionCompleted 测试

        [Fact]
        public void OnPrescriptionCompleted_ShouldEnablePrintAndComplete()
        {
            // Act
            _sut.OnPrescriptionCompleted();

            // Assert
            _sut.PrescriptionStatusText.Should().Be("已完成");
            _sut.PrescriptionStatusSummary.Should().Be("已开方");
            _sut.CanPrintPrescription.Should().BeTrue();
            _sut.CanComplete.Should().BeTrue();
        }

        #endregion

        #region PropertyChanged 测试

        [Fact]
        public void ConsultationStatusText_WhenChanged_ShouldRaisePropertyChanged()
        {
            // Arrange
            var propertyChangedRaised = false;
            _sut.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MedicalCaseStatusPresenter.ConsultationStatusText))
                    propertyChangedRaised = true;
            };

            // Act
            _sut.ConsultationStatusText = "测试";

            // Assert
            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void CanComplete_WhenChanged_ShouldRaisePropertyChanged()
        {
            // Arrange
            var propertyChangedRaised = false;
            _sut.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MedicalCaseStatusPresenter.CanComplete))
                    propertyChangedRaised = true;
            };

            // Act
            _sut.CanComplete = true;

            // Assert
            propertyChangedRaised.Should().BeTrue();
        }

        #endregion
    }
}
