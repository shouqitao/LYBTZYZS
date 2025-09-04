using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;

namespace LYBT.Module.Formula.Tests.Base;

/// <summary>
/// Formula Repository 测试基类
/// </summary>
public abstract class RepositoryTestBase : IDisposable
{
    protected readonly AppDbContext Context;
    private readonly string _databaseName;

    protected RepositoryTestBase()
    {
        _databaseName = $"FormulaTestDb_{Guid.NewGuid()}";
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new AppDbContext(options);
        
        // 确保数据库已创建
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        // 子类可以重写此方法来添加特定的测试数据
        await Task.CompletedTask;
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    protected virtual async Task ClearTestDataAsync()
    {
        // 清理验方相关数据
        Context.Formulas.RemoveRange(Context.Formulas);
        Context.Prescriptions.RemoveRange(Context.Prescriptions);
        Context.PrescriptionItems.RemoveRange(Context.PrescriptionItems);
        Context.Herbs.RemoveRange(Context.Herbs);
        
        await Context.SaveChangesAsync();
    }

    public virtual void Dispose()
    {
        Context?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Context?.Dispose();
        }
    }
}