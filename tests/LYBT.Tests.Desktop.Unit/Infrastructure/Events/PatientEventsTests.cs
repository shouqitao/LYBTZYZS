using FluentAssertions;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Tests.Events;

/// <summary>
/// PatientEvents单元测试
/// OpenSpec: unify-event-system (EVENT-006)
/// </summary>
public class PatientEventsTests
{
    private readonly EventAggregator _eventAggregator;

    public PatientEventsTests()
    {
        _eventAggregator = new EventAggregator();
    }

    #region CreatedEvent测试

    /// <summary>
    /// 测试：CreatedEvent可正常订阅和发布
    /// </summary>
    [Fact]
    public void CreatedEvent_CanSubscribeAndPublish()
    {
        // Arrange
        PatientCreatedPayload? receivedPayload = null;
        var createdEvent = _eventAggregator.GetEvent<PatientEvents.CreatedEvent>();
        createdEvent.Subscribe(payload => receivedPayload = payload);

        var patient = new PatientDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "测试患者",
            Gender = Gender.Male,
            PhoneNumber = "13800138000"
        };

        var payload = new PatientCreatedPayload
        {
            Patient = patient,
            Source = "UnitTest"
        };

        // Act
        createdEvent.Publish(payload);

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Patient.Id.Should().Be(patient.Id);
        receivedPayload.Patient.Name.Should().Be("测试患者");
        receivedPayload.Source.Should().Be("UnitTest");
        receivedPayload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 测试：PatientCreatedPayload默认Timestamp为当前时间
    /// </summary>
    [Fact]
    public void PatientCreatedPayload_HasDefaultTimestamp()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var payload = new PatientCreatedPayload
        {
            Patient = new PatientDetailDto
            {
                Name = "测试",
                Gender = Gender.Male,
                PhoneNumber = "13800138000"
            }
        };

        // Assert
        payload.Timestamp.Should().BeOnOrAfter(beforeCreate);
        payload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region UpdatedEvent测试

    /// <summary>
    /// 测试：UpdatedEvent可正常订阅和发布
    /// </summary>
    [Fact]
    public void UpdatedEvent_CanSubscribeAndPublish()
    {
        // Arrange
        PatientUpdatedPayload? receivedPayload = null;
        var updatedEvent = _eventAggregator.GetEvent<PatientEvents.UpdatedEvent>();
        updatedEvent.Subscribe(payload => receivedPayload = payload);

        var patient = new PatientDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "更新后的患者",
            Gender = Gender.Female,
            PhoneNumber = "13900139000"
        };

        var payload = new PatientUpdatedPayload
        {
            Patient = patient
        };

        // Act
        updatedEvent.Publish(payload);

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Patient.Name.Should().Be("更新后的患者");
        receivedPayload.Patient.Gender.Should().Be(Gender.Female);
    }

    #endregion

    #region SelectedEvent测试

    /// <summary>
    /// 测试：SelectedEvent可正常订阅和发布
    /// </summary>
    [Fact]
    public void SelectedEvent_CanSubscribeAndPublish()
    {
        // Arrange
        PatientSelectedPayload? receivedPayload = null;
        var selectedEvent = _eventAggregator.GetEvent<PatientEvents.SelectedEvent>();
        selectedEvent.Subscribe(payload => receivedPayload = payload);

        var payload = new PatientSelectedPayload
        {
            PatientId = Guid.NewGuid(),
            PatientName = "选中的患者",
            Gender = Gender.Male,
            Age = 35,
            PhoneNumber = "13800138000",
            AllergyHistory = "无",
            SelectedAt = DateTime.UtcNow
        };

        // Act
        selectedEvent.Publish(payload);

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.PatientName.Should().Be("选中的患者");
        receivedPayload.Age.Should().Be(35);
    }

    #endregion

    #region 多订阅者测试

    /// <summary>
    /// 测试：多个订阅者都能接收到事件
    /// </summary>
    [Fact]
    public void PatientEvents_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var receivedCount = 0;
        var createdEvent = _eventAggregator.GetEvent<PatientEvents.CreatedEvent>();
        createdEvent.Subscribe(_ => receivedCount++);
        createdEvent.Subscribe(_ => receivedCount++);

        var payload = new PatientCreatedPayload
        {
            Patient = new PatientDetailDto
            {
                Name = "测试",
                Gender = Gender.Male,
                PhoneNumber = "13800138000"
            }
        };

        // Act
        createdEvent.Publish(payload);

        // Assert
        receivedCount.Should().Be(2);
    }

    /// <summary>
    /// 测试：取消订阅后不再接收事件
    /// </summary>
    [Fact]
    public void PatientEvents_Unsubscribe_NoLongerReceivesEvent()
    {
        // Arrange
        var receivedCount = 0;
        var updatedEvent = _eventAggregator.GetEvent<PatientEvents.UpdatedEvent>();
        var token = updatedEvent.Subscribe(_ => receivedCount++);

        var payload = new PatientUpdatedPayload
        {
            Patient = new PatientDetailDto
            {
                Name = "测试",
                Gender = Gender.Male,
                PhoneNumber = "13800138000"
            }
        };

        // Act
        updatedEvent.Publish(payload); // 第一次发布
        token.Dispose();
        updatedEvent.Publish(payload); // 第二次发布

        // Assert
        receivedCount.Should().Be(1); // 只接收到第一次
    }

    #endregion
}
