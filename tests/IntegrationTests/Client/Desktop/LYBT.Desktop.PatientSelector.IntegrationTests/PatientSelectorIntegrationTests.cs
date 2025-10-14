using System;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Presentation.Components.PatientSelector;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Xunit;

namespace LYBT.Desktop.PatientSelector.IntegrationTests
{
    /// <summary>
    /// PatientSelector组件集成测试 - 简化版本（基于ViewModel测试）
    /// </summary>
    public class PatientSelectorIntegrationTests : IDisposable
    {
        private Mock<IEventAggregator> _mockEventAggregator;
        private Mock<PatientSelectedEvent> _mockPatientSelectedEvent;
        private PatientSelectorViewModel _viewModel;

        public PatientSelectorIntegrationTests()
        {
            // 初始化Mock对象
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockPatientSelectedEvent = new Mock<PatientSelectedEvent>();
            _mockEventAggregator.Setup(x => x.GetEvent<PatientSelectedEvent>())
                .Returns(_mockPatientSelectedEvent.Object);

            // 直接创建ViewModel进行测试（避免WPF STA线程问题）
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
        }

        [Fact]
        public void PatientSelectorViewModel_ShouldInitializeCorrectly()
        {
            // Assert
            Assert.NotNull(_viewModel);
            Assert.Equal(string.Empty, _viewModel.SearchKeyword);
            Assert.NotNull(_viewModel.SearchResults);
            Assert.Empty(_viewModel.SearchResults);
            Assert.False(_viewModel.HasNoResults);
            Assert.Null(_viewModel.SelectedPatient);
            Assert.False(_viewModel.ShowQuickCreate);
        }

        [Fact]
        public async Task PatientSelectorViewModel_ShouldHandleSearchWorkflow()
        {
            // Arrange
            var eventReceived = false;
            PatientSelectedPayload? receivedPayload = null;

            _mockPatientSelectedEvent.Setup(x => x.Publish(It.IsAny<PatientSelectedPayload>()))
                .Callback<PatientSelectedPayload>(payload =>
                {
                    eventReceived = true;
                    receivedPayload = payload;
                });

            // Act
            _viewModel.SearchKeyword = "张三";

            // 等待搜索防抖完成
            await Task.Delay(400);

            // 模拟选择患者
            var mockPatient = new
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                Gender = "男",
                Age = 35,
                PhoneNumber = "13800138000"
            };

            _viewModel.SelectPatientCommand.Execute(mockPatient);

            // Assert
            Assert.True(eventReceived);
            Assert.NotNull(receivedPayload);
            Assert.Equal("张三", receivedPayload.PatientName);
            Assert.Equal("男", receivedPayload.Gender);
            Assert.Equal("13800138000", receivedPayload.PhoneNumber);
        }

        [Fact]
        public async Task PatientSelectorViewModel_ShouldHandleQuickCreateWorkflow()
        {
            // Arrange
            var eventReceived = false;
            PatientSelectedPayload? receivedPayload = null;

            _mockPatientSelectedEvent.Setup(x => x.Publish(It.IsAny<PatientSelectedPayload>()))
                .Callback<PatientSelectedPayload>(payload =>
                {
                    eventReceived = true;
                    receivedPayload = payload;
                });

            // Act
            _viewModel.ToggleQuickCreateCommand.Execute();

            // 填写新患者信息
            _viewModel.NewPatientName = "李四";
            _viewModel.NewPatientGender = "女";
            _viewModel.NewPatientPhone = "13900139000123";

            // 创建患者
            _viewModel.QuickCreateCommand.Execute();
            await Task.Delay(600); // 等待异步创建完成

            // Assert
            Assert.True(eventReceived);
            Assert.NotNull(receivedPayload);
            Assert.Equal("李四", receivedPayload.PatientName);
            Assert.Equal("女", receivedPayload.Gender);
            Assert.Equal("13900139000123", receivedPayload.PhoneNumber);
            Assert.False(_viewModel.ShowQuickCreate); // 应该自动关闭创建面板
        }

        [Fact]
        public void PatientSelectorViewModel_ShouldValidateInput()
        {
            // Arrange & Act
            _viewModel.ToggleQuickCreateCommand.Execute();

            // 测试空输入
            _viewModel.NewPatientName = "";
            _viewModel.NewPatientGender = "";
            _viewModel.NewPatientPhone = "";

            // Assert
            Assert.False(_viewModel.QuickCreateCommand.CanExecute());

            // 测试手机号太短
            _viewModel.NewPatientName = "测试患者";
            _viewModel.NewPatientGender = "男";
            _viewModel.NewPatientPhone = "123";

            // Assert
            Assert.False(_viewModel.QuickCreateCommand.CanExecute());

            // 测试有效输入
            _viewModel.NewPatientPhone = "13800138000123";

            // Assert
            Assert.True(_viewModel.QuickCreateCommand.CanExecute());
        }

        [Fact]
        public void PatientSelectorViewModel_ShouldHandleErrorStates()
        {
            // Arrange & Act
            _viewModel.ErrorMessage = "测试错误消息";

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Equal("测试错误消息", _viewModel.ErrorMessage);

            // 清除错误
            _viewModel.ErrorMessage = "";

            // Assert
            Assert.False(_viewModel.HasError);
        }

        [Fact]
        public async Task PatientSelectorViewModel_ShouldHandleLoadingState()
        {
            // Arrange & Act
            _viewModel.SearchKeyword = "测试搜索";
            _viewModel.SearchCommand.Execute();

            // Assert - 应该显示加载状态
            Assert.True(_viewModel.IsLoading);

            // 等待搜索完成
            await Task.Delay(600);

            // Assert - 加载状态应该结束
            Assert.False(_viewModel.IsLoading);
        }

        [Fact]
        public void PatientSelectorViewModel_ShouldShowNoResultsWhenAppropriate()
        {
            // Arrange & Act
            _viewModel.SearchKeyword = "不存在的患者";
            _viewModel.SearchResults.Clear();

            // Assert
            Assert.True(_viewModel.HasNoResults);

            // 当有结果时
            _viewModel.SearchResults.Add(new { Id = Guid.NewGuid(), Name = "测试患者" });

            // Assert
            Assert.False(_viewModel.HasNoResults);
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }
    }
}