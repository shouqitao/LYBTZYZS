using System.Collections.ObjectModel;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// Edit context for MedicalCase XAML bindings.
/// Exposes the properties that MedicalCaseEditControl binds to,
/// decoupling the view from specific ViewModel implementations.
/// OpenSpec: medicalcase-frontend-unification
/// </summary>
public interface IMedicalCaseEditContext
{
    /// <summary>Consultation edit model (XAML binding target)</summary>
    ConsultationItem Consultation { get; }

    /// <summary>Prescription edit model (XAML binding target)</summary>
    PrescriptionItem Prescription { get; }

    /// <summary>All available herbs for autocomplete</summary>
    ObservableCollection<HerbListDto> AllHerbs { get; }

    /// <summary>Remark field on the medical case</summary>
    string? Remark { get; set; }
}
