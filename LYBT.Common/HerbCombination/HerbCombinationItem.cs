namespace LYBT.Common.HerbCombination;

/// <summary>
/// Represents a single herb entry within a combination.
/// </summary>
public class HerbCombinationItem
{
    /// <summary>
    /// Linked herb identifier from the master Herbs table.
    /// </summary>
    public string? HerbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? Dosage { get; set; }

    public string? Unit { get; set; }

    public string? Usage { get; set; }

    public string? Remark { get; set; }
}
