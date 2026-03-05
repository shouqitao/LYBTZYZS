using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

public class MedicalCaseChangeTrackerTests
{
    private static MedicalCaseDetailDto CreateTestDto(
        string remark = "test",
        string? illness = "headache",
        int dosageCount = 7)
    {
        return new MedicalCaseDetailDto
        {
            Id = Guid.NewGuid(),
            CaseNumber = "MC-001",
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Remark = remark,
            Consultation = new ConsultationDetailDto
            {
                PresentIllness = illness,
                TongueDiagnosis = "red",
                PulseDiagnosis = "rapid",
                TcmDiagnosis = "wind-heat"
            },
            Prescription = new PrescriptionDetailDto
            {
                DosageCount = dosageCount,
                Usage = "水煎服",
                Advice = "饭后服用",
                Remark = "test"
            }
        };
    }

    [Fact]
    public void HasChanges_returns_false_when_no_baseline()
    {
        var tracker = new MedicalCaseChangeTracker();
        Assert.False(tracker.HasChanges(CreateTestDto()));
    }

    [Fact]
    public void HasChanges_returns_false_when_unchanged()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        tracker.SetBaseline(dto);
        Assert.False(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_remark_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto(remark: "original");
        tracker.SetBaseline(dto);
        dto.Remark = "modified";
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_consultation_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto(illness: "headache");
        tracker.SetBaseline(dto);
        dto.Consultation!.PresentIllness = "fever";
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_prescription_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto(dosageCount: 7);
        tracker.SetBaseline(dto);
        dto.Prescription!.DosageCount = 14;
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_case_status_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        tracker.SetBaseline(dto);
        dto.CaseStatus = MedicalCaseStatus.Completed; // Changed status
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void ClearBaseline_resets_tracking()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        tracker.SetBaseline(dto);
        dto.Remark = "changed";
        tracker.ClearBaseline();
        Assert.False(tracker.HasChanges(dto));
    }

    [Fact]
    public void SetBaseline_deep_copies_so_original_mutations_detected()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto(remark: "original");
        tracker.SetBaseline(dto);
        dto.Remark = "mutated"; // Mutate after baseline
        Assert.True(tracker.HasChanges(dto)); // Detected because baseline was deep-copied
    }

    [Fact]
    public void HasChanges_returns_false_when_current_is_null()
    {
        var tracker = new MedicalCaseChangeTracker();
        tracker.SetBaseline(CreateTestDto());
        Assert.False(tracker.HasChanges(null));
    }

    [Fact]
    public void HasChanges_detects_consultation_null_to_non_null()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        dto.Consultation = null;
        tracker.SetBaseline(dto);
        dto.Consultation = new ConsultationDetailDto { PresentIllness = "new" };
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_prescription_usage_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        tracker.SetBaseline(dto);
        dto.Prescription!.Usage = "changed usage";
        Assert.True(tracker.HasChanges(dto));
    }

    [Fact]
    public void HasChanges_detects_prescription_discount_change()
    {
        var tracker = new MedicalCaseChangeTracker();
        var dto = CreateTestDto();
        tracker.SetBaseline(dto);
        dto.Prescription!.Discount = 0.5m;
        Assert.True(tracker.HasChanges(dto));
    }
}
