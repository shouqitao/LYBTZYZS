using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Modules.Patients.Models;

/// <summary>
/// 患者列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用PatientDto，实现Desktop层与Shared层的解耦
/// 保持属性名与PatientDto一致，确保XAML绑定兼容
/// </summary>
public partial class PatientItem : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string gender = string.Empty;

    [ObservableProperty]
    private int? age;

    [ObservableProperty]
    private string phoneNumber = string.Empty;

    [ObservableProperty]
    private string? address;

    [ObservableProperty]
    private string? idCard;

    [ObservableProperty]
    private string? medicalHistory;

    [ObservableProperty]
    private string? allergyHistory;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? lastVisitDate;

    [ObservableProperty]
    private int visitCount;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isHighlighted;

    /// <summary>
    /// 从PatientDto创建PatientItem
    /// </summary>
    public static PatientItem FromDto(PatientDto dto)
    {
        return new PatientItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Gender = dto.Gender,
            Age = dto.Age,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            IdCard = dto.IdCard,
            MedicalHistory = dto.MedicalHistory,
            AllergyHistory = dto.AllergyHistory,
            CreatedAt = dto.CreatedAt,
            LastVisitDate = dto.LastVisitDate,
            VisitCount = dto.VisitCount
        };
    }

    /// <summary>
    /// 转换为PatientDto（用于API调用）
    /// </summary>
    public PatientDto ToDto()
    {
        return new PatientDto
        {
            Id = Id,
            Name = Name,
            Gender = Gender,
            Age = Age,
            PhoneNumber = PhoneNumber,
            Address = Address,
            IdCard = IdCard,
            MedicalHistory = MedicalHistory,
            AllergyHistory = AllergyHistory,
            CreatedAt = CreatedAt,
            LastVisitDate = LastVisitDate,
            VisitCount = VisitCount
        };
    }

    /// <summary>
    /// 从PatientDto更新当前项
    /// </summary>
    public void UpdateFromDto(PatientDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        Gender = dto.Gender;
        Age = dto.Age;
        PhoneNumber = dto.PhoneNumber;
        Address = dto.Address;
        IdCard = dto.IdCard;
        MedicalHistory = dto.MedicalHistory;
        AllergyHistory = dto.AllergyHistory;
        CreatedAt = dto.CreatedAt;
        LastVisitDate = dto.LastVisitDate;
        VisitCount = dto.VisitCount;
    }

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{Name} ({Gender}/{Age}岁)";

    /// <summary>
    /// 是否为新患者（30天内首次就诊）
    /// </summary>
    public bool IsNewPatient => CreatedAt > DateTime.Now.AddDays(-30) && VisitCount <= 1;
}