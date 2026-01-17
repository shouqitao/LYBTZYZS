using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Desktop.MedicalCase.Models.Edit;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Models.Edit;

/// <summary>
/// PrescriptionEditModel 单元测试
/// OpenSpec: unify-control-data-binding
/// </summary>
public class PrescriptionEditModelTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var model = new PrescriptionEditModel();
        Assert.Equal(7, model.DosageCount);
        Assert.Equal("水煎服，一日一剂，分早晚两次温服", model.Usage);
        Assert.Null(model.Advice);
        Assert.Null(model.Remark);
    }

    [Fact]
    public void IsValid_FalseWhenNoItems()
    {
        var model = new PrescriptionEditModel();
        Assert.False(model.IsValid);
    }

    [Fact]
    public void IsValid_TrueWhenHasItems()
    {
        var model = new PrescriptionEditModel();
        model.Items.Add(new HerbItemDto { HerbId = Guid.NewGuid() });
        Assert.True(model.IsValid);
    }

    [Fact]
    public void TotalPrice_CalculatesCorrectly()
    {
        var model = new PrescriptionEditModel
        {
            SingleDosePrice = 50.5m,
            DosageCount = 3
        };
        Assert.Equal(151.5m, model.TotalPrice);
    }

    [Fact]
    public void Reset_ClearsAllFields()
    {
        var model = new PrescriptionEditModel
        {
            DosageCount = 14,
            Usage = "特殊用法",
            Advice = "特殊医嘱",
            Remark = "备注内容",
            SingleDosePrice = 100m
        };
        model.Items.Add(new HerbItemDto { HerbId = Guid.NewGuid() });

        model.Reset();

        Assert.Equal(7, model.DosageCount);
        Assert.Equal("水煎服，一日一剂，分早晚两次温服", model.Usage);
        Assert.Null(model.Advice);
        Assert.Null(model.Remark);
        Assert.Equal(0, model.SingleDosePrice);
        Assert.Empty(model.Items);
    }

    [Fact]
    public void PropertyChanged_RaisedOnDosageCountChange()
    {
        var model = new PrescriptionEditModel();
        var changedProperties = new List<string?>();
        model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        model.DosageCount = 14;

        Assert.Contains("DosageCount", changedProperties);
        Assert.Contains("TotalPrice", changedProperties);
    }

    [Fact]
    public void PropertyChanged_RaisedOnSingleDosePriceChange()
    {
        var model = new PrescriptionEditModel();
        var changedProperties = new List<string?>();
        model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        model.SingleDosePrice = 100m;

        Assert.Contains("SingleDosePrice", changedProperties);
        Assert.Contains("TotalPrice", changedProperties);
    }
}
