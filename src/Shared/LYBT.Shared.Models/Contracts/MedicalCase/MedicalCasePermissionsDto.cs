namespace LYBT.Shared.Models.Contracts.MedicalCase;

public class MedicalCasePermissionsDto
{
    public bool CanEdit { get; set; }
    public bool CanComplete { get; set; }
    public bool CanSuspend { get; set; }
    public bool CanCancel { get; set; }
    public bool CanDelete { get; set; }
}
