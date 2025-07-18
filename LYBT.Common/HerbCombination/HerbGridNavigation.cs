namespace LYBT.Common.HerbCombination;

/// <summary>
/// Helper logic for navigating herb grid cells.
/// </summary>
public static class HerbGridNavigation
{
    /// <summary>
    /// Calculate next cell coordinates.
    /// </summary>
    public static (int row, int col, bool newRow) NextCell(int rowCount, int colCount, int row, int col, bool reverse)
    {
        if (!reverse)
        {
            if (col == colCount - 1)
            {
                if (row == rowCount - 1)
                    return (rowCount, 0, true);
                return (row + 1, 0, false);
            }
            return (row, col + 1, false);
        }
        else
        {
            if (col == 0)
            {
                if (row == 0) return (0, 0, false);
                return (row - 1, colCount - 1, false);
            }
            return (row, col - 1, false);
        }
    }
}
