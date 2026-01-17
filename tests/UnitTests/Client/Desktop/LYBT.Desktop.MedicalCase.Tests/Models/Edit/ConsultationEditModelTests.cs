using LYBT.Desktop.MedicalCase.Models.Edit;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Models.Edit;

/// <summary>
/// ConsultationEditModel 单元测试
/// OpenSpec: unify-control-data-binding
/// </summary>
public class ConsultationEditModelTests
{
    [Fact]
    public void DefaultValues_AreNull()
    {
        var model = new ConsultationEditModel();
        Assert.Null(model.PresentIllness);
        Assert.Null(model.TongueDiagnosis);
        Assert.Null(model.PulseDiagnosis);
        Assert.Null(model.TcmDiagnosis);
    }

    [Fact]
    public void IsValid_FalseWhenTcmDiagnosisEmpty()
    {
        var model = new ConsultationEditModel { TcmDiagnosis = null };
        Assert.False(model.IsValid);
    }

    [Fact]
    public void IsValid_TrueWhenTcmDiagnosisSet()
    {
        var model = new ConsultationEditModel { TcmDiagnosis = "肝郁气滞" };
        Assert.True(model.IsValid);
    }

    [Fact]
    public void Reset_ClearsAllFields()
    {
        var model = new ConsultationEditModel
        {
            PresentIllness = "头痛三天",
            TongueDiagnosis = "舌红苔薄",
            PulseDiagnosis = "弦脉",
            TcmDiagnosis = "肝阳上亢"
        };
        model.Reset();
        Assert.Null(model.PresentIllness);
        Assert.Null(model.TongueDiagnosis);
        Assert.Null(model.PulseDiagnosis);
        Assert.Null(model.TcmDiagnosis);
    }

    [Fact]
    public void PropertyChanged_RaisedOnSet()
    {
        var model = new ConsultationEditModel();
        var changedProperties = new List<string?>();
        model.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        model.TcmDiagnosis = "气血两虚";

        Assert.Contains("TcmDiagnosis", changedProperties);
        Assert.Contains("IsValid", changedProperties);
    }
}
