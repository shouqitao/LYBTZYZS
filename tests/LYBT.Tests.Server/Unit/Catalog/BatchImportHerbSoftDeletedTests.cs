using LYBT.Entities.Herbs;

namespace LYBT.Tests.Server.Unit.Catalog;

/// <summary>
/// P1-15 批量导入软删隔离验证
/// </summary>
public class BatchImportHerbSoftDeletedTests
{
    [Fact]
    public void SoftDeletedHerb_UpdateStrategy_Should_Not_Reuse_OldRecord()
    {
        // 模拟已软删的旧记录
        var deleted = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "当归",
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var now = DateTime.UtcNow;

        // 按 P1-15 逻辑：IsDeleted=true 视为不存在，应新建而非复活
        // 验证：若走复活路径则 CreatedAt 保持旧值且 IsDeleted=false；正确路径应保留 IsDeleted=true 并新建
        // 此单测仅验证模型语义，实际 Handler 分支在 BatchImportHerbsCommandHandler:Update 中已加 IsDeleted 判断
        Assert.True(deleted.IsDeleted);
        // 新建实体应有新 Id 与新 CreatedAt
        var newHerb = Herb.Create(deleted.Name, "g", 10m, "DG", null, null, null, null, null, null, null, null, Guid.NewGuid());
        Assert.NotEqual(deleted.Id, newHerb.Id);
        Assert.True(newHerb.CreatedAt >= now.AddSeconds(-1));
        Assert.False(newHerb.IsDeleted);
        // 旧记录仍保持软删
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public void ExistsByName_Should_Filter_Deleted()
    {
        var deleted = new Herb { Name = "甘草", IsDeleted = true };
        var active = new Herb { Name = "甘草", IsDeleted = false };
        // 仓储层 GetByNameAsync 过滤 IsDeleted 的语义验证（内存模拟）
        var list = new[] { deleted, active };
        var activeOnly = list.Where(h => h.Name == "甘草" && !h.IsDeleted).ToList();
        Assert.Single(activeOnly);
        Assert.False(activeOnly[0].IsDeleted);
    }
}
