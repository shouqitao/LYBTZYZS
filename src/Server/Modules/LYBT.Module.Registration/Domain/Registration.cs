using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Primitives;

namespace LYBT.Module.Registration.Domain;

/// <summary>
/// 挂号聚合根。封装挂号生命周期的业务规则和状态转换。
/// 状态机: Waiting -> InProgress -> Completed/Cancelled
/// </summary>
public class Registration : Entity, IAggregateRoot
{
    /// <summary>关联患者 ID</summary>
    [Required]
    public Guid PatientId { get; private set; }

    /// <summary>患者姓名 (冗余字段，列表展示用)</summary>
    [Required]
    [StringLength(100)]
    public string PatientName { get; private set; } = string.Empty;

    /// <summary>指派医生 ID</summary>
    [Required]
    public Guid DoctorId { get; private set; }

    /// <summary>医生姓名 (冗余字段)</summary>
    [Required]
    [StringLength(100)]
    public string DoctorName { get; private set; } = string.Empty;

    /// <summary>关联医案 ID (Waiting 状态时为 null，接诊后填入)</summary>
    public Guid? MedicalCaseId { get; private set; }

    /// <summary>挂号来源</summary>
    [Required]
    public RegistrationSource Source { get; private set; }

    /// <summary>挂号状态</summary>
    [Required]
    public RegistrationStatus Status { get; private set; }

    /// <summary>当日顺序号 (创建时自动生成)</summary>
    public int QueueNumber { get; private set; }

    /// <summary>挂号费 (元)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal RegistrationFee { get; private set; }

    /// <summary>备注</summary>
    [StringLength(500)]
    public string? Remark { get; private set; }

    /// <summary>软删除标记</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>创建时间 (UTC)</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>更新时间 (UTC)</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>创建者ID</summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>更新者ID</summary>
    public Guid? UpdatedBy { get; private set; }

    /// <summary>乐观并发控制</summary>
    [Timestamp]
    public byte[]? RowVersion { get; private set; }

    /// <summary>是否已创建医案</summary>
    public bool HasMedicalCase => MedicalCaseId.HasValue;

    private Registration() { }

    /// <summary>
    /// 创建挂号记录。
    /// Receptionist 源: Status=Waiting; Doctor 源: Status=InProgress。
    /// </summary>
    public static Registration Create(
        Guid patientId,
        string patientName,
        Guid doctorId,
        string doctorName,
        RegistrationSource source,
        int queueNumber,
        decimal registrationFee = 0,
        string? remark = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(patientName))
            throw new ArgumentException("患者姓名不能为空", nameof(patientName));
        if (string.IsNullOrWhiteSpace(doctorName))
            throw new ArgumentException("医生姓名不能为空", nameof(doctorName));

        var status = source == RegistrationSource.Doctor
            ? RegistrationStatus.InProgress
            : RegistrationStatus.Waiting;

        return new Registration
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            PatientName = patientName.Trim(),
            DoctorId = doctorId,
            DoctorName = doctorName.Trim(),
            Source = source,
            Status = status,
            QueueNumber = queueNumber,
            RegistrationFee = registrationFee,
            Remark = remark?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 接诊: Waiting -> InProgress。
    /// </summary>
    public void StartVisit()
    {
        if (Status != RegistrationStatus.Waiting)
            throw new InvalidOperationException("仅等待中的挂号记录可以接诊");

        Status = RegistrationStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 完成挂号: InProgress -> Completed。
    /// 由医案完成联动触发。
    /// </summary>
    public void Complete()
    {
        if (Status != RegistrationStatus.InProgress)
            throw new InvalidOperationException("仅接诊中的挂号记录可以完成");

        Status = RegistrationStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 取消挂号: Waiting -> Cancelled。
    /// </summary>
    public void Cancel()
    {
        if (Status != RegistrationStatus.Waiting)
            throw new InvalidOperationException("仅等待中的挂号记录可取消");

        if (MedicalCaseId.HasValue)
            throw new InvalidOperationException("挂号记录有关联医案，不允许取消");

        Status = RegistrationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 关联医案。
    /// </summary>
    public void AssignMedicalCase(Guid medicalCaseId)
    {
        MedicalCaseId = medicalCaseId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 回退到等待状态 (Receptionist 模式医案取消联动)。
    /// </summary>
    public void RevertToWaiting()
    {
        Status = RegistrationStatus.Waiting;
        MedicalCaseId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 软删除挂号记录。
    /// </summary>
    public void SoftDelete(Guid deletedBy)
    {
        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复已软删除的挂号记录。
    /// </summary>
    public void Restore(Guid restoredBy)
    {
        IsDeleted = false;
        UpdatedBy = restoredBy;
        UpdatedAt = DateTime.UtcNow;
    }
}


