using System.Collections.ObjectModel;
using LYBT.Common.HerbCombination;
using Xunit;

namespace LYBT.Tests.Controls;

public class HerbCombinationEditorTests {
    [Fact]
    public void NextCellAddsRowAtEnd() {
        var result = HerbGridNavigation.NextCell(1, 5, 0, 4, false);
        Assert.Equal((1,0,true), (result.row, result.col, result.newRow));
    }

    [Fact]
    public void ValidateDetectsMissingFields() {
        var vm = new HerbCombinationEditorViewModel { Mode = HerbEditorMode.Template };
        vm.Items.Add(new HerbCombinationItem());
        bool ok = vm.Validate(out var msg);
        Assert.False(ok);
        Assert.NotNull(msg);
    }
}
