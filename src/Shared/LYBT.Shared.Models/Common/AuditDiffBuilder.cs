using System.Text.Json;

namespace LYBT.Shared.Models.Common;

/// <summary>
/// 审计差异构建器（T3.2）
/// 统一 Capture(old,new).BuildAuditLog()，替代 MedicalCase/Formula 2 处手写 diff
/// </summary>
public sealed class AuditDiffBuilder
{
    private readonly Dictionary<string, (string? Old, string? New)> _changed = new();

    public AuditDiffBuilder Capture(string field, string? oldVal, string? newVal)
    {
        if (!string.Equals(oldVal, newVal))
            _changed[field] = (oldVal, newVal);
        return this;
    }

    public bool HasChanges => _changed.Count > 0;

    public (string ChangedFields, string OldValues, string NewValues) Build()
    {
        var changedFields = string.Join(",", _changed.Keys);
        var oldValues = JsonSerializer.Serialize(_changed.ToDictionary(k => k.Key, v => v.Value.Old));
        var newValues = JsonSerializer.Serialize(_changed.ToDictionary(k => k.Key, v => v.Value.New));
        return (changedFields, oldValues, newValues);
    }
}
