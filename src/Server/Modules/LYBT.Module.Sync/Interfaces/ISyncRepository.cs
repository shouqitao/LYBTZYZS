using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;

namespace LYBT.Module.Sync.Interfaces;

/// <summary>
/// 同步模块数据访问接口 - 封装所有跨实体的同步数据库操作
/// </summary>
public interface ISyncRepository
{
    // ── 元数据查询 (IgnoreQueryFilters + AsNoTracking, 含软删除) ──────────────

    Task<List<Herb>> GetAllHerbsIncludingDeletedAsync(CancellationToken ct = default);
    Task<List<Patient>> GetAllPatientsIncludingDeletedAsync(CancellationToken ct = default);
    Task<List<Formula>> GetAllFormulasWithHerbsIncludingDeletedAsync(CancellationToken ct = default);
    Task<List<MedicalCase>> GetAllMedicalCasesIncludingDeletedAsync(CancellationToken ct = default);

    // ── 上传: 查找已追踪实体 ────────────────────────────────────────────────────

    Task<Herb?> FindHerbAsync(Guid id);
    Task<Patient?> FindPatientAsync(Guid id);
    Task<Formula?> FindFormulaWithHerbsAsync(Guid id);
    Task<MedicalCase?> FindMedicalCaseWithIncludesAsync(Guid id);

    // ── 上传: 值覆盖 ─────────────────────────────────────────────────────────

    void UpdateHerbValues(Herb existing, Herb incoming);
    void UpdatePatientValues(Patient existing, Patient incoming);
    void UpdateFormulaValues(Formula existing, Formula incoming);
    void UpdateMedicalCaseValues(MedicalCase existing, MedicalCase incoming);

    // ── 上传: 新增 ────────────────────────────────────────────────────────────

    void AddHerb(Herb herb);
    void AddPatient(Patient patient);
    void AddFormula(Formula formula);
    void AddMedicalCase(MedicalCase medicalCase);
    void RemoveFormulaHerbs(ICollection<FormulaHerbItem> herbs);
    void AddFormulaHerbItem(FormulaHerbItem item);

    // ── 下载 (AsNoTracking) ───────────────────────────────────────────────────

    Task<Herb?> GetHerbByIdNoTrackingAsync(Guid id, CancellationToken ct = default);
    Task<Patient?> GetPatientByIdNoTrackingAsync(Guid id, CancellationToken ct = default);
    Task<Formula?> GetFormulaWithHerbsByIdNoTrackingAsync(Guid id, CancellationToken ct = default);
    Task<MedicalCase?> GetMedicalCaseWithIncludesByIdNoTrackingAsync(Guid id, CancellationToken ct = default);

    // ── 软删除 (IgnoreQueryFilters, 含软删除记录) ────────────────────────────

    Task<bool> SoftDeleteHerbAsync(Guid id, CancellationToken ct = default);
    Task<bool> SoftDeletePatientAsync(Guid id, CancellationToken ct = default);
    Task<bool> SoftDeleteFormulaAsync(Guid id, CancellationToken ct = default);
    Task<bool> SoftDeleteMedicalCaseAsync(Guid id, CancellationToken ct = default);

    // ── 持久化 ────────────────────────────────────────────────────────────────

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
