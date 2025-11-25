using System.Data;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者导入数据映射器 - 负责Excel数据到DTO的转换和验证
/// Issue #1790: 从PatientImportWizardViewModel提取数据映射逻辑(~100行)
/// </summary>
public class PatientImportDataMapper
{
    /// <summary>
    /// 从DataRow创建PatientInputDto
    /// Issue #1789: 从ImportWorker_DoWork提取，封装DTO创建逻辑
    /// Issue #2240: 改为读取"出生日期"列，不再从"年龄"反算
    /// </summary>
    public PatientInputDto CreatePatientDtoFromRow(DataRow row)
    {
        var name = row["姓名"]?.ToString()?.Trim() ?? string.Empty;
        var gender = row["性别"]?.ToString()?.Trim() ?? string.Empty;

        // Issue #2240: 读取出生日期列，如果有"年龄"列则忽略
        DateTime? birthDate = null;
        if (row.Table.Columns.Contains("出生日期"))
        {
            birthDate = ParseBirthDate(row["出生日期"]?.ToString());
        }
        else if (row.Table.Columns.Contains("年龄"))
        {
            // 兼容性处理：如果Excel只有"年龄"列但没有"出生日期"列，使用年龄反算
            // 注意：这是临时兼容措施，建议更新Excel模板为"出生日期"列
            var age = ParseAge(row["年龄"]?.ToString()) ?? 0;
            birthDate = age > 0 ? DateTime.Today.AddYears(-age) : null;
        }

        return new PatientInputDto
        {
            Name = name,
            Gender = ParseGender(gender),
            BirthDate = birthDate,
            PhoneNumber = row["电话"]?.ToString()?.Trim(),
            IdNumber = row["证件号"]?.ToString()?.Trim(),
            Address = row["地址"]?.ToString()?.Trim(),
            AllergyHistory = row["过敏史"]?.ToString()?.Trim()
        };
    }

    /// <summary>
    /// 检查导入行是否为空
    /// Issue #1789: 从ImportWorker_DoWork提取，封装空行检查逻辑
    /// </summary>
    public bool IsImportRowEmpty(DataRow row, DataColumnCollection columns)
    {
        foreach (DataColumn col in columns)
        {
            if (!string.IsNullOrWhiteSpace(row[col]?.ToString()))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 验证导入行的必需字段
    /// Issue #1789: 从ImportWorker_DoWork提取，封装必需字段验证逻辑
    /// </summary>
    public string? ValidateImportRequiredFields(DataRow row, int rowIndex)
    {
        var name = row["姓名"]?.ToString()?.Trim();
        var gender = row["性别"]?.ToString()?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            return $"第{rowIndex + 2}行：姓名不能为空";
        }

        if (string.IsNullOrEmpty(gender) || (gender != "男" && gender != "女" && gender != "未知"))
        {
            return $"第{rowIndex + 2}行 ({name})：性别格式错误，应为'男'、'女'或'未知'";
        }

        return null;
    }

    /// <summary>
    /// 解析性别字符串为枚举
    /// Issue #1790: 从PatientImportWizardViewModel提取
    /// </summary>
    private Gender ParseGender(string? genderText)
    {
        return genderText?.Trim() switch
        {
            "男" => Gender.Male,
            "女" => Gender.Female,
            "未知" => Gender.Unknown,
            _ => Gender.Unknown
        };
    }

    /// <summary>
    /// 解析年龄字符串为整数
    /// Issue #1790: 从PatientImportWizardViewModel提取
    /// </summary>
    private int? ParseAge(string? ageText)
    {
        if (int.TryParse(ageText, out var age) && age > 0 && age <= 150)
        {
            return age;
        }

        return null;
    }

    /// <summary>
    /// 解析出生日期字符串为DateTime
    /// Issue #2240: 新增，支持从Excel读取出生日期
    /// </summary>
    private DateTime? ParseBirthDate(string? birthDateText)
    {
        if (string.IsNullOrWhiteSpace(birthDateText))
        {
            return null;
        }

        // 尝试解析日期（支持多种格式）
        if (DateTime.TryParse(birthDateText, out var birthDate))
        {
            // 验证日期合理性（不能是未来日期，不能超过150年前）
            if (birthDate <= DateTime.Today && birthDate >= DateTime.Today.AddYears(-150))
            {
                return birthDate;
            }
        }

        return null;
    }
}
