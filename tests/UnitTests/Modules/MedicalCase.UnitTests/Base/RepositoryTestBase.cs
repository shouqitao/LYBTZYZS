using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;

namespace LYBT.Module.MedicalCase.Tests.Base;

/// <summary>
/// MedicalCase Repository 测试基类
/// </summary>
public abstract class RepositoryTestBase : IDisposable
{
    protected readonly AppDbContext Context;
    private readonly string _databaseName;

    protected RepositoryTestBase()
    {
        _databaseName = $"MedicalCaseTestDb_{Guid.NewGuid()}";
        
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
        // 清理医疗案例相关数据
        Context.Consultations.RemoveRange(Context.Consultations);
        Context.MedicalCases.RemoveRange(Context.MedicalCases);
        Context.Patients.RemoveRange(Context.Patients);
        Context.Users.RemoveRange(Context.Users);
        Context.Prescriptions.RemoveRange(Context.Prescriptions);
        
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