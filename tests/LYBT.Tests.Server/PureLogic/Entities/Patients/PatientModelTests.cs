using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Entities.Patients
{
    /// <summary>
    /// Patient实体单元测试 - 测试患者实体的所有属性和默认值
    /// </summary>
    public class PatientModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var patient = new Patient();

            // Assert
            patient.Id.Should().NotBe(Guid.Empty); // BaseEntity 使用 Guid.NewGuid()
            patient.Name.Should().Be(string.Empty);
            patient.PinYinCode.Should().BeNull();
            patient.Gender.Should().Be(Gender.Unknown);
            patient.MaritalStatus.Should().Be(0);
            patient.BirthDate.Should().BeNull();
            patient.IdType.Should().Be(0);
            patient.IdNumber.Should().BeNull();
            patient.PhoneNumber.Should().BeNull();
            patient.Address.Should().BeNull();
            patient.AllergyHistory.Should().BeNull();
            patient.BloodType.Should().Be(0);
            patient.EmergencyContactName.Should().BeNull();
            patient.EmergencyContactPhone.Should().BeNull();
            patient.EmergencyContactRelation.Should().BeNull();
            patient.Status.Should().Be(CommonStatus.Enabled);
            patient.DisableReason.Should().BeNull();
            patient.LastVisitTime.Should().BeNull();
            patient.VisitCount.Should().Be(0);
            patient.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            patient.UpdatedAt.Should().BeNull();
            patient.CreatedBy.Should().BeNull();
            patient.UpdatedBy.Should().BeNull();
            patient.RowVersion.Should().BeNull(); // RowVersion 由 EF Core 管理
            patient.Age.Should().BeNull();
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testId = Guid.NewGuid();

            // Act
            patient.Id = testId;

            // Assert
            patient.Id.Should().Be(testId);
        }

        [Fact]
        public void Name_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testName = "张三";

            // Act
            patient.Name = testName;

            // Assert
            patient.Name.Should().Be(testName);
        }

        [Fact]
        public void PinYinCode_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testPinYin = "zs";

            // Act
            patient.PinYinCode = testPinYin;

            // Assert
            patient.PinYinCode.Should().Be(testPinYin);
        }

        [Fact]
        public void Gender_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();

            // Act & Assert
            patient.Gender = Gender.Male;
            patient.Gender.Should().Be(Gender.Male);

            patient.Gender = Gender.Female;
            patient.Gender.Should().Be(Gender.Female);

            patient.Gender = Gender.Unknown;
            patient.Gender.Should().Be(Gender.Unknown);
        }

        [Fact]
        public void MaritalStatus_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const int testMaritalStatus = 1;

            // Act
            patient.MaritalStatus = testMaritalStatus;

            // Assert
            patient.MaritalStatus.Should().Be(testMaritalStatus);
        }

        [Fact]
        public void BirthDate_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testBirthDate = new DateTime(1990, 5, 15);

            // Act
            patient.BirthDate = testBirthDate;

            // Assert
            patient.BirthDate.Should().Be(testBirthDate);
        }

        [Fact]
        public void IdType_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const int testIdType = 1;

            // Act
            patient.IdType = testIdType;

            // Assert
            patient.IdType.Should().Be(testIdType);
        }

        [Fact]
        public void IdNumber_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testIdNumber = "110101199005156789";

            // Act
            patient.IdNumber = testIdNumber;

            // Assert
            patient.IdNumber.Should().Be(testIdNumber);
        }

        [Fact]
        public void PhoneNumber_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testPhoneNumber = "13800138000";

            // Act
            patient.PhoneNumber = testPhoneNumber;

            // Assert
            patient.PhoneNumber.Should().Be(testPhoneNumber);
        }

        [Fact]
        public void Address_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testAddress = "北京市朝阳区某某街道123号";

            // Act
            patient.Address = testAddress;

            // Assert
            patient.Address.Should().Be(testAddress);
        }

        [Fact]
        public void AllergyHistory_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testAllergyHistory = "对青霉素过敏";

            // Act
            patient.AllergyHistory = testAllergyHistory;

            // Assert
            patient.AllergyHistory.Should().Be(testAllergyHistory);
        }

        [Fact]
        public void BloodType_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const int testBloodType = 1; // A型血

            // Act
            patient.BloodType = testBloodType;

            // Assert
            patient.BloodType.Should().Be(testBloodType);
        }

        [Fact]
        public void EmergencyContactName_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testContactName = "李四";

            // Act
            patient.EmergencyContactName = testContactName;

            // Assert
            patient.EmergencyContactName.Should().Be(testContactName);
        }

        [Fact]
        public void EmergencyContactPhone_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testContactPhone = "13900139000";

            // Act
            patient.EmergencyContactPhone = testContactPhone;

            // Assert
            patient.EmergencyContactPhone.Should().Be(testContactPhone);
        }

        [Fact]
        public void EmergencyContactRelation_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testContactRelation = "配偶";

            // Act
            patient.EmergencyContactRelation = testContactRelation;

            // Assert
            patient.EmergencyContactRelation.Should().Be(testContactRelation);
        }

        [Fact]
        public void Status_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();

            // Act & Assert
            patient.Status = CommonStatus.Disabled;
            patient.Status.Should().Be(CommonStatus.Disabled);

            patient.Status = CommonStatus.Enabled;
            patient.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void DisableReason_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const string testDisableReason = "患者要求停止治疗";

            // Act
            patient.DisableReason = testDisableReason;

            // Assert
            patient.DisableReason.Should().Be(testDisableReason);
        }

        [Fact]
        public void LastVisitTime_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testVisitTime = new DateTime(2024, 1, 15, 10, 30, 0);

            // Act
            patient.LastVisitTime = testVisitTime;

            // Assert
            patient.LastVisitTime.Should().Be(testVisitTime);
        }

        [Fact]
        public void VisitCount_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            const int testVisitCount = 5;

            // Act
            patient.VisitCount = testVisitCount;

            // Assert
            patient.VisitCount.Should().Be(testVisitCount);
        }

        [Fact]
        public void CreatedAt_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testCreatedAt = new DateTime(2024, 1, 1, 9, 0, 0);

            // Act
            patient.CreatedAt = testCreatedAt;

            // Assert
            patient.CreatedAt.Should().Be(testCreatedAt);
        }

        [Fact]
        public void UpdatedAt_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testUpdatedAt = new DateTime(2024, 1, 2, 14, 30, 0);

            // Act
            patient.UpdatedAt = testUpdatedAt;

            // Assert
            patient.UpdatedAt.Should().Be(testUpdatedAt);
        }

        [Fact]
        public void CreatedBy_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testCreatedBy = Guid.NewGuid();

            // Act
            patient.CreatedBy = testCreatedBy;

            // Assert
            patient.CreatedBy.Should().Be(testCreatedBy);
        }

        [Fact]
        public void UpdatedBy_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testUpdatedBy = Guid.NewGuid();

            // Act
            patient.UpdatedBy = testUpdatedBy;

            // Assert
            patient.UpdatedBy.Should().Be(testUpdatedBy);
        }

        [Fact]
        public void RowVersion_PropertyCanBeSetAndGet()
        {
            // Arrange
            var patient = new Patient();
            var testRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Act
            patient.RowVersion = testRowVersion;

            // Assert
            patient.RowVersion.Should().BeEquivalentTo(testRowVersion);
        }

        [Fact]
        public void Age_CalculatedProperty_WhenBirthDateIsNull_ShouldReturnNull()
        {
            // Arrange
            var patient = new Patient { BirthDate = null };

            // Act & Assert
            patient.Age.Should().BeNull();
        }

        [Fact]
        public void Age_CalculatedProperty_WhenBirthDateIsSet_ShouldCalculateCorrectAge()
        {
            // Arrange
            var patient = new Patient();
            var birthDate = DateTime.Today.AddYears(-30); // 30岁
            patient.BirthDate = birthDate;

            // Act & Assert
            patient.Age.Should().Be(30);
        }

        [Fact]
        public void Age_CalculatedProperty_WhenBirthDateIsThisYear_ShouldCalculateCorrectAge()
        {
            // Arrange
            var patient = new Patient();
            var birthDate = DateTime.Today.AddMonths(-6); // 今年出生，6个月前
            patient.BirthDate = birthDate;

            // Act & Assert
            patient.Age.Should().Be(0);
        }

        [Fact]
        public void Age_CalculatedProperty_WhenBirthdayNotYetReached_ShouldCalculateCorrectAge()
        {
            // Arrange
            var patient = new Patient();
            var birthDate = new DateTime(DateTime.Today.Year - 25, DateTime.Today.Month + 1, DateTime.Today.Day); // 25岁，生日还未到
            patient.BirthDate = birthDate;

            // Act & Assert
            patient.Age.Should().Be(24); // 生日未到，应该是24岁
        }

        [Fact]
        public void NullableProperties_CanBeSetToNull()
        {
            // Arrange
            var patient = new Patient();

            // Act
            patient.PinYinCode = null;
            patient.BirthDate = null;
            patient.IdNumber = null;
            patient.PhoneNumber = null;
            patient.Address = null;
            patient.AllergyHistory = null;
            patient.EmergencyContactName = null;
            patient.EmergencyContactPhone = null;
            patient.EmergencyContactRelation = null;
            patient.DisableReason = null;
            patient.LastVisitTime = null;
            patient.UpdatedAt = null;
            patient.CreatedBy = null;
            patient.UpdatedBy = null;

            // Assert
            patient.PinYinCode.Should().BeNull();
            patient.BirthDate.Should().BeNull();
            patient.IdNumber.Should().BeNull();
            patient.PhoneNumber.Should().BeNull();
            patient.Address.Should().BeNull();
            patient.AllergyHistory.Should().BeNull();
            patient.EmergencyContactName.Should().BeNull();
            patient.EmergencyContactPhone.Should().BeNull();
            patient.EmergencyContactRelation.Should().BeNull();
            patient.DisableReason.Should().BeNull();
            patient.LastVisitTime.Should().BeNull();
            patient.UpdatedAt.Should().BeNull();
            patient.CreatedBy.Should().BeNull();
            patient.UpdatedBy.Should().BeNull();
        }

        [Fact]
        public void CreateCompletePatient_ShouldSetAllProperties()
        {
            // Arrange
            var patient = new Patient();
            var patientId = Guid.NewGuid();
            var createdBy = Guid.NewGuid();
            var birthDate = new DateTime(1985, 3, 20);
            var createdAt = DateTime.Now;

            // Act
            patient.Id = patientId;
            patient.Name = "王小明";
            patient.PinYinCode = "wxm";
            patient.Gender = Gender.Male;
            patient.MaritalStatus = 1;
            patient.BirthDate = birthDate;
            patient.IdType = 1;
            patient.IdNumber = "110101198503201234";
            patient.PhoneNumber = "13812345678";
            patient.Address = "北京市海淀区中关村大街1号";
            patient.AllergyHistory = "无";
            patient.BloodType = 2;
            patient.EmergencyContactName = "王小红";
            patient.EmergencyContactPhone = "13987654321";
            patient.EmergencyContactRelation = "妹妹";
            patient.Status = CommonStatus.Enabled;
            patient.VisitCount = 3;
            patient.CreatedAt = createdAt;
            patient.CreatedBy = createdBy;

            // Assert
            patient.Id.Should().Be(patientId);
            patient.Name.Should().Be("王小明");
            patient.PinYinCode.Should().Be("wxm");
            patient.Gender.Should().Be(Gender.Male);
            patient.MaritalStatus.Should().Be(1);
            patient.BirthDate.Should().Be(birthDate);
            patient.IdType.Should().Be(1);
            patient.IdNumber.Should().Be("110101198503201234");
            patient.PhoneNumber.Should().Be("13812345678");
            patient.Address.Should().Be("北京市海淀区中关村大街1号");
            patient.AllergyHistory.Should().Be("无");
            patient.BloodType.Should().Be(2);
            patient.EmergencyContactName.Should().Be("王小红");
            patient.EmergencyContactPhone.Should().Be("13987654321");
            patient.EmergencyContactRelation.Should().Be("妹妹");
            patient.Status.Should().Be(CommonStatus.Enabled);
            patient.VisitCount.Should().Be(3);
            patient.CreatedAt.Should().Be(createdAt);
            patient.CreatedBy.Should().Be(createdBy);
            patient.Age.Should().Be(DateTime.Today.Year - 1985 - (DateTime.Today < new DateTime(DateTime.Today.Year, 3, 20) ? 1 : 0));
        }
    }
}