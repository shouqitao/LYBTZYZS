using LYBT.Desktop.Patients.Models.Display;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// PatientDetailDisplayModel 单元测试
/// OpenSpec: unify-control-data-binding
/// </summary>
public class PatientDetailDisplayModelTests
{
    [Fact]
    public void AgeDisplay_ReturnsFormattedAge()
    {
        var model = new PatientDetailDisplayModel
        {
            Age = 35
        };
        Assert.Equal("35岁", model.AgeDisplay);
    }

    [Fact]
    public void AgeDisplay_ReturnsUnknownWhenNull()
    {
        var model = new PatientDetailDisplayModel
        {
            Age = null
        };
        Assert.Equal("未知", model.AgeDisplay);
    }

    [Fact]
    public void GenderDisplay_ReturnsChineseText()
    {
        var male = new PatientDetailDisplayModel { Gender = Gender.Male };
        var female = new PatientDetailDisplayModel { Gender = Gender.Female };
        var unknown = new PatientDetailDisplayModel { Gender = Gender.Unknown };

        Assert.Equal("男", male.GenderDisplay);
        Assert.Equal("女", female.GenderDisplay);
        Assert.Equal("未知", unknown.GenderDisplay);
    }

    [Fact]
    public void Summary_CombinesBasicInfo()
    {
        var model = new PatientDetailDisplayModel
        {
            Name = "张三",
            Gender = Gender.Male,
            Age = 45
        };
        Assert.Equal("张三 | 男 | 45岁", model.Summary);
    }


}
