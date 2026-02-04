using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;

namespace LYBT.Desktop.LocalData.Helpers;

/// <summary>
/// Checksum 计算辅助类（LocalData 版本）
/// 用于计算实体的 SHA256 哈希值，排除审计字段
/// 注意：必须与服务器端 LYBT.Module.Sync.Services.ChecksumHelper 保持完全一致
/// </summary>
public static class ChecksumHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// 计算 Herb 实体的 Checksum
    /// 仅包含业务字段，排除审计字段（CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, RowVersion）
    /// </summary>
    public static string ComputeHerbChecksum(Herb herb)
    {
        var data = new
        {
            herb.Id,
            herb.Name,
            herb.PinYinCode,
            herb.Category,
            herb.Origin,
            herb.Spec,
            herb.Unit,
            herb.Price,
            herb.CostPrice,
            herb.Effect,
            herb.Usage,
            herb.Remark,
            herb.Status,
            herb.IsDeleted
        };

        return ComputeHash(data);
    }

    /// <summary>
    /// 计算 Patient 实体的 Checksum
    /// </summary>
    public static string ComputePatientChecksum(Patient patient)
    {
        var data = new
        {
            patient.Id,
            patient.Name,
            patient.PinYinCode,
            patient.Gender,
            patient.BirthDate,
            patient.IdNumber,
            patient.PhoneNumber,
            patient.Address,
            patient.AllergyHistory,
            patient.MedicalHistory,
            patient.Status,
            patient.DisableReason,
            patient.IsDeleted
        };

        return ComputeHash(data);
    }

    /// <summary>
    /// 计算 Formula 实体的 Checksum
    /// </summary>
    public static string ComputeFormulaChecksum(Formula formula)
    {
        // FormulaHerbItems 按 HerbId 排序以确保一致性
        var sortedHerbs = formula.Herbs?
            .OrderBy(h => h.HerbId)
            .ThenBy(h => h.HerbName)
            .Select(h => new
            {
                h.HerbId,
                h.HerbName,
                h.Dosage,
                h.Unit,
                h.Remark
            })
            .ToList();

        var data = new
        {
            formula.Id,
            formula.Name,
            formula.Category,
            formula.Effect,
            formula.Indication,
            formula.Usage,
            formula.Remark,
            formula.Property,
            formula.Status,
            formula.FormulaType,
            formula.IsDeleted,
            Herbs = sortedHerbs
        };

        return ComputeHash(data);
    }

    /// <summary>
    /// 根据实体类型计算 Checksum
    /// </summary>
    public static string ComputeChecksum(object entity, string entityType)
    {
        return entityType switch
        {
            "Herb" => ComputeHerbChecksum((Herb)entity),
            "Patient" => ComputePatientChecksum((Patient)entity),
            "Formula" => ComputeFormulaChecksum((Formula)entity),
            _ => throw new ArgumentException($"不支持的实体类型: {entityType}")
        };
    }

    /// <summary>
    /// 计算对象的 SHA256 哈希
    /// </summary>
    private static string ComputeHash(object data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
